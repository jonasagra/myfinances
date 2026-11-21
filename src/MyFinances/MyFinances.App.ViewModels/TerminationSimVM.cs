using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data;

namespace MyFinances.App.ViewModels;

public sealed class TerminationSimVM : ObservableObject
{
	public sealed record Option<T>(T Amount, string Name);

	private Option<TerminationKind> _kind;

	private Option<NoticeKind> _notice;

	private Option<ThirteenthAlreadyReceived> _thirteenth;

	private Option<int> _priorClaim;

	private string _exitDate = "";

	private string _hire = "";

	private string _salary = "";

	private string _average = "";

	private string _p1 = "";

	private string _vacation = "";

	private string _fgts = "";

	private string _payrollLoan = "";

	private bool _birthdayWithdrawal;

	private bool _collateral = true;

	private bool _worksNotice = true;

	private string _severanceItems = "";

	private string _others = "";

	private string _warningsText = "";

	private string _error = "";

	private bool _salaryManual;

	private bool _averageManual;

	private bool _p1Manual;

	private bool _vacationManual;

	private bool _payrollLoanManual;

	private bool _thirteenthManual;

	private TerminationResult? _res;

	public IReadOnlyList<Option<TerminationKind>> Kinds { get; } = (from t in Enum.GetValues<TerminationKind>()
		select new Option<TerminationKind>(t, Termination.Name(t))).ToList();

	public IReadOnlyList<Option<NoticeKind>> Warnings { get; } = new _003C_003Ez__ReadOnlyArray<Option<NoticeKind>>(new Option<NoticeKind>[2]
	{
		new Option<NoticeKind>(NoticeKind.Indemnified, "Indenizado (sai no dia)"),
		new Option<NoticeKind>(NoticeKind.Worked, "Trabalhado (30 dias)")
	});

	public IReadOnlyList<Option<ThirteenthAlreadyReceived>> ThirteenthOptions { get; } = new _003C_003Ez__ReadOnlyArray<Option<ThirteenthAlreadyReceived>>(new Option<ThirteenthAlreadyReceived>[3]
	{
		new Option<ThirteenthAlreadyReceived>(ThirteenthAlreadyReceived.Nothing, "Nada"),
		new Option<ThirteenthAlreadyReceived>(ThirteenthAlreadyReceived.FirstInstallment, "1ª parcela"),
		new Option<ThirteenthAlreadyReceived>(ThirteenthAlreadyReceived.All, "Tudo")
	});

	public IReadOnlyList<Option<int>> PriorClaims { get; } = new _003C_003Ez__ReadOnlyArray<Option<int>>(new Option<int>[3]
	{
		new Option<int>(0, "Nunca pediu"),
		new Option<int>(1, "Pediu 1 vez"),
		new Option<int>(2, "Pediu 2 vezes ou mais")
	});

	public Option<TerminationKind> Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			if (Set(ref _kind, value, "Kind"))
			{
				OnPropertyChanged("IsResignation");
				Calculate();
			}
		}
	}

	public Option<NoticeKind> Notice
	{
		get
		{
			return _notice;
		}
		set
		{
			if (Set(ref _notice, value, "Notice"))
			{
				Calculate();
			}
		}
	}

	public Option<ThirteenthAlreadyReceived> Thirteenth
	{
		get
		{
			return _thirteenth;
		}
		set
		{
			if (Set(ref _thirteenth, value, "Thirteenth"))
			{
				_thirteenthManual = true;
				Calculate();
			}
		}
	}

	public Option<int> PriorClaim
	{
		get
		{
			return _priorClaim;
		}
		set
		{
			if (Set(ref _priorClaim, value, "PriorClaim"))
			{
				Calculate();
			}
		}
	}

	public string ExitDate
	{
		get
		{
			return _exitDate;
		}
		set
		{
			if (Set(ref _exitDate, value, "ExitDate"))
			{
				Calculate();
			}
		}
	}

	public string HireDate
	{
		get
		{
			return _hire;
		}
		set
		{
			if (Set(ref _hire, value, "HireDate"))
			{
				Calculate();
			}
		}
	}

	public string BaseSalary
	{
		get
		{
			return _salary;
		}
		set
		{
			if (Set(ref _salary, value, "BaseSalary"))
			{
				_salaryManual = true;
				Calculate();
			}
		}
	}

	public string Average
	{
		get
		{
			return _average;
		}
		set
		{
			if (Set(ref _average, value, "Average"))
			{
				_averageManual = true;
				Calculate();
			}
		}
	}

	public string FirstInstallment
	{
		get
		{
			return _p1;
		}
		set
		{
			if (Set(ref _p1, value, "FirstInstallment"))
			{
				_p1Manual = true;
				Calculate();
			}
		}
	}

	public string DueVacation
	{
		get
		{
			return _vacation;
		}
		set
		{
			if (Set(ref _vacation, value, "DueVacation"))
			{
				_vacationManual = true;
				Calculate();
			}
		}
	}

	public string FgtsBalance
	{
		get
		{
			return _fgts;
		}
		set
		{
			if (Set(ref _fgts, value, "FgtsBalance"))
			{
				Calculate();
			}
		}
	}

	public string PayrollLoanBalance
	{
		get
		{
			return _payrollLoan;
		}
		set
		{
			if (Set(ref _payrollLoan, value, "PayrollLoanBalance"))
			{
				_payrollLoanManual = true;
				Calculate();
			}
		}
	}

	public bool BirthdayWithdrawal
	{
		get
		{
			return _birthdayWithdrawal;
		}
		set
		{
			if (Set(ref _birthdayWithdrawal, value, "BirthdayWithdrawal"))
			{
				Calculate();
			}
		}
	}

	public bool Collateral
	{
		get
		{
			return _collateral;
		}
		set
		{
			if (Set(ref _collateral, value, "Collateral"))
			{
				Calculate();
			}
		}
	}

	public bool WorksNotice
	{
		get
		{
			return _worksNotice;
		}
		set
		{
			if (Set(ref _worksNotice, value, "WorksNotice"))
			{
				Calculate();
			}
		}
	}

	public bool IsResignation => Kind.Amount == TerminationKind.Resignation;

	public string SeveranceItems
	{
		get
		{
			return _severanceItems;
		}
		private set
		{
			Set(ref _severanceItems, value, "SeveranceItems");
		}
	}

	public string Others
	{
		get
		{
			return _others;
		}
		private set
		{
			Set(ref _others, value, "Others");
		}
	}

	public string WarningsText
	{
		get
		{
			return _warningsText;
		}
		private set
		{
			Set(ref _warningsText, value, "WarningsText");
		}
	}

	public string Error
	{
		get
		{
			return _error;
		}
		private set
		{
			Set(ref _error, value, "Error");
		}
	}

	public RelayCommand PostCommand { get; }

	public RelayCommand RestoreDefaultsCommand { get; }

	public TerminationSimVM()
	{
		_kind = Kinds[0];
		_notice = Warnings[0];
		_thirteenth = ThirteenthOptions[0];
		_priorClaim = PriorClaims[0];
		PostCommand = new RelayCommand(Post, () => (object)_res != null);
		RestoreDefaultsCommand = new RelayCommand(() =>
		{
			_salaryManual = (_averageManual = (_p1Manual = (_vacationManual = (_payrollLoanManual = (_thirteenthManual = false)))));
			Calculate();
		});
	}

	public void Fill()
	{
		DateOnly? hire = Svc.Config.Preferences().Payroll.HireDate;
		string hire2;
		if (hire.HasValue)
		{
			DateOnly a = hire.GetValueOrDefault();
			hire2 = Input.Date(a);
		}
		else
		{
			hire2 = "";
		}
		_hire = hire2;
		if (string.IsNullOrEmpty(_exitDate))
		{
			_exitDate = Input.Date(DateOnly.FromDateTime(DateTime.Today));
		}
		OnPropertyChanged("HireDate");
		OnPropertyChanged("ExitDate");
		Calculate();
	}

	private void Auto(ref string field, string amount, string name)
	{
		field = amount;
		OnPropertyChanged(name);
	}

	private void Calculate()
	{
		_res = null;
		SeveranceItems = "";
		Others = "";
		WarningsText = "";
		Error = "";
		try
		{
			List<string> errors = new List<string>();
			DateOnly outgoing = Input.Date(ExitDate, "Último dia trabalhado", errors);
			DateOnly hire = Input.Date(HireDate, "Admissão", errors);
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			YearMonth nextMonth = YearMonth.From(outgoing).Next;
			if (!_salaryManual)
			{
				Auto(ref _salary, Money.Num(SimulatorsViewModel.SalaryOn(nextMonth)), "BaseSalary");
			}
			if (!_averageManual)
			{
				Auto(ref _average, Money.Num(Payroll.OvertimeAverage12Months(Svc.Finance.Earnings(), YearMonth.From(outgoing))), "Average");
			}
			if (!_p1Manual)
			{
				Auto(ref _p1, Money.Num(Money.R2(SimulatorsViewModel.SalaryOn(new YearMonth(outgoing.Year, 11)) / 2m)), "FirstInstallment");
			}
			if (!_thirteenthManual)
			{
				Option<ThirteenthAlreadyReceived> d;
				if (outgoing >= new DateOnly(outgoing.Year, 12, 20))
				{
					d = ThirteenthOptions[2];
				}
				else
				{
					d = ((outgoing >= new DateOnly(outgoing.Year, 11, 30)) ? ThirteenthOptions[1] : ThirteenthOptions[0]);
				}
				_thirteenth = d;
				OnPropertyChanged("Thirteenth");
			}
			if (!_payrollLoanManual)
			{
				Auto(ref _payrollLoan, Money.Num(Svc.Finance.PayrollLoanInstallmentsSince(nextMonth)), "PayrollLoanBalance");
			}
			if (!_vacationManual)
			{
				int taken = 1 + Svc.Finance.VacationsPostedUntil(outgoing);
				Auto(ref _vacation, Termination.SuggestDueVacation(hire, outgoing, taken).ToString(), "DueVacation");
			}
			TerminationInput e = new TerminationInput
			{
				Kind = Kind.Amount,
				Notice = Notice.Amount,
				LastDay = outgoing,
				HireDate = hire,
				Salary = Input.Dec(BaseSalary, "Salário base", errors),
				OvertimeAverage = Input.Dec(Average, "Média de extras", errors, required: false),
				Thirteenth = Thirteenth.Amount,
				FirstInstallmentPaid = Input.Dec(FirstInstallment, "1ª parcela paga", errors, required: false),
				DueVacationDays = Input.Int(string.IsNullOrWhiteSpace(DueVacation) ? "0" : DueVacation, "Férias vencidas (dias)", errors, 0, 180),
				BenefitPriorClaims = PriorClaim.Amount,
				FgtsBalance = Input.Dec(FgtsBalance, "Saldo do FGTS", errors, required: false),
				BirthdayWithdrawal = BirthdayWithdrawal,
				PayrollLoanBalance = Input.Dec(PayrollLoanBalance, "Saldo do consignado", errors, required: false),
				PayrollLoanFgtsCollateral = Collateral,
				WorksNoticeOnResignation = WorksNotice
			};
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			_res = Termination.Simulate(e, SimulatorsViewModel.Params(outgoing.Year));
			SeveranceItems = _res.SeveranceItems.ToString();
			Others = _res.Others.ToString();
			WarningsText = string.Join("\n", _res.Warnings);
		}
		catch (ArgumentException ex)
		{
			Error = ex.Message;
		}
	}

	private void Post()
	{
		if ((object)_res != null)
		{
			List<string> errors = new List<string>();
			DateOnly outgoing = Input.Date(ExitDate, "Saída", errors);
			Input.Check(errors);
			List<PlanItem> items = new List<PlanItem>();
			YearMonth severanceMonth = YearMonth.From(_res.PaymentDeadline);
			if (_res.SeveranceNet > 0m)
			{
				items.Add(new PlanItem(severanceMonth, EntryKind.Inflow, "Demissão: rescisão (líquido)", _res.SeveranceNet));
			}
			if (_res.FgtsWithdrawal > 0m)
			{
				items.Add(new PlanItem(severanceMonth, EntryKind.Inflow, "Demissão: saque do FGTS", _res.FgtsWithdrawal));
			}
			for (int k = 1; k <= _res.BenefitInstallments; k++)
			{
				items.Add(new PlanItem(YearMonth.From(outgoing.AddDays(7 + 30 * k)), EntryKind.Inflow, $"Demissão: seguro-desemprego {k}/{_res.BenefitInstallments}", _res.BenefitInstallmentAmount));
			}
			if (_res.RemainingPayrollLoan > 0m)
			{
				items.Add(new PlanItem(YearMonth.From(outgoing).Next, EntryKind.Expense, "Demissão: consignado que sobrou (boleto)", _res.RemainingPayrollLoan));
			}
			if (items.Count == 0)
			{
				Dialogs.Info("Nada a lançar.");
			}
			else if (Dialogs.Confirm($"Lançar {items.Count} linha(s) previstas da demissão?\n\nObs.: o salário dos meses seguintes NÃO é zerado automaticamente. " + "Ajuste em Meses e lançamentos se a demissão acontecer."))
			{
				Svc.Finance.ApplyPlan(items, $"Demissão {outgoing:dd/MM/yyyy}", EntryOrigin.Termination);
				Dialogs.Info("Lançado. Para desfazer, selecione uma das linhas em Meses e lançamentos e use \"Desfazer grupo\".");
			}
		}
	}
}
