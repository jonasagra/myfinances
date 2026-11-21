using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data.Infra;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MyFinances.Data.Services;

public sealed class ImportService(FinanceService finance, SettingsService config)
{
	private static readonly string[] FixedRows = new string[3] { "INSS", "Vale transporte", "Faltas e atrasos" };

	public SpreadsheetRead Read(string originalFile, bool includeHiddenSheets)
	{
		if (!File.Exists(originalFile))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Arquivo não encontrado."));
		}
		string ext = Path.GetExtension(originalFile).ToLowerInvariant();
		if ((!(ext == ".xlsm") && !(ext == ".xlsx")) || 1 == 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Escolha uma planilha .xlsm ou .xlsx."));
		}
		if (new FileInfo(originalFile).Length > 52428800)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Planilha grande demais (máximo 50 MB)."));
		}
		Directory.CreateDirectory(AppPaths.Temp);
		string copy = Path.Combine(AppPaths.Temp, "import-" + Guid.NewGuid().ToString("N") + ext);
		using (FileStream src = new FileStream(originalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			using FileStream dst = File.Create(copy);
			src.CopyTo(dst);
		}
		try
		{
			string sha;
			using (FileStream f = File.OpenRead(copy))
			{
				sha = Convert.ToHexString(SHA256.HashData(f));
			}
			bool macros;
			using (ZipArchive z = ZipFile.OpenRead(copy))
			{
				macros = z.Entries.Any((ZipArchiveEntry e) => e.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase));
			}
			using XLWorkbook wb = new XLWorkbook(copy);
			YearMonth? first = null;
			if (wb.Worksheets.TryGetWorksheet("PAINEL", out IXLWorksheet dashboard))
			{
				IXLCell c = dashboard.Cell("I3");
				XLCellValue v = (c.HasFormula ? c.CachedValue : c.Value);
				if (v.IsDateTime)
				{
					first = YearMonth.From(v.GetDateTime());
				}
			}
			SpreadsheetRead reading = new SpreadsheetRead
			{
				File = originalFile,
				Sha256 = sha,
				FirstMonth = first,
				HadMacros = macros
			};
			if (macros)
			{
				reading.Notes.Add("A planilha tem macros VBA. Elas NÃO foram executadas; só os dados foram lidos.");
			}
			Dictionary<string, TableMapping> prefixes = new Dictionary<string, TableMapping>();
			foreach (IXLWorksheet ws in wb.Worksheets)
			{
				if (!YearMonth.TryParseSheetName(ws.Name, out var month))
				{
					continue;
				}
				bool hidden = ws.Visibility != XLWorksheetVisibility.Visible;
				if (hidden && !includeHiddenSheets)
				{
					continue;
				}
				IXLCell b3 = ws.Cell("B3");
				decimal? salaryNum = ReadCell(b3).Number;
				MonthAbsences absences = ReadAbsences(ws);
				reading.Months.Add(new ImportMonth
				{
					Month = month,
					Sheet = ws.Name,
					Hidden = hidden,
					Salary = salaryNum,
					SalaryTyped = !b3.HasFormula,
					Absences = absences
				});
				foreach (IXLTable t in ws.Tables)
				{
					string prefix = Regex.Replace(t.Name, "_[A-Z]{3}\\d{2}$", "");
					if (prefix.Equals("tbFaltas", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					List<string> fields = t.Fields.Select((IXLTableField iXLTableField) => iXLTableField.Name).ToList();
					if (!prefixes.TryGetValue(prefix, out var map))
					{
						map = (prefixes[prefix] = new TableMapping
						{
							Prefix = prefix,
							Columns = fields,
							Kind = DefaultKind(prefix),
							DescriptionColumn = (fields.FirstOrDefault((string text) => text.Equals("Lançamento", StringComparison.OrdinalIgnoreCase)) ?? fields.FirstOrDefault() ?? ""),
							AmountColumn = (fields.FirstOrDefault((string text) => text.Equals("Valor", StringComparison.OrdinalIgnoreCase)) ?? fields.LastOrDefault() ?? "")
						});
					}
					map.TableCount++;
					foreach (IXLTableRow row in t.DataRange.Rows())
					{
						Dictionary<string, (string, decimal?, string)> cols = new Dictionary<string, (string, decimal?, string)>();
						for (int i = 0; i < fields.Count; i++)
						{
							cols[fields[i]] = ReadCell(row.Cell(i + 1));
						}
						reading.Raw.Add((ws.Name, hidden, month, t.Name, prefix, row.RowNumber(), cols));
					}
				}
			}
			reading.Mappings.AddRange(prefixes.Values.OrderBy((TableMapping m) => m.Prefix));
			if (reading.Months.Count == 0)
			{
				throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Não encontrei abas de mês (nomes como \"OUT 2026\") nessa planilha."));
			}
			Log.Info($"Planilha lida para importação: {reading.Months.Count} mês(es), {reading.Mappings.Count} tipo(s) de tabela.");
			return reading;
		}
		finally
		{
			try
			{
				File.Delete(copy);
			}
			catch
			{
			}
		}
	}

	private static EntryKind? DefaultKind(string prefix)
	{
		return prefix.ToLowerInvariant() switch
		{
			"tbproventos" => (EntryKind?)EntryKind.Earning, 
			"tbdescontos" => EntryKind.Deduction, 
			"tbentradas" => EntryKind.Inflow, 
			"tbdespesas" => EntryKind.Expense, 
			_ => null, 
		};
	}

	private static MonthAbsences ReadAbsences(IXLWorksheet ws)
	{
		decimal days = 0m;
		decimal dsr = 0m;
		decimal hours = 0m;
		IXLTable t = ws.Tables.FirstOrDefault((IXLTable x) => x.Name.StartsWith("tbFaltas", StringComparison.OrdinalIgnoreCase));
		if (t != null)
		{
			foreach (IXLTableRow item2 in t.DataRange.Rows())
			{
				string item = item2.Cell(1).GetString().Trim()
					.ToLowerInvariant();
				decimal? n = ReadCell(item2.Cell(2)).Number;
				if (n.HasValue)
				{
					if (item.StartsWith("dias de falta"))
					{
						days = n.Value;
					}
					else if (item.StartsWith("repousos"))
					{
						dsr = n.Value;
					}
					else if (item.StartsWith("horas de atraso"))
					{
						hours = n.Value;
					}
				}
			}
		}
		return new MonthAbsences(days, dsr, hours);
	}

	private static (string Text, decimal? Number, string How) ReadCell(IXLCell c)
	{
		if (c.HasFormula)
		{
			if (Arithmetic.TryEvaluate(c.FormulaA1, out var v))
			{
				return (Text: Money.Num(v), Number: v, How: "fórmula recalculada");
			}
			XLCellValue cv = c.CachedValue;
			if (cv.IsNumber)
			{
				return (Text: cv.GetNumber().ToString(CultureInfo.InvariantCulture), Number: (decimal)cv.GetNumber(), How: "valor guardado pelo Excel");
			}
			if (cv.IsText)
			{
				return (Text: cv.GetText(), Number: null, How: "texto");
			}
			return (Text: "", Number: null, How: "vazio");
		}
		XLCellValue val = c.Value;
		if (val.IsNumber)
		{
			return (Text: val.GetNumber().ToString(CultureInfo.InvariantCulture), Number: (decimal)val.GetNumber(), How: "valor");
		}
		if (val.IsText)
		{
			return (Text: val.GetText(), Number: null, How: "texto");
		}
		if (val.IsBlank)
		{
			return (Text: "", Number: null, How: "vazio");
		}
		return (Text: val.ToString(), Number: null, How: "outro");
	}

	public List<ImportRow> Preview(SpreadsheetRead l)
	{
		Dictionary<string, TableMapping> maps = l.Mappings.ToDictionary((TableMapping tableMapping) => tableMapping.Prefix);
		List<ImportRow> rows = new List<ImportRow>();
		using AppDbContext db = AppDbContext.Create();
		List<string> keys = l.Months.Select((ImportMonth importMonth) => importMonth.Month.Key).ToList();
		HashSet<string> existingOnes = (from x in (from x in db.Entries.AsNoTracking()
				where keys.Contains(x.YearMonth)
				select x).ToList()
			select $"{x.YearMonth}|{x.Kind}|{x.Description.Trim().ToLowerInvariant()}|{x.Amount:0.00}").ToHashSet();
		foreach (var r in l.Raw)
		{
			if (!maps.TryGetValue(r.Prefix, out var m) || !m.Kind.HasValue)
			{
				continue;
			}
			r.Columns.TryGetValue(m.DescriptionColumn, out (string, decimal?, string) d);
			r.Columns.TryGetValue(m.AmountColumn, out (string, decimal?, string) v);
			string desc = (d.Item1 ?? "").Trim();
			if (desc.Length == 0)
			{
				decimal? item = v.Item2;
				if (!item.HasValue)
				{
					continue;
				}
			}
			if (desc.Equals("Total", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			ImportRow row = new ImportRow
			{
				Month = r.Month,
				Sheet = r.Sheet,
				SheetHidden = r.Hidden,
				Table = r.Table,
				Prefix = r.Prefix,
				SpreadsheetRow = r.Row,
				Kind = m.Kind,
				Description = desc,
				Amount = Money.R2(v.Item2.GetValueOrDefault()),
				AsRead = (v.Item3 ?? "")
			};
			if (m.Kind == EntryKind.Deduction && FixedRows.Any((string x) => x.Equals(desc, StringComparison.OrdinalIgnoreCase)))
			{
				row.Selected = false;
				row.Notice = "Linha fixa: o MyFinances calcula sozinho.";
			}
			else if (desc.StartsWith("13º", StringComparison.OrdinalIgnoreCase))
			{
				row.Selected = false;
				row.Notice = "13º: calculado pelo MyFinances (cenário com/sem 13º).";
			}
			else if (desc.Length == 0)
			{
				row.Selected = false;
				row.Notice = "Sem descrição.";
			}
			else
			{
				decimal? item = v.Item2;
				if (!item.HasValue)
				{
					row.Selected = false;
					row.Notice = "Sem valor numérico.";
				}
				else if (row.Amount == 0m)
				{
					row.Selected = false;
					row.Notice = "Valor zero.";
				}
				else if (existingOnes.Contains($"{r.Month.Key}|{(int)m.Kind.Value}|{desc.ToLowerInvariant()}|{row.Amount:0.00}"))
				{
					row.Selected = false;
					row.Notice = "Já existe igual no MyFinances.";
				}
				else if (v.Item3 == "valor guardado pelo Excel")
				{
					row.Notice = "Fórmula com referência: usado o último valor guardado pelo Excel.";
				}
			}
			rows.Add(row);
		}
		return (from x in rows
			orderby x.Month, x.Kind, x.SpreadsheetRow
			select x).ToList();
	}

	public (int Entries, int Months, string Group) Confirm(SpreadsheetRead l, IEnumerable<ImportRow> rows, bool importSalaryAndAbsences, bool adjustFirstMonth)
	{
		string group = $"Importação {DateTime.Now:yyyyMMdd-HHmmss}";
		List<ImportRow> sel = rows.Where((ImportRow importRow) => importRow.Selected && importRow.Kind.HasValue).ToList();
		HashSet<YearMonth> selectedMonths = (from importMonth in l.Months
			where importMonth.Selected
			select importMonth.Month).ToHashSet();
		sel = sel.Where((ImportRow importRow) => selectedMonths.Contains(importRow.Month)).ToList();
		int monthCount = 0;
		using (AppDbContext db = AppDbContext.Create())
		{
			using IDbContextTransaction tx = db.Database.BeginTransaction();
			foreach (ImportRow x in sel)
			{
				DbSet<Entry> entries = db.Entries;
				Entry entry = new Entry
				{
					YearMonth = x.Month.Key,
					Kind = (int)x.Kind.Value,
					Description = x.Description,
					Amount = x.Amount,
					Origin = EntryOrigin.Imported,
					Group = group
				};
				Match mm = Regex.Match(x.Description, "(\\d+/\\d+)\\s*$");
				entry.Installment = ((mm != null && mm.Success) ? mm.Groups[1].Value : null);
				entries.Add(entry);
			}
			if (importSalaryAndAbsences)
			{
				decimal? previous = null;
				foreach (ImportMonth m in from importMonth in l.Months
					where importMonth.Selected
					orderby importMonth.Month
					select importMonth)
				{
					FinanceMonth mf = db.Months.FirstOrDefault((FinanceMonth financeMonth) => financeMonth.YearMonth == m.Month.Key);
					if (mf == null)
					{
						mf = new FinanceMonth
						{
							YearMonth = m.Month.Key
						};
						db.Months.Add(mf);
					}
					if (m.Salary > (decimal?)0m)
					{
						if (previous.HasValue)
						{
							decimal value = Money.R2(m.Salary.Value);
							decimal? num = previous;
							if ((decimal?)value == num)
							{
								goto IL_0391;
							}
						}
						mf.SalarySet = Money.R2(m.Salary.Value);
					}
					goto IL_0391;
					IL_0391:
					if (m.Salary > (decimal?)0m)
					{
						previous = Money.R2(m.Salary.Value);
					}
					mf.AbsenceDays = m.Absences.AbsenceDays;
					mf.LostDsr = m.Absences.LostDsr;
					mf.LateHours = Money.R(m.Absences.LateHours, 6);
					monthCount++;
				}
			}
			Audit.Record(db, "Import", null, "Confirm", $"{sel.Count} entry(ies), {monthCount} month(s)");
			db.SaveChanges();
			tx.Commit();
		}
		if (adjustFirstMonth && selectedMonths.Count > 0)
		{
			Preferences p = config.Preferences();
			YearMonth smaller = l.FirstMonth ?? selectedMonths.Min();
			if (!YearMonth.TryParseKey(p.FirstMonth, out var current) || smaller < current || finance.SalariesSet().Count == 0)
			{
				config.Save(p with
				{
					FirstMonth = smaller.Key
				});
			}
		}
		Log.Info($"Importação confirmada: {sel.Count} lançamento(s).");
		return (Entries: sel.Count, Months: monthCount, Group: group);
	}
}
