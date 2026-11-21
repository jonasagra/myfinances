using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class VacationSimVM : ObservableObject
{
	private string _start = "";

	private string _days = "30";

	private string _salary = "";

	private string _average = "";

	private string _payrollLoan = "";

	private string _breakdown = "";

	private string _notice = "";

	private string _error = "";

	private bool _reserve = true;

	private bool _salaryManual;

	private bool _averageManual;

	private bool _payrollLoanManual;

	private VacationResult? _res;

	private PostedGroup? _posted;

	public ObservableCollection<PlanRow> Plan { get; } = new ObservableCollection<PlanRow>();

	public ObservableCollection<PostedGroup> PostedItems { get; } = new ObservableCollection<PostedGroup>();

	public string Start
	{
		get
		{
			return _start;
		}
		set
		{
			if (Set(ref _start, value, "Start"))
			{
				Calculate();
			}
		}
	}

	public string Days
	{
		get
		{
			return _days;
		}
		set
		{
			if (Set(ref _days, value, "Days"))
			{
				Calculate();
			}
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
			if (Set(ref _salary, value, "Salary"))
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

	public string PayrollLoan
	{
		get
		{
			return _payrollLoan;
		}
		set
		{
			if (Set(ref _payrollLoan, value, "PayrollLoan"))
			{
				_payrollLoanManual = true;
				Calculate();
			}
		}
	}

	public bool Reserve
	{
		get
		{
			return _reserve;
		}
		set
		{
			if (Set(ref _reserve, value, "Reserve"))
			{
				Calculate();
			}
		}
	}

	public string BreakdownText
	{
		get
		{
			return _breakdown;
		}
		private set
		{
			Set(ref _breakdown, value, "BreakdownText");
		}
	}

	public string Notice
	{
		get
		{
			return _notice;
		}
		private set
		{
			Set(ref _notice, value, "Notice");
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

	public PostedGroup? Posted
	{
		get
		{
			return _posted;
		}
		set
		{
			Set(ref _posted, value, "Posted");
		}
	}

	public RelayCommand PostCommand { get; }

	public RelayCommand UndoCommand { get; }

	public RelayCommand RestoreDefaultsCommand { get; }

	public VacationSimVM()
	{
		PostCommand = new RelayCommand(Post, () => (object)_res != null);
		UndoCommand = new RelayCommand(Undo, () => (object)Posted != null);
		RestoreDefaultsCommand = new RelayCommand(() =>
		{
			_salaryManual = (_averageManual = (_payrollLoanManual = false));
			Calculate();
		});
	}

	public void Load()
	{
		SimulatorsViewModel.Fill(PostedItems, from g in Svc.Finance.Groups(EntryOrigin.Vacation)
			select new PostedGroup(g.Group, g.Rows, g.Months));
		Posted = PostedItems.FirstOrDefault();
		Calculate();
	}

	private void SetIfNotManual(ref string field, string amount, string name)
	{
		field = amount;
		OnPropertyChanged(name);
	}

	private void Calculate()
	{
		_res = null;
		Plan.Clear();
		BreakdownText = "";
		Notice = "";
		Error = "";
		if (string.IsNullOrWhiteSpace(Start))
		{
			Error = "Digite o início das férias (dd/mm/aaaa).";
			return;
		}
		try
		{
			List<string> errors = new List<string>();
			DateOnly start = Input.Date(Start, "Início", errors);
			int days = Input.Int(Days, "Dias", errors, 1, 30);
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			YearMonth month1 = YearMonth.From(start).Next;
			if (!_salaryManual)
			{
				SetIfNotManual(ref _salary, Money.Num(SimulatorsViewModel.SalaryOn(month1)), "Salary");
			}
			if (!_averageManual)
			{
				SetIfNotManual(ref _average, Money.Num(Payroll.OvertimeAverage12Months(Svc.Finance.Earnings(), YearMonth.From(start))), "Average");
			}
			if (!_payrollLoanManual)
			{
				SetIfNotManual(ref _payrollLoan, Money.Num(Svc.Finance.PayrollLoansOfMonth(month1)), "PayrollLoan");
			}
			decimal salary = Input.Dec(Salary, "Salário", errors);
			decimal avg = Input.Dec(Average, "Média de extras", errors, required: false);
			decimal payrollLoan = Input.Dec(PayrollLoan, "Consignado", errors, required: false);
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			Preferences prefs = Svc.Config.Preferences();
			LegalParameters p = SimulatorsViewModel.Params(start.Year);
			decimal tv = (prefs.Payroll.TransportVoucherActive ? Money.R2(salary * prefs.Payroll.TransportVoucherPct / 100m) : 0m);
			_res = Vacation.Simulate(new VacationInput(start, days, salary, avg, payrollLoan, Reserve, tv, tv), p, Svc.Config.Calendar());
			BreakdownText = _res.Breakdown.ToString();
			Notice = _res.Notice ?? "";
			foreach (PlanItem i in _res.Plan)
			{
				Plan.Add(PlanRow.From(i));
			}
		}
		catch (ArgumentException ex)
		{
			Error = ex.Message;
		}
	}

	private void Post()
	{
		if ((object)_res != null && Dialogs.Confirm($"Lançar {_res.Plan.Count} linha(s) das férias de {_res.Key} nos meses? Dá para desfazer depois."))
		{
			Svc.Finance.ApplyPlan(_res.Plan, "Férias " + _res.Key, EntryOrigin.Vacation);
			Dialogs.Info("Férias lançadas.");
			Load();
		}
	}

	private void Undo()
	{
		if ((object)Posted != null && Dialogs.Confirm("Desfazer \"" + Posted.Group + "\"? As linhas somem de todos os meses."))
		{
			Svc.Finance.UndoGroup(Posted.Group);
			Load();
		}
	}
}
