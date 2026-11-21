using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Loans;

namespace MyFinances.App.ViewModels;

public sealed class LoanTermsForm : ObservableObject
{
	private string _capital = "";

	private string _date = "";

	private string _first = "";

	private string _installmentCount = "6";

	private string _rate = "10";

	private string _gracePeriods = "0";

	private string _fee = "0";

	private string _penalty = "2";

	private string _lateInterest = "1";

	private string _tolerance = "0";

	private string _risk = "90";

	private string _provision = "100";

	private Option<Frequency> _frequency;

	private Option<AmortizationMethod> _method;

	private Option<GraceKind> _grace;

	public IReadOnlyList<Option<Frequency>> Frequencies { get; } = new _003C_003Ez__ReadOnlyArray<Option<Frequency>>(new Option<Frequency>[4]
	{
		new Option<Frequency>(MyFinances.Core.Loans.Frequency.Monthly, "Mensal"),
		new Option<Frequency>(MyFinances.Core.Loans.Frequency.Biweekly, "Quinzenal"),
		new Option<Frequency>(MyFinances.Core.Loans.Frequency.Weekly, "Semanal"),
		new Option<Frequency>(MyFinances.Core.Loans.Frequency.Daily, "Diária")
	});

	public IReadOnlyList<Option<AmortizationMethod>> Methods { get; } = (from m in Enum.GetValues<AmortizationMethod>()
		select new Option<AmortizationMethod>(m, Schedule.MethodName(m))).ToList();

	public IReadOnlyList<Option<GraceKind>> GraceTypes { get; } = new _003C_003Ez__ReadOnlyArray<Option<GraceKind>>(new Option<GraceKind>[3]
	{
		new Option<GraceKind>(GraceKind.None, "Sem carência"),
		new Option<GraceKind>(GraceKind.PaysInterest, "Carência pagando só juros"),
		new Option<GraceKind>(GraceKind.NoPayment, "Carência sem pagamento")
	});

	public string Capital
	{
		get
		{
			return _capital;
		}
		set
		{
			Set(ref _capital, value, "Capital");
		}
	}

	public string ContractDate
	{
		get
		{
			return _date;
		}
		set
		{
			Set(ref _date, value, "ContractDate");
		}
	}

	public string FirstDueDate
	{
		get
		{
			return _first;
		}
		set
		{
			Set(ref _first, value, "FirstDueDate");
		}
	}

	public string Installments
	{
		get
		{
			return _installmentCount;
		}
		set
		{
			Set(ref _installmentCount, value, "Installments");
		}
	}

	public string Rate
	{
		get
		{
			return _rate;
		}
		set
		{
			Set(ref _rate, value, "Rate");
		}
	}

	public Option<Frequency> Frequency
	{
		get
		{
			return _frequency;
		}
		set
		{
			if (Set(ref _frequency, value, "Frequency"))
			{
				OnPropertyChanged("RateLabel");
			}
		}
	}

	public string RateLabel => "Juros (%) " + Schedule.PeriodName(Frequency.Amount);

	public Option<AmortizationMethod> Method
	{
		get
		{
			return _method;
		}
		set
		{
			Set(ref _method, value, "Method");
		}
	}

	public Option<GraceKind> Grace
	{
		get
		{
			return _grace;
		}
		set
		{
			Set(ref _grace, value, "Grace");
		}
	}

	public string GracePeriods
	{
		get
		{
			return _gracePeriods;
		}
		set
		{
			Set(ref _gracePeriods, value, "GracePeriods");
		}
	}

	public string Fee
	{
		get
		{
			return _fee;
		}
		set
		{
			Set(ref _fee, value, "Fee");
		}
	}

	public string Penalty
	{
		get
		{
			return _penalty;
		}
		set
		{
			Set(ref _penalty, value, "Penalty");
		}
	}

	public string LateInterest
	{
		get
		{
			return _lateInterest;
		}
		set
		{
			Set(ref _lateInterest, value, "LateInterest");
		}
	}

	public string Tolerance
	{
		get
		{
			return _tolerance;
		}
		set
		{
			Set(ref _tolerance, value, "Tolerance");
		}
	}

	public string Risk
	{
		get
		{
			return _risk;
		}
		set
		{
			Set(ref _risk, value, "Risk");
		}
	}

	public string Provision
	{
		get
		{
			return _provision;
		}
		set
		{
			Set(ref _provision, value, "Provision");
		}
	}

	public LoanTermsForm()
	{
		_frequency = Frequencies[0];
		_method = Methods[0];
		_grace = GraceTypes[0];
		DateOnly today = DateOnly.FromDateTime(DateTime.Today);
		_date = Input.Date(today);
		_first = Input.Date(today.AddMonths(1));
		LateRules a = Svc.Config.Preferences().DefaultLateRules;
		_penalty = a.PenaltyPct.ToString("0.##", Money.PtBr);
		_lateInterest = a.MonthlyLateInterestPct.ToString("0.##", Money.PtBr);
		_tolerance = a.ToleranceDays.ToString();
		_risk = a.DaysAtRisk.ToString();
		_provision = a.LossProvisionPct.ToString("0.##", Money.PtBr);
	}

	public LoanTerms Read(List<string> errors, bool requireCapital = true)
	{
		GraceKind grace = Grace.Amount;
		int gracePeriods = Input.Int(GracePeriods, "Períodos de carência", errors, 0, 120);
		if (grace == GraceKind.None)
		{
			gracePeriods = 0;
		}
		return new LoanTerms
		{
			Principal = (requireCapital ? Input.Dec(Capital, "Capital emprestado", errors) : 0m),
			ContractDate = Input.Date(ContractDate, "Data de contratação", errors),
			FirstDueDate = Input.Date(FirstDueDate, "1º vencimento", errors),
			InstallmentCount = Input.Int(Installments, "Parcelas", errors, 1, 600),
			InterestRatePct = Input.Dec(Rate, "Taxa de juros", errors, required: false),
			Frequency = Frequency.Amount,
			Method = Method.Amount,
			GracePeriods = gracePeriods,
			GraceKind = ((gracePeriods != 0) ? grace : GraceKind.None),
			SetupFee = Input.Dec(Fee, "Tarifa", errors, required: false),
			LateRules = new LateRules
			{
				PenaltyPct = Input.Dec(Penalty, "Multa (%)", errors, required: false),
				MonthlyLateInterestPct = Input.Dec(LateInterest, "Juros de atraso (% ao mês)", errors, required: false),
				ToleranceDays = Input.Int(Tolerance, "Tolerância (dias)", errors, 0, 60),
				DaysAtRisk = Input.Int(Risk, "Dias para inadimplência", errors, 1, 3650),
				LossProvisionPct = Input.Dec(Provision, "Provisão de perda (%)", errors, required: false)
			}
		};
	}
}
