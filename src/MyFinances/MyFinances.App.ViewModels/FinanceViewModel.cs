using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using MyFinances.App.Infra;
using MyFinances.App.Views;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class FinanceViewModel : ScreenViewModel
{
	private sealed record UndoStep(string Text, YearMonth Month, Action Undo);

	private sealed record Clip(IReadOnlyList<Guid> Ids, bool Cut, YearMonth From);

	private YearMonth _month = YearMonth.Today;

	private MonthData? _data;

	private EntryRow? _selected;

	private string _salary = "";

	private string _absenceDays = "0";

	private string _dsr = "0";

	private string _hours = "0";

	private string _note = "";

	private string _search = "";

	private readonly List<(EntryKind Kind, EntryRow Row)> _rows = new List<(EntryKind, EntryRow)>();

	private readonly Stack<UndoStep> _undo = new Stack<UndoStep>();

	private readonly DispatcherTimer _toastTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(7L)
	};

	private string _toast = "";

	private static Clip? _clip;

	private bool _toastUndo = true;

	private IReadOnlyList<EntryRow> _selectedRows = Array.Empty<EntryRow>();

	public override string Title => "Meses e lançamentos";

	public string Toast
	{
		get
		{
			return _toast;
		}
		private set
		{
			Set(ref _toast, value, "Toast");
		}
	}

	public RelayCommand UndoCommand { get; }

	public bool ToastCanUndo
	{
		get
		{
			return _toastUndo;
		}
		private set
		{
			Set(ref _toastUndo, value, "ToastCanUndo");
		}
	}

	public bool CanPaste => (object)_clip != null;

	public RelayCommand CopyCommand { get; }

	public RelayCommand CutCommand { get; }

	public RelayCommand PasteCommand { get; }

	public IReadOnlyList<EntryRow> SelectedRows
	{
		get
		{
			return _selectedRows;
		}
		set
		{
			_selectedRows = value;
			OnPropertyChanged("SelectedRows");
			OnPropertyChanged("CanMergeSelected");
			OnPropertyChanged("MergeSelectedText");
		}
	}

	public bool CanMergeSelected => Enumerable.Count(_selectedRows, (EntryRow r) => r.CanEdit) > 1;

	public string MergeSelectedText => $"Mesclar os {Enumerable.Count(_selectedRows, (EntryRow r) => r.CanEdit)} selecionados em um só";

	public RelayCommand MergeTwinsCommand { get; }

	public RelayCommand MergeSelectedCommand { get; }

	public IReadOnlyList<EntryTable> Tables { get; } = new _003C_003Ez__ReadOnlyArray<EntryTable>(new EntryTable[4]
	{
		new EntryTable("Proventos no salário", EntryKind.Earning, "Provento", "Total"),
		new EntryTable("Descontos no salário", EntryKind.Deduction, "Desconto", "Total"),
		new EntryTable("Entradas na conta", EntryKind.Inflow, "Entrada", "Total (fora o 13º)"),
		new EntryTable("Despesas e gastos", EntryKind.Expense, "Despesa", "Total")
	});

	public string SalaryHeader
	{
		get
		{
			if ((object)_data != null)
			{
				return "Salário, faltas e observação do mês  ·  salário " + Money.Fmt(_data.Salary) + ((_data.Absences.AbsenceDays > 0m) ? ("  ·  " + _data.Absences.AbsenceDays.ToString("0.##", Money.PtBr) + " falta(s)") : "") + ((_data.Absences.LateHours > 0m) ? ("  ·  " + _data.Absences.LateHours.ToString("0.##", Money.PtBr) + " h de atraso") : "") + (string.IsNullOrWhiteSpace(_data.Note) ? "" : "  ·  tem observação");
			}
			return "Salário, faltas e observação do mês";
		}
	}

	public string MonthText => _month.SheetName;

	public string Payslip => Svc.Config.Calendar().PayslipText(_month);

	public MonthData? Data
	{
		get
		{
			return _data;
		}
		private set
		{
			Set(ref _data, value, "Data");
			OnPropertyChanged("SalarySource");
			OnPropertyChanged("Breakdown");
		}
	}

	public string SalarySource
	{
		get
		{
			if ((object)_data != null)
			{
				if (!_data.SalaryTyped)
				{
					return "repete o mês anterior";
				}
				return "digitado neste mês";
			}
			return "";
		}
	}

	public string Breakdown => _data?.Payroll.Breakdown.ToString() ?? "";

	public EntryRow? Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			Set(ref _selected, value, "Selected");
		}
	}

	public string Salary
	{
		get
		{
			return _salary;
		}
		set
		{
			Set(ref _salary, value, "Salary");
		}
	}

	public string AbsenceDays
	{
		get
		{
			return _absenceDays;
		}
		set
		{
			Set(ref _absenceDays, value, "AbsenceDays");
		}
	}

	public string Dsr
	{
		get
		{
			return _dsr;
		}
		set
		{
			Set(ref _dsr, value, "Dsr");
		}
	}

	public string Hours
	{
		get
		{
			return _hours;
		}
		set
		{
			Set(ref _hours, value, "Hours");
		}
	}

	public string Note
	{
		get
		{
			return _note;
		}
		set
		{
			Set(ref _note, value, "Note");
		}
	}

	public string Search
	{
		get
		{
			return _search;
		}
		set
		{
			if (Set(ref _search, value, "Search"))
			{
				FillTables();
			}
		}
	}

	public RelayCommand PreviousCommand { get; }

	public RelayCommand NextCommand { get; }

	public RelayCommand TodayCommand { get; }

	public RelayCommand NewCommand { get; }

	public RelayCommand EditCommand { get; }

	public RelayCommand DeleteCommand { get; }

	public RelayCommand UndoGroupCommand { get; }

	public RelayCommand SaveSalaryCommand { get; }

	public RelayCommand RepeatSalaryCommand { get; }

	public RelayCommand SaveAbsencesCommand { get; }

	public RelayCommand AdjustBalanceCommand { get; }

	public RelayCommand BreakdownCommand { get; }

	public RelayCommand ImportPayslipCommand { get; }

	public RelayCommand ClearSearchCommand { get; }

	private void Done(string text, Action undo)
	{
		ToastCanUndo = true;
		_undo.Push(new UndoStep(text, _month, undo));
		while (_undo.Count > 50)
		{
			List<UndoStep> list = _undo.Reverse().Skip(1).ToList();
			_undo.Clear();
			foreach (UndoStep u in list)
			{
				_undo.Push(u);
			}
		}
		Toast = text;
		_toastTimer.Stop();
		_toastTimer.Start();
	}

	public void Undo()
	{
		if (_undo.TryPop(out UndoStep step))
		{
			_toastTimer.Stop();
			Toast = "";
			step.Undo();
			_month = step.Month;
			Load();
		}
	}

	private void ShowNote(string text)
	{
		ToastCanUndo = false;
		Toast = text;
		_toastTimer.Stop();
		_toastTimer.Start();
	}

	public void Take(IReadOnlyList<EntryRow> rows, bool cut)
	{
		List<EntryRow> ok = rows.Where((EntryRow r) => (object)r != null && r.CanEdit && !r.IsAdjustment).ToList();
		if (ok.Count != 0)
		{
			_clip = new Clip(ok.Select((EntryRow r) => r.Id.Value).Distinct().ToList(), cut, _month);
			OnPropertyChanged("CanPaste");
			if (cut)
			{
				Load();
			}
			string what = ((ok.Count == 1) ? ("\"" + ok[0].Description + "\"") : $"{ok.Count} lançamentos");
			ShowNote(cut ? (what + " recortado(s) de " + _month.SheetName) : (what + " copiado(s) de " + _month.SheetName));
		}
	}

	public void Paste()
	{
		Clip clip = _clip;
		if ((object)clip == null)
		{
			return;
		}
		YearMonth target = _month;
		if (clip.Cut)
		{
			if (clip.From == target)
			{
				ShowNote("Esses lançamentos já estão neste mês.");
				return;
			}
			(List<(Guid, string)>, int) tuple = Svc.Finance.MoveTo(clip.Ids, target);
			List<(Guid Id, string From)> moved = tuple.Item1;
			int kept = tuple.Item2;
			_clip = null;
			OnPropertyChanged("CanPaste");
			Load();
			if (moved.Count == 0)
			{
				ShowNote("Nada foi movido: este mês já tem lançamentos iguais.");
				return;
			}
			Done($"{moved.Count} lançamento(s) movido(s) de {clip.From.SheetName} para {target.SheetName}" + ((kept > 0) ? $" · {kept} já existia(m) aqui" : ""), () =>
			{
				Svc.Finance.MoveBack(moved);
			});
			return;
		}
		var (created, skipped) = Svc.Finance.CopyTo(clip.Ids, target);
		Load();
		if (created.Count == 0)
		{
			ShowNote("Nada foi colado: este mês já tem esses lançamentos.");
			return;
		}
		Done($"{created.Count} lançamento(s) colado(s) de {clip.From.SheetName}" + ((skipped > 0) ? $" · {skipped} já existia(m) aqui" : ""), () =>
		{
			Svc.Finance.DeleteMany(created);
		});
	}

	public void MergeShortcut()
	{
		if (CanMergeSelected)
		{
			MergeSelectedCommand.Execute(null);
			return;
		}
		EntryRow row = Selected;
		if ((object)row != null && row.HasTwins)
		{
			MergeTwinsCommand.Execute(row);
		}
	}

	public void MergeRows(IReadOnlyList<EntryRow> rows, string description)
	{
		List<Guid> ids = (from r in rows
			where r.CanEdit
			select r.Id.Value).Distinct().ToList();
		if (ids.Count >= 2)
		{
			Guid merged;
			List<Entry> removed;
			(merged, removed) = Svc.Finance.Merge(ids, description);
			Load();
			Done($"{ids.Count} lançamentos mesclados em \"{description.Trim()}\"", () =>
			{
				Svc.Finance.UndoMerge(merged, removed);
			});
		}
	}

	public FinanceViewModel()
	{
		PreviousCommand = new RelayCommand(() =>
		{
			_month = _month.Previous;
			Load();
		});
		NextCommand = new RelayCommand(() =>
		{
			_month = _month.Next;
			Load();
		});
		TodayCommand = new RelayCommand(() =>
		{
			_month = YearMonth.Today;
			Load();
		});
		NewCommand = new RelayCommand((object? p) =>
		{
			AddEntry(p as string);
		});
		EditCommand = new RelayCommand((object? p) =>
		{
			Edit(Row(p));
		}, (object? p) => Row(p)?.CanEdit ?? false);
		DeleteCommand = new RelayCommand((object? p) =>
		{
			Delete(Row(p));
		}, (object? p) => Row(p)?.CanEdit ?? false);
		UndoGroupCommand = new RelayCommand((object? p) =>
		{
			UndoGroup(Row(p));
		}, (object? p) =>
		{
			EntryRow entryRow = Row(p);
			return (object)entryRow != null && !entryRow.Fixed && entryRow.Group != null;
		});
		SaveSalaryCommand = new RelayCommand(SaveSalary);
		RepeatSalaryCommand = new RelayCommand(() =>
		{
			if (Dialogs.Confirm(_month.SheetName + " vai voltar a repetir o salário do mês anterior. Continuar?"))
			{
				Svc.Finance.SetSalary(_month, null);
				Load();
			}
		});
		SaveAbsencesCommand = new RelayCommand(SaveAbsences);
		AdjustBalanceCommand = new RelayCommand(() =>
		{
			Dialogs.Guard(AdjustBalance);
		});
		ImportPayslipCommand = new RelayCommand(() =>
		{
			ImportPayslip(null);
		});
		BreakdownCommand = new RelayCommand(() =>
		{
			TextViewWindow textViewWindow = new TextViewWindow("Memória de cálculo · " + _month.SheetName, Breakdown);
			textViewWindow.Owner = Application.Current.MainWindow;
			textViewWindow.ShowDialog();
		});
		ClearSearchCommand = new RelayCommand(() =>
		{
			Search = "";
		});
		UndoCommand = new RelayCommand(Undo);
		CopyCommand = new RelayCommand((object? p) =>
		{
			Take(RowsFor(p), cut: false);
		});
		CutCommand = new RelayCommand((object? p) =>
		{
			Take(RowsFor(p), cut: true);
		});
		PasteCommand = new RelayCommand(Paste, () => CanPaste);
		_toastTimer.Tick += (object? _, EventArgs _) =>
		{
			_toastTimer.Stop();
			Toast = "";
		};
		MergeTwinsCommand = new RelayCommand((object? p) =>
		{
			EntryRow row = p as EntryRow;
			if ((object)row != null && row.HasTwins)
			{
				EntryKind kind = _rows.First(((EntryKind Kind, EntryRow Row) x) => x.Row == row).Kind;
				MergeRows((from x in _rows
					where x.Kind == kind && x.Row.CanEdit && SameName(x.Row, row)
					select x.Row).ToList(), row.Description);
			}
		}, (object? p) => p is EntryRow entryRow && entryRow.HasTwins);
		MergeSelectedCommand = new RelayCommand(() =>
		{
			List<EntryRow> list = _selectedRows.Where((EntryRow r) => r.CanEdit).ToList();
			string key = (from g in list.GroupBy((EntryRow r) => r.Description.Trim(), StringComparer.CurrentCultureIgnoreCase)
				orderby g.Count() descending, g.Sum((EntryRow r) => r.Amount) descending
				select g).First().Key;
			MergeRows(list, key);
		}, () => CanMergeSelected);
	}

	private static bool SameName(EntryRow a, EntryRow b)
	{
		return string.Equals(a.Description.Trim(), b.Description.Trim(), StringComparison.CurrentCultureIgnoreCase);
	}

	private EntryRow? Row(object? p)
	{
		return (p as EntryRow) ?? Selected;
	}

	private IReadOnlyList<EntryRow> RowsFor(object? p)
	{
		if (!(p is EntryRow r) || _selectedRows.Contains(r))
		{
			if (_selectedRows.Count <= 0)
			{
				EntryRow one = Row(p);
				if ((object)one == null)
				{
					return Array.Empty<EntryRow>();
				}
				return new _003C_003Ez__ReadOnlySingleElementList<EntryRow>(one);
			}
			return _selectedRows;
		}
		return new _003C_003Ez__ReadOnlySingleElementList<EntryRow>(r);
	}

	public void ShowMonth(YearMonth month)
	{
		_month = month;
	}

	public override void Load()
	{
		MonthData d = (Data = Svc.Finance.GetMonth(_month));
		OnPropertyChanged("MonthText");
		OnPropertyChanged("Payslip");
		Salary = Money.Num(d.Salary);
		AbsenceDays = d.Absences.AbsenceDays.ToString("0.##", Money.PtBr);
		Dsr = d.Absences.LostDsr.ToString("0.##", Money.PtBr);
		Hours = d.Absences.LateHours.ToString("0.####", Money.PtBr);
		Note = d.Note ?? "";
		_rows.Clear();
		Add(EntryKind.Deduction, new EntryRow(null, "INSS", d.Payroll.Inss, null, Fixed: true, ""));
		if (d.Payroll.TransportVoucher != 0m)
		{
			Add(EntryKind.Deduction, new EntryRow(null, "Vale transporte", d.Payroll.TransportVoucher, null, Fixed: true, ""));
		}
		if (d.Payroll.Absences != 0m)
		{
			Add(EntryKind.Deduction, new EntryRow(null, "Faltas e atrasos", d.Payroll.Absences, null, Fixed: true, ""));
		}
		if (d.Payroll.Irrf != 0m)
		{
			Add(EntryKind.Deduction, new EntryRow(null, "IRRF", d.Payroll.Irrf, null, Fixed: true, ""));
		}
		MonthSummary r = d.Summary;
		if ((object)r != null && r.Thirteenth > 0m)
		{
			Add(EntryKind.Inflow, new EntryRow(null, (_month.Month == 11) ? "13º salário - 1ª parcela" : "13º salário - 2ª parcela", r.Thirteenth, null, Fixed: true, ""));
		}
		foreach (Entry l in d.Entries)
		{
			Add((EntryKind)l.Kind, L(l));
		}
		for (int i = 0; i < _rows.Count; i++)
		{
			var (k, row) = _rows[i];
			if (row.CanEdit)
			{
				int n = Enumerable.Count(_rows, ((EntryKind Kind, EntryRow Row) x) => x.Kind == k && x.Row.CanEdit && SameName(x.Row, row));
				if (n > 1)
				{
					_rows[i] = (k, row with
					{
						Twins = n
					});
				}
			}
		}
		Tables[0].Total = d.Payroll.Earnings;
		Tables[1].Total = d.Payroll.TotalDeductions;
		Tables[2].Total = d.Summary?.Inflows ?? 0m;
		Tables[3].Total = d.Summary?.Expenses ?? 0m;
		OnPropertyChanged("SalaryHeader");
		FillTables();
		void Add(EntryKind item, EntryRow item2)
		{
			_rows.Add((item, item2));
		}
		static EntryRow L(Entry entry)
		{
			Clip c = _clip;
			EntryRow entryRow = new EntryRow(entry.Id, entry.Description, entry.Amount, entry.Group, Fixed: false, OriginName(entry.Origin))
			{
				IsCut = (object)c != null && c.Cut && c.Ids.Contains(entry.Id),
				IsAdjustment = entry.Origin == EntryOrigin.Adjustment,
				Recurring = FixedEntries.IsFixed(entry)
			};
			return entryRow;
		}
	}

	private void FillTables()
	{
		string term = _search.Trim();
		foreach (EntryTable t in Tables)
		{
			Fill(t.Rows, t.Kind);
		}
		void Fill(ObservableCollection<EntryRow> c, EntryKind kind)
		{
			c.Clear();
			foreach (var item in from x in _rows
				where x.Kind == kind && Match(x.Row)
				orderby x.Row.Amount descending
				select x)
			{
				EntryRow row = item.Row;
				c.Add(row);
			}
		}
		bool Match(EntryRow r)
		{
			if (term.Length != 0 && !r.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase) && !Money.Fmt(r.Amount).Contains(term))
			{
				return Money.Num(r.Amount).Contains(term);
			}
			return true;
		}
	}

	public static string OriginName(EntryOrigin o)
	{
		return o switch
		{
			EntryOrigin.Manual => "", 
			EntryOrigin.Imported => "importado", 
			EntryOrigin.Vacation => "férias", 
			EntryOrigin.Overtime => "hora extra", 
			EntryOrigin.Retroactive => "retroativo", 
			EntryOrigin.Termination => "demissão", 
			EntryOrigin.PayrollLoan => "consignado", 
			EntryOrigin.Installments => "parcela", 
			EntryOrigin.Recurring => "recorrente", 
			EntryOrigin.Adjustment => "ajuste", 
			EntryOrigin.Fixed => "fixo", 
			_ => "", 
		};
	}

	private void AdjustBalance()
	{
		BalanceAdjustmentWindow w = new BalanceAdjustmentWindow(_month.SheetName, Svc.Finance.RemainingWithoutAdjustment(_month))
		{
			Owner = Application.Current.MainWindow
		};
		if (w.ShowDialog() == true)
		{
			decimal diff = Svc.Finance.AdjustToRealBalance(_month, w.BalanceEntered, w.PendingInflow, w.PendingOutflow);
			Load();
			Dialogs.Info((diff == 0m) ? "Bateu certinho. Nenhum ajuste lançado." : $"Ajuste de {Money.Fmt(Math.Abs(diff))} lançado em {((diff > 0m) ? "Despesas" : "Entradas")} ({_month.SheetName}).\nPara desfazer, exclua a linha marcada \"ajuste\".");
		}
	}

	private void AddEntry(string? kind)
	{
		if (new EntryWindow(new EntryEdit(null, kind switch
		{
			"Earning" => EntryKind.Earning, 
			"Deduction" => EntryKind.Deduction, 
			"Inflow" => EntryKind.Inflow, 
			_ => EntryKind.Expense, 
		}, _month, "", null))
		{
			Owner = Application.Current.MainWindow
		}.ShowDialog() == true)
		{
			Load();
		}
	}

	private void Edit(EntryRow? row)
	{
		if ((object)row == null || !row.CanEdit)
		{
			return;
		}
		Guid? id = row.Id;
		if (!id.HasValue)
		{
			return;
		}
		Guid id2 = id.GetValueOrDefault();
		if ((object)_data == null)
		{
			return;
		}
		Entry l = _data.Entries.First((Entry x) => x.Id == id2);
		Entry before = Svc.Finance.Get(id2);
		EntryWindow w = new EntryWindow(new EntryEdit(l.Id, (EntryKind)l.Kind, YearMonth.ParseKey(l.YearMonth), l.Description, l.Amount))
		{
			Owner = Application.Current.MainWindow
		};
		if (w.ShowDialog() != true)
		{
			return;
		}
		FixedEntries.Change change = w.Vm.FixedChange;
		if ((object)change != null && before != null)
		{
			Load();
			Done(w.Vm.FixedChangeText ?? "Lançamento fixo alterado", () =>
			{
				FixedEntries.Undo(change);
				Svc.Finance.Save(before);
			});
			return;
		}
		Entry saved = Svc.Finance.Get(id2);
		if (saved != null && FixedEntries.IsFixed(saved) && before != null && (saved.Amount != before.Amount || saved.Description != before.Description || saved.Kind != before.Kind) && Dialogs.Confirm($"\"{saved.Description}\" é um lançamento fixo.\n\nAplicar a mudança também nos meses seguintes?\n\nSim: {YearMonth.ParseKey(saved.YearMonth).SheetName} em diante\nNão: só {YearMonth.ParseKey(saved.YearMonth).SheetName}", "Lançamento fixo"))
		{
			List<Entry> old = FixedEntries.ApplyForward(id2);
			Load();
			Done("\"" + saved.Description + "\" alterado daqui pra frente", () =>
			{
				FixedEntries.RestoreRows(old);
				Svc.Finance.Save(before);
			});
			return;
		}
		Load();
		if (before != null)
		{
			Entry after = Svc.Finance.Get(id2);
			if (after != null && after.Amount != before.Amount)
			{
				Done($"\"{after.Description}\": {Money.Fmt(before.Amount)} → {Money.Fmt(after.Amount)}", () =>
				{
					Svc.Finance.Save(before);
				});
				return;
			}
		}
		if (before != null)
		{
			Done("\"" + before.Description + "\" alterado", () =>
			{
				Svc.Finance.Save(before);
			});
		}
	}

	private void Delete(EntryRow? row)
	{
		if ((object)row == null || !row.CanEdit)
		{
			return;
		}
		Guid? id = row.Id;
		if (!id.HasValue)
		{
			return;
		}
		Guid id2 = id.GetValueOrDefault();
		if (row.Recurring)
		{
			string group = row.Group;
			if (group != null)
			{
				bool? answer = Dialogs.Choose($"\"{row.Description}\" é um lançamento fixo.\n\nSim: parar de {_month.SheetName} em diante (sai deste mês e dos próximos)\nNão: tirar só de {_month.SheetName}\nCancelar: não excluir", "Excluir lançamento fixo");
				if (!answer.HasValue)
				{
					return;
				}
				if (answer == true)
				{
					List<Entry> removed;
					FixedSeries series;
					(removed, series) = FixedEntries.StopFrom(group, _month);
					Load();
					Done("\"" + row.Description + "\" parado a partir de " + _month.SheetName, () =>
					{
						FixedEntries.UndoStop(removed, series);
					});
					return;
				}
				goto IL_01da;
			}
		}
		if (!Dialogs.Confirm($"Excluir \"{row.Description}\" ({Money.Fmt(row.Amount)}) de {_month.SheetName}?"))
		{
			return;
		}
		goto IL_01da;
		IL_01da:
		Entry copy = Svc.Finance.Get(id2);
		Svc.Finance.Delete(id2);
		Load();
		if (copy != null)
		{
			Done("\"" + row.Description + "\" excluído", () =>
			{
				Svc.Finance.Restore(copy);
			});
		}
	}

	public void ImportPayslip(string? path)
	{
		if (path == null)
		{
			path = Dialogs.OpenFile("Contracheque (*.pdf)|*.pdf", "Importar contracheque");
		}
		if (path == null)
		{
			return;
		}
		PayslipImportWindow w = new PayslipImportWindow(new PayslipImportViewModel(PayslipReader.Read(path), Path.GetFileName(path)))
		{
			Owner = Application.Current.MainWindow
		};
		if (w.ShowDialog() != true)
		{
			return;
		}
		YearMonth? target = w.Vm.Target;
		if (!target.HasValue)
		{
			return;
		}
		YearMonth month = target.GetValueOrDefault();
		PayslipImportViewModel vm = w.Vm;
		MonthData before = Svc.Finance.GetMonth(month);
		string group = "Contracheque " + month.Key + " " + Guid.NewGuid().ToString("N").Substring(0, 6);
		int n = ((vm.Selected.Count != 0) ? Svc.Finance.ApplyPlan(vm.Selected.Select((PayslipImportRow r) => new PlanItem(month, r.Kind, r.Name, r.Amount)), group, EntryOrigin.Imported) : 0);
		bool absences = vm.FillAbsences && vm.AbsencesDiffer;
		bool salary = vm.UpdateSalary && vm.SalaryDiffers && vm.PayslipSalary.HasValue;
		if (absences)
		{
			Svc.Finance.SaveAbsences(month, vm.PayslipAbsences);
		}
		if (salary)
		{
			Svc.Finance.SetSalary(month, vm.PayslipSalary);
		}
		_month = month;
		Load();
		if (n == 0 && !absences && !salary)
		{
			return;
		}
		List<string> parts = new List<string>();
		if (n > 0)
		{
			parts.Add($"{n} lançamento(s)");
		}
		if (absences)
		{
			parts.Add("faltas");
		}
		if (salary)
		{
			parts.Add("salário");
		}
		Done("Contracheque importado em " + month.SheetName + ": " + string.Join(", ", parts), () =>
		{
			if (n > 0)
			{
				Svc.Finance.UndoGroup(group);
			}
			if (absences)
			{
				Svc.Finance.SaveAbsences(month, before.Absences);
			}
			if (salary)
			{
				Svc.Finance.SetSalary(month, before.SalaryTyped ? new decimal?(before.Salary) : ((decimal?)null));
			}
		});
	}

	public void MoveEntry(EntryRow row, EntryKind kind)
	{
		if ((object)row == null || !row.CanEdit)
		{
			return;
		}
		Guid? id = row.Id;
		if (!id.HasValue)
		{
			return;
		}
		Guid id2 = id.GetValueOrDefault();
		Entry e = Svc.Finance.Get(id2);
		if (e == null || e.Kind == (int)kind)
		{
			return;
		}
		int from = e.Kind;
		e.Kind = (int)kind;
		Svc.Finance.Save(e);
		Load();
		string to = Tables.First((EntryTable t) => t.Kind == kind).Title;
		Done("\"" + row.Description + "\" movido para " + to, () =>
		{
			Entry entry = Svc.Finance.Get(id2);
			if (entry != null)
			{
				entry.Kind = from;
				Svc.Finance.Save(entry);
			}
		});
	}

	private void UndoGroup(EntryRow? row)
	{
		string g = row?.Group;
		if (g != null && Dialogs.Confirm("Apagar TODOS os lançamentos do grupo \"" + g + "\" (em todos os meses)?"))
		{
			int n = Svc.Finance.UndoGroup(g);
			Dialogs.Info($"{n} lançamento(s) removido(s).");
			Load();
		}
	}

	private void SaveSalary()
	{
		List<string> errors = new List<string>();
		decimal v = Input.Dec(Salary, "Salário base", errors);
		Input.Check(errors);
		Svc.Finance.SetSalary(_month, v);
		Svc.Finance.SaveNote(_month, Note);
		Load();
	}

	private void SaveAbsences()
	{
		List<string> errors = new List<string>();
		decimal d = Input.Dec(AbsenceDays, "Dias de falta", errors, required: false);
		decimal r = Input.Dec(Dsr, "DSR perdidos", errors, required: false);
		decimal h = ParseHours(Hours, errors);
		Input.Check(errors);
		Svc.Finance.SaveAbsences(_month, new MonthAbsences(d, r, h));
		Load();
	}

	private static decimal ParseHours(string txt, List<string> errors)
	{
		if (string.IsNullOrWhiteSpace(txt))
		{
			return 0m;
		}
		if (txt.Contains(':'))
		{
			if (Overtime.TryParseHours(txt, out var min))
			{
				return (decimal)min / 60m;
			}
			errors.Add("Horas de atraso: use h:mm (ex.: 7:17) ou número (ex.: 7,5).");
			return 0m;
		}
		return Input.Dec(txt, "Horas de atraso", errors, required: false);
	}
}
