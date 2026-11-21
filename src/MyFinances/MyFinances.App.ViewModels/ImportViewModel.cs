using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class ImportViewModel : ScreenViewModel
{
	private string _file = "";

	private string _status = "";

	private string _summary = "";

	private bool _hidden;

	private bool _absenceSalary = true;

	private bool _firstMonth = true;

	private SpreadsheetRead? _reading;

	private PostedGroup? _importSel;

	public override string Title => "Importar planilha";

	public string File
	{
		get
		{
			return _file;
		}
		set
		{
			Set(ref _file, value, "File");
		}
	}

	public bool IncludeHidden
	{
		get
		{
			return _hidden;
		}
		set
		{
			Set(ref _hidden, value, "IncludeHidden");
		}
	}

	public bool ImportSalaryAndAbsences
	{
		get
		{
			return _absenceSalary;
		}
		set
		{
			Set(ref _absenceSalary, value, "ImportSalaryAndAbsences");
		}
	}

	public bool AdjustFirstMonth
	{
		get
		{
			return _firstMonth;
		}
		set
		{
			Set(ref _firstMonth, value, "AdjustFirstMonth");
		}
	}

	public string Status
	{
		get
		{
			return _status;
		}
		set
		{
			Set(ref _status, value, "Status");
		}
	}

	public string Summary
	{
		get
		{
			return _summary;
		}
		set
		{
			Set(ref _summary, value, "Summary");
		}
	}

	public bool HasReading => _reading != null;

	public ObservableCollection<MappingVM> Mappings { get; } = new ObservableCollection<MappingVM>();

	public ObservableCollection<ImportMonth> Months { get; } = new ObservableCollection<ImportMonth>();

	public ObservableCollection<ImportRow> Rows { get; } = new ObservableCollection<ImportRow>();

	public ObservableCollection<PostedGroup> Imports { get; } = new ObservableCollection<PostedGroup>();

	public PostedGroup? SelectedImport
	{
		get
		{
			return _importSel;
		}
		set
		{
			Set(ref _importSel, value, "SelectedImport");
		}
	}

	public RelayCommand ChooseCommand { get; }

	public RelayCommand ReadCommand { get; }

	public RelayCommand PreviewCommand { get; }

	public RelayCommand CheckAllCommand { get; }

	public RelayCommand UncheckAllCommand { get; }

	public RelayCommand ConfirmCommand { get; }

	public RelayCommand UndoCommand { get; }

	public ImportViewModel()
	{
		ChooseCommand = new RelayCommand(() =>
		{
			string text = Dialogs.OpenFile("Planilhas do Excel|*.xlsm;*.xlsx", "Escolha a planilha (ela não será alterada)");
			if (text != null)
			{
				File = text;
				Read();
			}
		});
		ReadCommand = new RelayCommand(Read, () => !string.IsNullOrWhiteSpace(File));
		PreviewCommand = new RelayCommand(RefreshPreview, () => HasReading);
		CheckAllCommand = new RelayCommand(() =>
		{
			Check(v: true);
		}, () => HasReading);
		UncheckAllCommand = new RelayCommand(() =>
		{
			Check(v: false);
		}, () => HasReading);
		ConfirmCommand = new RelayCommand(Confirm, () => HasReading);
		UndoCommand = new RelayCommand(() =>
		{
			if ((object)SelectedImport != null && Dialogs.Confirm("Desfazer \"" + SelectedImport.Group + "\"? Todos os lançamentos dessa importação serão removidos (salário e faltas ficam)."))
			{
				int value = Svc.Finance.UndoGroup(SelectedImport.Group);
				Dialogs.Info($"{value} lançamento(s) removido(s).");
				Load();
			}
		}, () => (object)SelectedImport != null);
	}

	public override void Load()
	{
		Imports.Clear();
		foreach (var g in Svc.Finance.Groups(EntryOrigin.Imported))
		{
			Imports.Add(new PostedGroup(g.Group, g.Rows, g.Months));
		}
		SelectedImport = Imports.LastOrDefault();
	}

	private void Read()
	{
		_reading = Svc.Import.Read(File, IncludeHidden);
		OnPropertyChanged("HasReading");
		Mappings.Clear();
		foreach (TableMapping m in _reading.Mappings)
		{
			Mappings.Add(new MappingVM(m));
		}
		Months.Clear();
		foreach (ImportMonth m2 in _reading.Months.OrderBy((ImportMonth importMonth) => importMonth.Month))
		{
			Months.Add(m2);
		}
		string[] array = new string[5]
		{
			$"{_reading.Months.Count} mês(es) encontrados",
			null,
			null,
			null,
			null
		};
		YearMonth? firstMonth = _reading.FirstMonth;
		string text;
		if (firstMonth.HasValue)
		{
			YearMonth p = firstMonth.GetValueOrDefault();
			text = $"; primeiro mês do PAINEL: {p}";
		}
		else
		{
			text = "";
		}
		array[1] = text;
		array[2] = ". ";
		array[3] = string.Join(" ", _reading.Notes);
		array[4] = " O arquivo original não foi alterado.";
		Status = string.Concat(array);
		RefreshPreview();
	}

	private void RefreshPreview()
	{
		if (_reading == null)
		{
			return;
		}
		Rows.Clear();
		foreach (ImportRow l in Svc.Import.Preview(_reading))
		{
			Rows.Add(l);
		}
		RefreshSummary();
	}

	private void Check(bool v)
	{
		foreach (ImportRow item in Rows.Where((ImportRow importRow) => importRow.Notice == null || importRow.Notice.StartsWith("Fórmula")))
		{
			item.Selected = v;
		}
		List<ImportRow> list = Rows.ToList();
		Rows.Clear();
		foreach (ImportRow l in list)
		{
			Rows.Add(l);
		}
		RefreshSummary();
	}

	private void RefreshSummary()
	{
		List<ImportRow> sel = Rows.Where((ImportRow l) => l.Selected).ToList();
		Summary = $"{sel.Count} de {Rows.Count} linha(s) marcadas · proventos {Money.Fmt(sel.Where((ImportRow l) => l.Kind == EntryKind.Earning).Sum((ImportRow l) => l.Amount))} · descontos {Money.Fmt(sel.Where((ImportRow l) => l.Kind == EntryKind.Deduction).Sum((ImportRow l) => l.Amount))} · entradas {Money.Fmt(sel.Where((ImportRow l) => l.Kind == EntryKind.Inflow).Sum((ImportRow l) => l.Amount))} · despesas {Money.Fmt(sel.Where((ImportRow l) => l.Kind == EntryKind.Expense).Sum((ImportRow l) => l.Amount))}";
	}

	private void Confirm()
	{
		if (_reading != null)
		{
			RefreshSummary();
			int months = Enumerable.Count(Months, (ImportMonth importMonth) => importMonth.Selected);
			if (Dialogs.Confirm($"Gravar no MyFinances?\n\n{Summary}\nMeses: {months}" + (ImportSalaryAndAbsences ? " (com salário e faltas)" : "") + "\n\nA planilha original não é alterada. Dá para desfazer a importação inteira depois."))
			{
				var (n, m, g) = Svc.Import.Confirm(_reading, Rows, ImportSalaryAndAbsences, AdjustFirstMonth);
				Dialogs.Info($"Importação concluída: {n} lançamento(s) e {m} mês(es).\nGrupo: {g}");
				_reading = null;
				OnPropertyChanged("HasReading");
				Rows.Clear();
				Mappings.Clear();
				Months.Clear();
				Status = "Importação gravada.";
				Load();
			}
		}
	}
}
