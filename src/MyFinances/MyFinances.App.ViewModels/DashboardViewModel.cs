using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Loans;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class DashboardViewModel : ScreenViewModel
{
	private string _from = "";

	private string _until = "";

	private bool _with13th = true;

	private string _error = "";

	private decimal _revenue;

	private decimal _expenses;

	private decimal _balance;

	private decimal _finalRemaining;

	private decimal _finalRemainingWith;

	private decimal _finalRemainingWithout;

	private decimal _total13th;

	private decimal _currentRemaining;

	private string _currentLabel = "";

	private string _currentNote = "";

	private MonthSummary? _currentMonth;

	private string _parametersWarning = "";

	private LoanIndicators? _loanStats;

	private FinanceSummaryResult? _summary;

	public override string Title => "Painel";

	public string From
	{
		get
		{
			return _from;
		}
		set
		{
			Set(ref _from, value, "From");
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

	public string Error
	{
		get
		{
			return _error;
		}
		set
		{
			Set(ref _error, value, "Error");
		}
	}

	public bool With13th
	{
		get
		{
			return _with13th;
		}
		set
		{
			if (Set(ref _with13th, value, "With13th"))
			{
				OnPropertyChanged("Without13th");
				OnPropertyChanged("ScenarioText");
				Recalculate();
			}
		}
	}

	public bool Without13th
	{
		get
		{
			return !_with13th;
		}
		set
		{
			With13th = !value;
		}
	}

	public string ScenarioText
	{
		get
		{
			if (!With13th)
			{
				return "Cenário ativo: SEM 13º salário (as parcelas do 13º ficam de fora)";
			}
			return "Cenário ativo: COM 13º salário (as parcelas de NOV e DEZ entram nas receitas)";
		}
	}

	public ObservableCollection<DashboardMonthRow> Months { get; } = new ObservableCollection<DashboardMonthRow>();

	public ObservableCollection<SeriesBar> LoanSeries { get; } = new ObservableCollection<SeriesBar>();

	public ObservableCollection<DashboardMonthRow> ChartMonths { get; } = new ObservableCollection<DashboardMonthRow>();

	public decimal Revenue
	{
		get
		{
			return _revenue;
		}
		private set
		{
			Set(ref _revenue, value, "Revenue");
		}
	}

	public decimal Expenses
	{
		get
		{
			return _expenses;
		}
		private set
		{
			Set(ref _expenses, value, "Expenses");
		}
	}

	public decimal Balance
	{
		get
		{
			return _balance;
		}
		private set
		{
			Set(ref _balance, value, "Balance");
		}
	}

	public decimal FinalRemaining
	{
		get
		{
			return _finalRemaining;
		}
		private set
		{
			Set(ref _finalRemaining, value, "FinalRemaining");
		}
	}

	public decimal FinalRemainingWith13th
	{
		get
		{
			return _finalRemainingWith;
		}
		private set
		{
			Set(ref _finalRemainingWith, value, "FinalRemainingWith13th");
		}
	}

	public decimal FinalRemainingWithout13th
	{
		get
		{
			return _finalRemainingWithout;
		}
		private set
		{
			Set(ref _finalRemainingWithout, value, "FinalRemainingWithout13th");
		}
	}

	public decimal Total13th
	{
		get
		{
			return _total13th;
		}
		private set
		{
			Set(ref _total13th, value, "Total13th");
		}
	}

	public decimal CurrentRemaining
	{
		get
		{
			return _currentRemaining;
		}
		private set
		{
			Set(ref _currentRemaining, value, "CurrentRemaining");
		}
	}

	public string CurrentLabel
	{
		get
		{
			return _currentLabel;
		}
		private set
		{
			Set(ref _currentLabel, value, "CurrentLabel");
		}
	}

	public string CurrentNote
	{
		get
		{
			return _currentNote;
		}
		private set
		{
			Set(ref _currentNote, value, "CurrentNote");
		}
	}

	public string ParametersWarning
	{
		get
		{
			return _parametersWarning;
		}
		private set
		{
			Set(ref _parametersWarning, value, "ParametersWarning");
		}
	}

	public LoanIndicators? LoanStats
	{
		get
		{
			return _loanStats;
		}
		private set
		{
			Set(ref _loanStats, value, "LoanStats");
		}
	}

	public RelayCommand RefreshCommand { get; }

	public RelayCommand ThisYearCommand { get; }

	public RelayCommand Next12Command { get; }

	public RelayCommand AllCommand { get; }

	public RelayCommand OpenMonthCommand { get; } = new RelayCommand((object? p) =>
	{
		if (p is DashboardMonthRow dashboardMonthRow)
		{
			MainViewModel.Instance?.OpenMonth(dashboardMonthRow.Ym);
		}
	});

	public string PersonalFormulas => "Líquido = bruto (salário + proventos) − descontos (INSS, VT, faltas, IRRF e lançados).\nSaldo do mês = líquido + entradas (+ parcela do 13º no cenário com 13º) − despesas.\nRestante = restante do mês anterior + saldo do mês (começa no saldo inicial das Configurações).\nRestante sem 13º = restante − todas as parcelas de 13º recebidas até o mês.\nMeses futuros são projeção: usam o salário vigente e os lançamentos já cadastrados (parcelas, recorrentes).";

	public DashboardViewModel()
	{
		DashboardViewModel dashboardViewModel = this;
		Preferences prefs = Svc.Config.Preferences();
		_with13th = prefs.DefaultScenario == ThirteenthScenario.With13th;
		YearMonth today = YearMonth.Today;
		_from = Input.Month(YearStart(today.Year));
		_until = Input.Month(new YearMonth(today.Year, 12));
		RefreshCommand = new RelayCommand(Load);
		ThisYearCommand = new RelayCommand(() =>
		{
			dashboardViewModel.From = Input.Month(YearStart(today.Year));
			dashboardViewModel.Until = Input.Month(new YearMonth(today.Year, 12));
			dashboardViewModel.Load();
		});
		Next12Command = new RelayCommand(() =>
		{
			dashboardViewModel.From = Input.Month(today);
			dashboardViewModel.Until = Input.Month(today.AddMonths(11));
			dashboardViewModel.Load();
		});
		AllCommand = new RelayCommand(() =>
		{
			YearMonth c = (YearMonth.TryParseKey(Svc.Config.Preferences().FirstMonth, out var c2) ? c2 : today);
			dashboardViewModel.From = Input.Month(c);
			dashboardViewModel.Until = Input.Month(today.AddMonths(12));
			dashboardViewModel.Load();
		});
	}

	private static YearMonth YearStart(int year)
	{
		YearMonth january = new YearMonth(year, 1);
		if (!YearMonth.TryParseKey(Svc.Config.Preferences().FirstMonth, out var first) || !(first > january) || first.Year != year)
		{
			return january;
		}
		return first;
	}

	public override void Load()
	{
		List<string> errors = new List<string>();
		YearMonth from = Input.Month(From, "De", errors);
		YearMonth until = Input.Month(Until, "Até", errors);
		if (errors.Count == 0 && until < from)
		{
			errors.Add("O mês final precisa ser depois do inicial.");
		}
		if (errors.Count == 0 && YearMonth.Difference(from, until) > 120)
		{
			errors.Add("Período máximo: 10 anos.");
		}
		Error = string.Join(" ", errors);
		if (errors.Count > 0)
		{
			return;
		}
		_summary = Svc.Finance.Summary(from, until);
		YearMonth today = YearMonth.Today;
		_currentMonth = Svc.Finance.Summary(today, today).Months.FirstOrDefault();
		DateTime? checkedAt = Svc.Finance.LastAdjustment(today);
		CurrentLabel = ((!checkedAt.HasValue) ? ("Previsto para o fim de " + today.SheetName) : "Saldo atual");
		string currentNote;
		if (checkedAt.HasValue)
		{
			DateTime d = checkedAt.GetValueOrDefault();
			currentNote = $"Conferido com o banco em {d:dd/MM}";
		}
		else
		{
			currentNote = "Confira com o banco em Meses e lançamentos";
		}
		CurrentNote = currentNote;
		List<int> estimatedYears = (from a in Enumerable.Range(@from.Year, until.Year - @from.Year + 1)
			where Svc.Config.ParametersForYear(a).Estimated
			select a).ToList();
		ParametersWarning = ((estimatedYears.Count > 0) ? ("Tabelas legais de " + string.Join(", ", estimatedYears) + " ainda não cadastradas: usando as do ano anterior (ESTIMATIVA). Atualize em Configurações quando forem publicadas.") : "");
		Recalculate();
		Preferences prefs = Svc.Config.Preferences();
		LoanStats = Indicators.Calculate(Svc.Loans.AllForIndicators(), from.FirstDay, until.LastDay, DateOnly.FromDateTime(DateTime.Today), prefs.CapitalLimit);
		LoanSeries.Clear();
		decimal max = LoanStats.Series.Select((LoanMonthlySeries loanMonthlySeries) => Math.Max(loanMonthlySeries.ExpectedInterest, loanMonthlySeries.InterestAndChargesReceived)).DefaultIfEmpty(0m).Max();
		foreach (LoanMonthlySeries s in LoanStats.Series)
		{
			LoanSeries.Add(new SeriesBar(s.Month.SheetName, Height(s.ExpectedInterest, max), Height(s.InterestAndChargesReceived, max), s.ExpectedInterest, s.InterestAndChargesReceived));
		}
	}

	private static double Height(decimal v, decimal max)
	{
		if (!(max <= 0m))
		{
			return (double)(Math.Max(0m, v) / max) * 120.0;
		}
		return 0.0;
	}

	private void Recalculate()
	{
		if ((object)_summary == null)
		{
			return;
		}
		ThirteenthScenario c = ((!With13th) ? ThirteenthScenario.Without13th : ThirteenthScenario.With13th);
		IReadOnlyList<MonthSummary> months = _summary.Months;
		Months.Clear();
		ChartMonths.Clear();
		YearMonth today = YearMonth.Today;
		decimal max = months.Select((MonthSummary monthSummary) => Math.Abs(monthSummary.Balance(c))).DefaultIfEmpty(0m).Max();
		foreach (MonthSummary m in months)
		{
			DashboardMonthRow row = new DashboardMonthRow(m.Month, m.Payroll.Net, m.Inflows, (c == ThirteenthScenario.With13th) ? m.Thirteenth : 0m, m.Expenses, m.Balance(c), m.Remaining(c), m.Remaining((c == ThirteenthScenario.With13th) ? ThirteenthScenario.Without13th : ThirteenthScenario.With13th), (max <= 0m) ? 0.0 : ((double)(Math.Abs(m.Balance(c)) / max) * 110.0), m.Balance(c) < 0m, m.Month > today);
			Months.Add(row);
			ChartMonths.Add(row);
		}
		CurrentRemaining = _currentMonth?.Remaining(c) ?? 0m;
		Revenue = months.Sum((MonthSummary monthSummary) => monthSummary.Revenue(c));
		Expenses = months.Sum((MonthSummary monthSummary) => monthSummary.Expenses);
		Balance = months.Sum((MonthSummary monthSummary) => monthSummary.Balance(c));
		decimal finalRemaining;
		if (months.Count <= 0)
		{
			finalRemaining = 0m;
		}
		else
		{
			finalRemaining = months[months.Count - 1].Remaining(c);
		}
		FinalRemaining = finalRemaining;
		decimal finalRemainingWith13th;
		if (months.Count <= 0)
		{
			finalRemainingWith13th = 0m;
		}
		else
		{
			finalRemainingWith13th = months[months.Count - 1].RemainingWith13th;
		}
		FinalRemainingWith13th = finalRemainingWith13th;
		decimal finalRemainingWithout13th;
		if (months.Count <= 0)
		{
			finalRemainingWithout13th = 0m;
		}
		else
		{
			finalRemainingWithout13th = months[months.Count - 1].RemainingWithout13th;
		}
		FinalRemainingWithout13th = finalRemainingWithout13th;
		Total13th = months.Sum((MonthSummary monthSummary) => monthSummary.Thirteenth);
	}
}
