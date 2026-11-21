using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public sealed class FinanceService(SettingsService config)
{
	public static readonly string[] ReservedNames = new string[4] { "INSS", "Vale transporte", "Faltas e atrasos", "IRRF" };

	public static bool IsReservedName(string name, EntryKind kind)
	{
		if (kind == EntryKind.Deduction)
		{
			return ReservedNames.Any((string n) => string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	public Dictionary<YearMonth, decimal> SalariesSet()
	{
		using AppDbContext db = AppDbContext.Create();
		return (from m in db.Months.AsNoTracking()
			where m.SalarySet != (decimal?)null
			select m).ToList().ToDictionary((FinanceMonth m) => YearMonth.ParseKey(m.YearMonth), (FinanceMonth m) => m.SalarySet.Value);
	}

	public MonthData GetMonth(YearMonth c)
	{
		Preferences prefs = config.Preferences();
		FinanceSummaryResult summary = Summary(c, c, prefs);
		using AppDbContext db = AppDbContext.Create();
		FinanceMonth month = db.Months.AsNoTracking().FirstOrDefault((FinanceMonth m) => m.YearMonth == ((YearMonth)c).Key);
		List<Entry> entries = (from l in db.Entries.AsNoTracking().Include((Entry l) => l.Category)
			where l.YearMonth == ((YearMonth)c).Key
			orderby l.Kind, l.CreatedAt
			select l).ToList();
		decimal salary = FinanceSummary.CurrentSalary(SalariesSet(), c);
		MonthAbsences absences = ((month == null) ? MonthAbsences.None : new MonthAbsences(month.AbsenceDays, month.LostDsr, month.LateHours));
		(LegalParameters Parameters, bool Estimated) tuple = config.ParametersForYear(c.Year);
		LegalParameters p = tuple.Parameters;
		bool est = tuple.Estimated;
		MonthSummary r = summary.Months.FirstOrDefault();
		PayrollResult payroll = r?.Payroll ?? Payroll.Calculate(salary, 0m, 0m, absences, prefs.Payroll, p);
		return new MonthData(c, salary, (month?.SalarySet).HasValue, absences, month?.Note, entries, payroll, r, est);
	}

	public FinanceSummaryResult Summary(YearMonth from, YearMonth until, Preferences? prefs = null)
	{
		if ((object)prefs == null)
		{
			prefs = config.Preferences();
		}
		YearMonth trackingStart = (YearMonth.TryParseKey(prefs.FirstMonth, out var pm) ? pm : from);
		YearMonth start = ((trackingStart < from) ? trackingStart : from);
		YearMonth calcEnd = ((until.Month == 12) ? until : new YearMonth(until.Year, 12));
		FixedEntries.Extend(calcEnd);
		List<MonthCalc> months = LoadMonths(start, calcEnd);
		Dictionary<int, LegalParameters> cache = new Dictionary<int, LegalParameters>();
		FinanceSummaryResult r = FinanceSummary.Calculate(months, prefs.OpeningBalance, prefs.Payroll, Params);
		return r with
		{
			Months = r.Months.Where((MonthSummary m) => m.Month >= from && m.Month <= until).ToList()
		};
		LegalParameters Params(int year)
		{
			if (!cache.TryGetValue(year, out LegalParameters v))
			{
				return cache[year] = config.ParametersForYear(year).Parameters;
			}
			return v;
		}
	}

	private List<MonthCalc> LoadMonths(YearMonth from, YearMonth until)
	{
		using AppDbContext db = AppDbContext.Create();
		List<string> keys = (from yearMonth in YearMonth.Range(@from, until)
			select yearMonth.Key).ToList();
		Dictionary<string, FinanceMonth> months = (from financeMonth in db.Months.AsNoTracking()
			where keys.Contains(financeMonth.YearMonth)
			select financeMonth).ToList().ToDictionary((FinanceMonth financeMonth) => financeMonth.YearMonth);
		Dictionary<string, List<Entry>> entries = (from l in (from l in db.Entries.AsNoTracking().Include((Entry l) => l.Category)
				where keys.Contains(l.YearMonth)
				select l).ToList()
			group l by l.YearMonth).ToDictionary((IGrouping<string, Entry> g) => g.Key, (IGrouping<string, Entry> g) => g.ToList());
		Dictionary<YearMonth, decimal> salary = SalariesSet();
		List<MonthCalc> list = new List<MonthCalc>();
		foreach (YearMonth c in YearMonth.Range(from, until))
		{
			months.TryGetValue(c.Key, out var m);
			entries.TryGetValue(c.Key, out var ls);
			MonthAbsences absences = ((m == null) ? MonthAbsences.None : new MonthAbsences(m.AbsenceDays, m.LostDsr, m.LateHours));
			list.Add(new MonthCalc(c, FinanceSummary.CurrentSalary(salary, c), absences, (ls ?? new List<Entry>()).Select((Entry l) => new EntryCalc((EntryKind)l.Kind, l.Description, l.Amount, l.Category?.Name)).ToList()));
		}
		return list;
	}

	public static List<string> Validate(EntryKind kind, string description, decimal amount, bool allowNegative = false)
	{
		List<string> e = new List<string>();
		if (string.IsNullOrWhiteSpace(description))
		{
			e.Add("Preencha a descrição.");
		}
		else if (description.Length > 200)
		{
			e.Add("Descrição muito longa (máximo 200 caracteres).");
		}
		if (amount == 0m)
		{
			e.Add("O valor não pode ser zero.");
		}
		if (amount < 0m && !allowNegative)
		{
			e.Add("O valor precisa ser maior que zero (use o tipo certo: despesa, desconto...).");
		}
		if (Math.Abs(amount) > 100000000m)
		{
			e.Add("Valor fora do limite.");
		}
		if (IsReservedName(description, kind))
		{
			e.Add("Esse nome é de uma linha fixa (INSS, vale-transporte, faltas ou IRRF), que o app calcula sozinho.");
		}
		return e;
	}

	public (Guid Merged, List<Entry> Removed) Merge(IReadOnlyCollection<Guid> ids, string description)
	{
		if (ids.Count < 2)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Escolha pelo menos dois lançamentos para mesclar."));
		}
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = db.Entries.Where((Entry l) => ids.Contains(l.Id)).ToList();
		if (rows.Count != ids.Count)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Algum desses lançamentos não existe mais. Atualize a tela."));
		}
		if (rows.Select((Entry l) => (YearMonth: l.YearMonth, Kind: l.Kind)).Distinct().Count() > 1)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Só dá para mesclar lançamentos do mesmo mês e da mesma tabela."));
		}
		if (rows.Any((Entry l) => l.Origin == EntryOrigin.Adjustment))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("O ajuste para o saldo real não pode ser mesclado: ele é refeito toda vez que você confere com o banco."));
		}
		List<Entry> removed = rows.Select(Copy).ToList();
		Entry merged = new Entry
		{
			YearMonth = rows[0].YearMonth,
			Kind = rows[0].Kind,
			Description = description.Trim(),
			Amount = Money.R2(rows.Sum((Entry l) => l.Amount)),
			Origin = EntryOrigin.Manual,
			CreatedAt = rows.Min((Entry l) => l.CreatedAt),
			UpdatedAt = DateTime.Now
		};
		List<string> errors = Validate((EntryKind)merged.Kind, merged.Description, merged.Amount, allowNegative: true);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		db.Entries.RemoveRange(rows);
		db.Entries.Add(merged);
		Audit.Record(db, "Entry", merged.Id, "Merge", $"{merged.YearMonth} {rows.Count} row(s)");
		db.SaveChanges();
		return (Merged: merged.Id, Removed: removed);
	}

	public void UndoMerge(Guid merged, IReadOnlyList<Entry> removed)
	{
		AppDbContext db = AppDbContext.Create();
		try
		{
			Entry m = db.Entries.Find(merged);
			if (m != null)
			{
				db.Entries.Remove(m);
			}
			foreach (Entry e in removed.Where((Entry entry) => !db.Entries.Any((Entry x) => x.Id == entry.Id)))
			{
				db.Entries.Add(Copy(e));
			}
			Audit.Record(db, "Entry", merged, "UndoMerge", $"{removed.Count} row(s)");
			db.SaveChanges();
		}
		finally
		{
			if (db != null)
			{
				((IDisposable)db).Dispose();
			}
		}
	}

	public void Restore(Entry e)
	{
		using AppDbContext db = AppDbContext.Create();
		if (!db.Entries.Any((Entry x) => x.Id == e.Id))
		{
			db.Entries.Add(Copy(e));
			Audit.Record(db, "Entry", e.Id, "Restore", e.YearMonth);
			db.SaveChanges();
		}
	}

	private static bool SameEntry(Entry a, Entry b)
	{
		if (a.Kind == b.Kind && a.Amount == b.Amount)
		{
			return string.Equals(a.Description.Trim(), b.Description.Trim(), StringComparison.CurrentCultureIgnoreCase);
		}
		return false;
	}

	public (List<Guid> Created, int Skipped) CopyTo(IReadOnlyCollection<Guid> ids, YearMonth target)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> source = (from l in db.Entries.AsNoTracking()
			where ids.Contains(l.Id) && (int)l.Origin != 9
			select l).ToList();
		List<Entry> there = (from l in db.Entries.AsNoTracking()
			where l.YearMonth == ((YearMonth)target).Key
			select l).ToList();
		List<Guid> created = new List<Guid>();
		int skipped = 0;
		foreach (Entry e in from l in source
			orderby l.Kind, l.CreatedAt
			select l)
		{
			if (there.Any((Entry t) => SameEntry(t, e)))
			{
				skipped++;
				continue;
			}
			Entry copy = new Entry
			{
				YearMonth = target.Key,
				Kind = e.Kind,
				Description = e.Description.Trim(),
				Amount = e.Amount,
				CategoryId = e.CategoryId,
				Origin = EntryOrigin.Manual,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now
			};
			db.Entries.Add(copy);
			there.Add(copy);
			created.Add(copy.Id);
		}
		Audit.Record(db, "Entry", null, "Paste", $"{target.Key} {created.Count} copied, {skipped} skipped");
		db.SaveChanges();
		return (Created: created, Skipped: skipped);
	}

	public (List<(Guid Id, string From)> Moved, int Skipped) MoveTo(IReadOnlyCollection<Guid> ids, YearMonth target)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = db.Entries.Where((Entry l) => ids.Contains(l.Id) && (int)l.Origin != 9).ToList();
		List<Entry> there = (from l in db.Entries.AsNoTracking()
			where l.YearMonth == ((YearMonth)target).Key && !ids.Contains(l.Id)
			select l).ToList();
		List<(Guid, string)> moved = new List<(Guid, string)>();
		int skipped = 0;
		foreach (Entry e in rows)
		{
			if (!(e.YearMonth == target.Key))
			{
				if (there.Any((Entry t) => SameEntry(t, e)))
				{
					skipped++;
					continue;
				}
				moved.Add((e.Id, e.YearMonth));
				e.YearMonth = target.Key;
				e.UpdatedAt = DateTime.Now;
			}
		}
		Audit.Record(db, "Entry", null, "Move", $"{target.Key} {moved.Count} moved, {skipped} kept");
		db.SaveChanges();
		return (Moved: moved, Skipped: skipped);
	}

	public void MoveBack(IEnumerable<(Guid Id, string From)> moved)
	{
		using AppDbContext db = AppDbContext.Create();
		foreach (var item in moved)
		{
			Guid id = item.Id;
			string from = item.From;
			Entry e = db.Entries.Find(id);
			if (e != null)
			{
				e.YearMonth = from;
				e.UpdatedAt = DateTime.Now;
			}
		}
		Audit.Record(db, "Entry", null, "UndoMove");
		db.SaveChanges();
	}

	public void DeleteMany(IReadOnlyCollection<Guid> ids)
	{
		using AppDbContext db = AppDbContext.Create();
		db.Entries.RemoveRange(db.Entries.Where((Entry l) => ids.Contains(l.Id)));
		Audit.Record(db, "Entry", null, "UndoPaste", $"{ids.Count} row(s)");
		db.SaveChanges();
	}

	private static Entry Copy(Entry e)
	{
		return new Entry
		{
			Id = e.Id,
			YearMonth = e.YearMonth,
			Kind = e.Kind,
			Description = e.Description,
			Amount = e.Amount,
			CategoryId = e.CategoryId,
			Origin = e.Origin,
			Group = e.Group,
			Installment = e.Installment,
			CreatedAt = e.CreatedAt,
			UpdatedAt = e.UpdatedAt
		};
	}

	public Entry? Get(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		return db.Entries.AsNoTracking().FirstOrDefault((Entry l) => l.Id == id);
	}

	public void Save(Entry l)
	{
		List<string> errors = Validate((EntryKind)l.Kind, l.Description, l.Amount, l.Origin != EntryOrigin.Manual);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		using AppDbContext db = AppDbContext.Create();
		Entry existing = db.Entries.FirstOrDefault((Entry x) => x.Id == l.Id);
		if (existing == null)
		{
			Entry entry = l;
			DateTime createdAt = (l.UpdatedAt = DateTime.Now);
			entry.CreatedAt = createdAt;
			db.Entries.Add(l);
			Audit.Record(db, "Entry", l.Id, "Create", $"{l.YearMonth} kind {l.Kind}");
		}
		else
		{
			existing.YearMonth = l.YearMonth;
			existing.Kind = l.Kind;
			existing.Description = l.Description.Trim();
			existing.Amount = l.Amount;
			existing.CategoryId = l.CategoryId;
			existing.UpdatedAt = DateTime.Now;
			Audit.Record(db, "Entry", l.Id, "Update", $"{l.YearMonth} kind {l.Kind}");
		}
		db.SaveChanges();
	}

	public decimal RemainingWithoutAdjustment(YearMonth month)
	{
		MonthData month2 = GetMonth(month);
		decimal remaining = month2.Summary?.RemainingWith13th ?? 0m;
		IEnumerable<Entry> adjustments = month2.Entries.Where((Entry l) => l.Origin == EntryOrigin.Adjustment);
		return remaining + adjustments.Where((Entry l) => l.Kind == 4).Sum((Entry l) => l.Amount) - adjustments.Where((Entry l) => l.Kind == 3).Sum((Entry l) => l.Amount);
	}

	public DateTime? LastAdjustment(YearMonth month)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from l in db.Entries.AsNoTracking()
			where l.YearMonth == ((YearMonth)month).Key && (int)l.Origin == 9
			select l).Select((Expression<Func<Entry, DateTime?>>)((Entry l) => l.CreatedAt)).Max();
	}

	public decimal AdjustToRealBalance(YearMonth month, decimal realBalance, decimal pendingInflow, decimal pendingOutflow)
	{
		decimal diff = FinanceSummary.RealBalanceDifference(RemainingWithoutAdjustment(month), realBalance, pendingInflow, pendingOutflow);
		using AppDbContext db = AppDbContext.Create();
		foreach (Entry v in db.Entries.Where((Entry entry) => entry.YearMonth == ((YearMonth)month).Key && (int)entry.Origin == 9).ToList())
		{
			db.Entries.Remove(v);
			Audit.Record(db, "Entry", v.Id, "Delete", v.YearMonth + " adjustment replaced");
		}
		if (diff != 0m)
		{
			Entry l = new Entry
			{
				YearMonth = month.Key,
				Kind = ((diff > 0m) ? 4 : 3),
				Description = $"Ajuste para o saldo real ({Money.Fmt(realBalance)} em {DateTime.Today:dd/MM/yyyy})",
				Amount = Math.Abs(diff),
				Origin = EntryOrigin.Adjustment,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now
			};
			db.Entries.Add(l);
			Audit.Record(db, "Entry", l.Id, "Create", l.YearMonth + " balance adjustment");
		}
		db.SaveChanges();
		return diff;
	}

	public void Delete(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		Entry l = db.Entries.Find(id);
		if (l != null)
		{
			db.Entries.Remove(l);
			Audit.Record(db, "Entry", id, "Delete", $"{l.YearMonth} kind {l.Kind}");
			db.SaveChanges();
		}
	}

	public int ApplyPlan(IEnumerable<PlanItem> items, string group, EntryOrigin origin, Guid? categoryId = null)
	{
		using AppDbContext db = AppDbContext.Create();
		if (db.Entries.Any((Entry l) => l.Group == group))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Já existem lançamentos do grupo \"" + group + "\". Desfaça antes de lançar de novo."));
		}
		int n = 0;
		foreach (PlanItem i in items)
		{
			if (!(i.Amount == 0m))
			{
				Match installment = Regex.Match(i.Description, "(\\d+/\\d+)$");
				db.Entries.Add(new Entry
				{
					YearMonth = i.Month.Key,
					Kind = (int)i.Kind,
					Description = i.Description,
					Amount = Money.R2(i.Amount),
					Origin = origin,
					Group = group,
					CategoryId = categoryId,
					Installment = (installment.Success ? installment.Groups[1].Value : null)
				});
				n++;
			}
		}
		Audit.Record(db, "Entry", null, "ApplyGroup", $"{origin} {n} row(s)");
		db.SaveChanges();
		return n;
	}

	public int UndoGroup(string group)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> ls = db.Entries.Where((Entry l) => l.Group == group).ToList();
		db.Entries.RemoveRange(ls);
		Audit.Record(db, "Entry", null, "UndoGroup", $"{ls.Count} row(s)");
		db.SaveChanges();
		return ls.Count;
	}

	public List<(string Group, int Rows, string Months)> Groups(EntryOrigin origin)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from l in (from l in db.Entries.AsNoTracking()
				where (int)l.Origin == (int)origin && l.Group != null
				select l).ToList()
			group l by l.Group into g
			select (Key: g.Key, g.Count(), string.Join(", ", g.Select((Entry x) => YearMonth.ParseKey(x.YearMonth).SheetName).Distinct())) into g
			orderby g.Key
			select g).ToList();
	}

	public int PostRepeated(EntryKind kind, string description, decimal amount, YearMonth first, int months, bool numberInstallments, Guid? categoryId)
	{
		List<string> errors = Validate(kind, description, amount);
		if ((months < 1 || months > 240) ? true : false)
		{
			errors.Add("Repetir por: de 1 a 240 meses.");
		}
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		string group = (numberInstallments ? "Parcelado " : "Recorrente ") + Guid.NewGuid().ToString("N").Substring(0, 8);
		IEnumerable<PlanItem> items = from k in Enumerable.Range(1, months)
			select new PlanItem(first.AddMonths(k - 1), kind, numberInstallments ? $"{description.Trim()} {k}/{months}" : description.Trim(), amount);
		return ApplyPlan(items, group, numberInstallments ? EntryOrigin.Installments : EntryOrigin.Recurring, categoryId);
	}

	private static FinanceMonth GetOrCreateMonth(AppDbContext db, YearMonth c)
	{
		FinanceMonth m = db.Months.FirstOrDefault((FinanceMonth x) => x.YearMonth == ((YearMonth)c).Key);
		if (m == null)
		{
			m = new FinanceMonth
			{
				YearMonth = c.Key
			};
			db.Months.Add(m);
		}
		return m;
	}

	public void SaveAbsences(YearMonth c, MonthAbsences f)
	{
		if (f.AbsenceDays < 0m || f.LostDsr < 0m || f.LateHours < 0m)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Faltas e atrasos não podem ser negativos."));
		}
		if (f.AbsenceDays > 31m || f.LostDsr > 6m || f.LateHours > 300m)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Confira as faltas: valores acima do possível no mês."));
		}
		using AppDbContext db = AppDbContext.Create();
		FinanceMonth orCreateMonth = GetOrCreateMonth(db, c);
		orCreateMonth.AbsenceDays = f.AbsenceDays;
		orCreateMonth.LostDsr = f.LostDsr;
		orCreateMonth.LateHours = f.LateHours;
		db.SaveChanges();
	}

	public void SaveNote(YearMonth c, string? text)
	{
		using AppDbContext db = AppDbContext.Create();
		GetOrCreateMonth(db, c).Note = (string.IsNullOrWhiteSpace(text) ? null : text.Trim());
		db.SaveChanges();
	}

	public void SetSalary(YearMonth fromMonth, decimal? newValue)
	{
		if (newValue <= (decimal?)0m)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Digite um salário maior que zero."));
		}
		using AppDbContext db = AppDbContext.Create();
		FinanceMonth m = GetOrCreateMonth(db, fromMonth);
		decimal previous = FinanceSummary.CurrentSalary(SalariesSet(), fromMonth.Previous);
		m.SalarySet = ((!newValue.HasValue || (previous > 0m && Money.R2(newValue.Value) == previous)) ? ((decimal?)null) : new decimal?(Money.R2(newValue.Value)));
		Audit.Record(db, "FinanceMonth", m.Id, "SetSalary", fromMonth.Key);
		db.SaveChanges();
	}

	public List<Category> Categories()
	{
		using AppDbContext db = AppDbContext.Create();
		return (from c in db.Categories.AsNoTracking()
			where c.Active
			orderby c.Name
			select c).ToList();
	}

	public void SaveCategory(Category c)
	{
		if (string.IsNullOrWhiteSpace(c.Name))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Dê um nome à categoria."));
		}
		using AppDbContext db = AppDbContext.Create();
		Category e = db.Categories.Find(c.Id);
		if (e == null)
		{
			db.Categories.Add(c);
		}
		else
		{
			e.Name = c.Name.Trim();
			e.Kind = c.Kind;
			e.Color = c.Color;
			e.Active = c.Active;
		}
		db.SaveChanges();
	}

	public List<(YearMonth Month, string Description, decimal Amount)> Earnings()
	{
		using AppDbContext db = AppDbContext.Create();
		return (from l in (from l in db.Entries.AsNoTracking()
				where l.Kind == 1
				select l).ToList()
			select (YearMonth.ParseKey(l.YearMonth), Description: l.Description, Amount: l.Amount)).ToList();
	}

	public decimal PayrollLoansOfMonth(YearMonth c)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from l in (from l in db.Entries.AsNoTracking()
				where l.YearMonth == ((YearMonth)c).Key && l.Kind == 2
				select l).ToList()
			where l.Amount > 0m && !l.Description.Contains("férias", StringComparison.OrdinalIgnoreCase)
			select l).Sum((Entry l) => l.Amount);
	}

	public decimal PayrollLoanInstallmentsSince(YearMonth c)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from l in (from l in db.Entries.AsNoTracking()
				where l.Kind == 2
				select l).ToList()
			where YearMonth.ParseKey(l.YearMonth) >= c && l.Amount > 0m && l.Description.Contains("consig", StringComparison.OrdinalIgnoreCase)
			select l).Sum((Entry l) => l.Amount);
	}

	public int VacationsPostedUntil(DateOnly date)
	{
		return Enumerable.Count(Groups(EntryOrigin.Vacation), ((string Group, int Rows, string Months) g) => DateOnly.TryParseExact(g.Group.Replace("Férias ", ""), "dd/MM/yyyy", out var result) && result <= date);
	}
}
