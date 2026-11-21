using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using MyFinances.App.Infra;
using MyFinances.App.Views;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class EntriesManagerViewModel : ScreenViewModel
{
	private readonly EntriesManager _manager = new EntriesManager();

	private List<SeriesRow> _all = new List<SeriesRow>();

	private SeriesRow? _selected;

	private string _search = "";

	private string _newAmount = "";

	private string _newName = "";

	private string _until = "";

	private string _activeIn = "";

	private string _kindFilter = "Todos";

	private string _situationFilter = "Todos";

	private const string All = "Todos";

	private string _financed = "";

	private string _priceInfo = "";

	private string _priceError = "";

	private string _countText = "";

	public override string Title => "Gerenciar lançamentos";

	public ObservableCollection<SeriesRow> Series { get; } = new ObservableCollection<SeriesRow>();

	public ObservableCollection<OccurrenceRow> Occurrences { get; } = new ObservableCollection<OccurrenceRow>();

	public ObservableCollection<PriceViewRow> Price { get; } = new ObservableCollection<PriceViewRow>();

	public string Financed
	{
		get
		{
			return _financed;
		}
		set
		{
			if (Set(ref _financed, value, "Financed"))
			{
				BuildPrice();
			}
		}
	}

	public string PriceInfo
	{
		get
		{
			return _priceInfo;
		}
		private set
		{
			Set(ref _priceInfo, value, "PriceInfo");
		}
	}

	public string PriceError
	{
		get
		{
			return _priceError;
		}
		private set
		{
			Set(ref _priceError, value, "PriceError");
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
				Filter();
			}
		}
	}

	public IReadOnlyList<string> KindOptions { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[5] { "Todos", "Provento", "Desconto", "Entrada", "Despesa" });

	public IReadOnlyList<string> SituationOptions { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "Todos", "Parcelado", "Fixo / repetido", "Só um mês" });

	public string KindFilter
	{
		get
		{
			return _kindFilter;
		}
		set
		{
			if (Set(ref _kindFilter, value ?? "Todos", "KindFilter"))
			{
				Filter();
			}
		}
	}

	public string SituationFilter
	{
		get
		{
			return _situationFilter;
		}
		set
		{
			if (Set(ref _situationFilter, value ?? "Todos", "SituationFilter"))
			{
				Filter();
			}
		}
	}

	public string ActiveIn
	{
		get
		{
			return _activeIn;
		}
		set
		{
			if (Set(ref _activeIn, value, "ActiveIn"))
			{
				Filter();
			}
		}
	}

	public string CountText
	{
		get
		{
			return _countText;
		}
		private set
		{
			Set(ref _countText, value, "CountText");
		}
	}

	public bool HasFilters
	{
		get
		{
			if (_search.Trim().Length <= 0 && !(_kindFilter != "Todos") && !(_situationFilter != "Todos"))
			{
				return _activeIn.Trim().Length > 0;
			}
			return true;
		}
	}

	public string NewAmount
	{
		get
		{
			return _newAmount;
		}
		set
		{
			Set(ref _newAmount, value, "NewAmount");
		}
	}

	public string NewName
	{
		get
		{
			return _newName;
		}
		set
		{
			Set(ref _newName, value, "NewName");
		}
	}

	public string Until
	{
		get
		{
			return _until;
		}
		set
		{
			Set(ref _until, value, "Until");
		}
	}

	public SeriesRow? Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (Set(ref _selected, value, "Selected"))
			{
				OnPropertyChanged("HasSelection");
				OnPropertyChanged("IsInstallment");
				OnPropertyChanged("IsFixed");
				OnPropertyChanged("SelectedTitle");
				LoadOccurrences();
			}
		}
	}

	public bool HasSelection => (object)_selected != null;

	public bool IsInstallment => (_selected?.Series.TotalInstallments).HasValue;

	public bool IsFixed
	{
		get
		{
			if ((object)_selected != null)
			{
				return !IsInstallment;
			}
			return false;
		}
	}

	public string SelectedTitle
	{
		get
		{
			if ((object)_selected != null)
			{
				return _selected.Kind + ": " + _selected.Name;
			}
			return "";
		}
	}

	public string MissingText
	{
		get
		{
			EntrySeries s = _selected?.Series;
			if ((object)s == null || !s.HasMissingInstallments)
			{
				return "Todas as parcelas já estão lançadas.";
			}
			return $"Faltam as parcelas {s.LastInstallment + 1} a {s.TotalInstallments} depois de {s.Last.SheetName}.";
		}
	}

	public RelayCommand CheckAllCommand { get; }

	public RelayCommand CheckNoneCommand { get; }

	public RelayCommand ChangeAmountCommand { get; }

	public RelayCommand RenameCommand { get; }

	public RelayCommand DeleteCommand { get; }

	public RelayCommand CompleteCommand { get; }

	public RelayCommand RepeatCommand { get; }

	public RelayCommand ClearFiltersCommand { get; }

	public EntriesManagerViewModel()
	{
		CheckAllCommand = new RelayCommand(() =>
		{
			foreach (OccurrenceRow occurrence in Occurrences)
			{
				occurrence.Checked = true;
			}
		});
		CheckNoneCommand = new RelayCommand(() =>
		{
			foreach (OccurrenceRow occurrence2 in Occurrences)
			{
				occurrence2.Checked = false;
			}
		});
		ChangeAmountCommand = new RelayCommand(ChangeAmount);
		RenameCommand = new RelayCommand(Rename);
		DeleteCommand = new RelayCommand(Delete);
		CompleteCommand = new RelayCommand(Complete);
		RepeatCommand = new RelayCommand(Repeat);
		ClearFiltersCommand = new RelayCommand(() =>
		{
			_search = "";
			_kindFilter = "Todos";
			_situationFilter = "Todos";
			_activeIn = "";
			OnPropertyChanged("Search");
			OnPropertyChanged("KindFilter");
			OnPropertyChanged("SituationFilter");
			OnPropertyChanged("ActiveIn");
			Filter();
		});
	}

	public void EditOccurrence(OccurrenceRow row)
	{
		Entry l = Svc.Finance.Get(row.Occurrence.Id);
		if (l != null && new EntryWindow(new EntryEdit(l.Id, (EntryKind)l.Kind, YearMonth.ParseKey(l.YearMonth), l.Description, l.Amount))
		{
			Owner = Application.Current.MainWindow
		}.ShowDialog() == true)
		{
			Load();
		}
	}

	public override void Load()
	{
		EntrySeries keep = _selected?.Series;
		_all = (from s in _manager.Series()
			select new SeriesRow(s)).ToList();
		Filter();
		_selected = null;
		Selected = (((object)keep == null) ? null : Series.FirstOrDefault((SeriesRow s) => s.Series.Kind == keep.Kind && string.Equals(s.Name, keep.Name, StringComparison.OrdinalIgnoreCase)));
	}

	private void Filter()
	{
		Series.Clear();
		string term = _search.Trim();
		YearMonth? month = null;
		if (_activeIn.Trim().Length > 0)
		{
			List<string> errors = new List<string>();
			YearMonth m = Input.Month(_activeIn, "Aparece em", errors);
			if (errors.Count == 0)
			{
				month = m;
			}
		}
		foreach (SeriesRow s in _all.Where((SeriesRow seriesRow) =>
		{
			if ((term.Length == 0 || seriesRow.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)) && (_kindFilter == "Todos" || seriesRow.Kind == _kindFilter) && (_situationFilter == "Todos" || seriesRow.Situation == _situationFilter))
			{
				if (month.HasValue)
				{
					YearMonth valueOrDefault = month.GetValueOrDefault();
					if (seriesRow.Series.First <= valueOrDefault)
					{
						return seriesRow.Series.Last >= valueOrDefault;
					}
					return false;
				}
				return true;
			}
			return false;
		}))
		{
			Series.Add(s);
		}
		CountText = ((Series.Count == _all.Count) ? $"{_all.Count} lançamento(s)" : $"{Series.Count} de {_all.Count} lançamento(s)");
		OnPropertyChanged("HasFilters");
	}

	private void LoadOccurrences()
	{
		Occurrences.Clear();
		Price.Clear();
		PriceInfo = "";
		PriceError = "";
		OnPropertyChanged("MissingText");
		if ((object)_selected == null)
		{
			return;
		}
		foreach (EntryOccurrence o in _manager.Occurrences(_selected.Series.Kind, _selected.Name))
		{
			Occurrences.Add(new OccurrenceRow(o));
		}
		NewAmount = Input.Num(_selected.LastAmount);
		NewName = _selected.Name;
		YearMonth last = _selected.Series.Last;
		Until = Input.Month((new YearMonth(last.Year, 12) > last) ? new YearMonth(last.Year, 12) : last.AddMonths(12));
		if (IsInstallment)
		{
			decimal? credit = _manager.CreditOf(_selected.Series.Kind, _selected.Name);
			string financed;
			if (credit.HasValue)
			{
				decimal c = credit.GetValueOrDefault();
				financed = Input.Num(c);
			}
			else
			{
				financed = "";
			}
			_financed = financed;
			OnPropertyChanged("Financed");
		}
		BuildPrice();
	}

	private void BuildPrice()
	{
		Price.Clear();
		PriceInfo = "";
		PriceError = "";
		EntrySeries s = _selected?.Series;
		if ((object)s == null)
		{
			return;
		}
		int? totalInstallments = s.TotalInstallments;
		if (!totalInstallments.HasValue)
		{
			return;
		}
		int count = totalInstallments.GetValueOrDefault();
		if (string.IsNullOrWhiteSpace(_financed))
		{
			PriceError = "Digite o valor financiado do contrato para ver a tabela.";
			return;
		}
		List<string> errors = new List<string>();
		decimal financed = Input.Dec(_financed, "Valor financiado", errors);
		if (errors.Count > 0)
		{
			PriceError = string.Join(" ", errors);
			return;
		}
		try
		{
			double rate = PriceTable.ImpliedRate(financed, s.LastAmount, count);
			List<PriceRow> rows = PriceTable.Build(financed, s.LastAmount, count);
			Dictionary<int, YearMonth> byNumber = (from o in Occurrences
				select (o: o, m: Regex.Match(o.Description, "(\\d+)\\s*/\\s*\\d+\\s*$")) into x
				where x.m.Success
				select x).ToDictionary(((OccurrenceRow o, Match m) x) => int.Parse(x.m.Groups[1].Value), ((OccurrenceRow o, Match m) x) => x.o.Occurrence.Month);
			if (byNumber.Count == 0)
			{
				PriceError = "Não achei o número das parcelas (ex.: 3/48) nos lançamentos.";
				return;
			}
			KeyValuePair<int, YearMonth> first = byNumber.MinBy((KeyValuePair<int, YearMonth> kv) => kv.Key);
			YearMonth today = YearMonth.Today;
			decimal balanceToday = financed;
			foreach (PriceRow r in rows)
			{
				YearMonth month = (byNumber.TryGetValue(r.Number, out var m) ? m : first.Value.AddMonths(r.Number - first.Key));
				bool paid = month < today;
				if (paid)
				{
					balanceToday = r.Balance;
				}
				Price.Add(new PriceViewRow(r.Number, month.SheetName, r.Installment, r.Interest, r.Amortization, r.Balance, paid));
			}
			decimal total = s.LastAmount * (decimal)count;
			PriceInfo = $"Juros reais: {rate * 100.0:0.00}% ao mês. Você paga {Money.Fmt(total)} no total, {Money.Fmt(total - financed)} só de juros. Saldo devedor hoje: {Money.Fmt(balanceToday)}.";
		}
		catch (ArgumentException ex)
		{
			PriceError = ex.Message;
		}
	}

	private List<Guid> CheckedIds()
	{
		List<Guid> list = (from o in Occurrences
			where o.Checked
			select o.Occurrence.Id).ToList();
		if (list.Count == 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Marque pelo menos um mês."));
		}
		return list;
	}

	private void ChangeAmount()
	{
		List<Guid> ids = CheckedIds();
		List<string> errors = new List<string>();
		decimal amount = Input.Dec(NewAmount, "Novo valor", errors, required: true, allowNegative: false, _selected?.LastAmount);
		Input.Check(errors);
		int n = _manager.ChangeAmount(ids, amount);
		Load();
		Dialogs.Info($"Valor alterado em {n} mês(es).");
	}

	private void Rename()
	{
		List<Guid> ids = CheckedIds();
		int n = _manager.Rename(ids, NewName);
		_selected = null;
		_all = (from s in _manager.Series()
			select new SeriesRow(s)).ToList();
		Filter();
		Selected = Series.FirstOrDefault((SeriesRow s) => string.Equals(s.Name, NewName.Trim(), StringComparison.OrdinalIgnoreCase));
		Dialogs.Info($"Nome alterado em {n} mês(es).");
	}

	private void Delete()
	{
		List<Guid> ids = CheckedIds();
		if (Dialogs.Confirm($"Excluir \"{_selected?.Name}\" de {ids.Count} mês(es)?"))
		{
			int n = _manager.Delete(ids);
			Load();
			Dialogs.Info($"Excluído de {n} mês(es).");
		}
	}

	private void Complete()
	{
		if ((object)_selected != null)
		{
			int n = _manager.CompleteInstallments(_selected.Series.Kind, _selected.Name);
			Load();
			Dialogs.Info($"{n} parcela(s) lançada(s).");
		}
	}

	private void Repeat()
	{
		if ((object)_selected != null)
		{
			List<string> errors = new List<string>();
			YearMonth until = Input.Month(Until, "Repetir até", errors);
			Input.Check(errors);
			int n = _manager.RepeatUntil(_selected.Series.Kind, _selected.Name, until);
			Load();
			Dialogs.Info((n == 0) ? "Nenhum mês novo: já estava lançado até lá." : $"Lançado em mais {n} mês(es).");
		}
	}
}
