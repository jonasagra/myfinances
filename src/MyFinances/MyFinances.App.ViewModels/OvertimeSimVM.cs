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

public sealed class OvertimeSimVM : ObservableObject
{
	private string _salary = "";

	private string _workload = "180";

	private string _dueDate = "";

	private string _regularDays = "0";

	private string _regularOvertime = "1:45";

	private string _holidayCount = "0";

	private string _holidayHours = "6:00";

	private string _holidayOvertime = "0:00";

	private string _breakdown = "";

	private string _warnings = "";

	private string _error = "";

	private bool _salaryManual;

	private OvertimeResult? _res;

	private PostedGroup? _posted;

	public ObservableCollection<PlanRow> Plan { get; } = new ObservableCollection<PlanRow>();

	public ObservableCollection<PostedGroup> PostedItems { get; } = new ObservableCollection<PostedGroup>();

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

	public string Workload
	{
		get
		{
			return _workload;
		}
		set
		{
			if (Set(ref _workload, value, "Workload"))
			{
				Calculate();
			}
		}
	}

	public string DueDate
	{
		get
		{
			return _dueDate;
		}
		set
		{
			if (Set(ref _dueDate, value, "DueDate"))
			{
				Calculate();
			}
		}
	}

	public string RegularDays
	{
		get
		{
			return _regularDays;
		}
		set
		{
			if (Set(ref _regularDays, value, "RegularDays"))
			{
				Calculate();
			}
		}
	}

	public string RegularOvertime
	{
		get
		{
			return _regularOvertime;
		}
		set
		{
			if (Set(ref _regularOvertime, value, "RegularOvertime"))
			{
				Calculate();
			}
		}
	}

	public string Holidays
	{
		get
		{
			return _holidayCount;
		}
		set
		{
			if (Set(ref _holidayCount, value, "Holidays"))
			{
				Calculate();
			}
		}
	}

	public string HolidayHours
	{
		get
		{
			return _holidayHours;
		}
		set
		{
			if (Set(ref _holidayHours, value, "HolidayHours"))
			{
				Calculate();
			}
		}
	}

	public string HolidayOvertime
	{
		get
		{
			return _holidayOvertime;
		}
		set
		{
			if (Set(ref _holidayOvertime, value, "HolidayOvertime"))
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

	public string Warnings
	{
		get
		{
			return _warnings;
		}
		private set
		{
			Set(ref _warnings, value, "Warnings");
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

	public OvertimeSimVM()
	{
		PostCommand = new RelayCommand(Post, () => (object)_res != null && _res.Plan.Count > 0);
		UndoCommand = new RelayCommand(() =>
		{
			if ((object)Posted != null && Dialogs.Confirm("Desfazer \"" + Posted.Group + "\"?"))
			{
				Svc.Finance.UndoGroup(Posted.Group);
				Load();
			}
		}, () => (object)Posted != null);
	}

	public void Load()
	{
		_workload = Svc.Config.Preferences().Payroll.MonthlyHours.ToString();
		OnPropertyChanged("Workload");
		if (string.IsNullOrEmpty(_dueDate))
		{
			_dueDate = Input.Month(YearMonth.Today);
			OnPropertyChanged("DueDate");
		}
		SimulatorsViewModel.Fill(PostedItems, from g in Svc.Finance.Groups(EntryOrigin.Overtime)
			select new PostedGroup(g.Group, g.Rows, g.Months));
		Posted = PostedItems.FirstOrDefault();
		Calculate();
	}

	private void Calculate()
	{
		_res = null;
		Plan.Clear();
		BreakdownText = "";
		Warnings = "";
		Error = "";
		try
		{
			List<string> errors = new List<string>();
			YearMonth due = Input.Month(DueDate, "Vencimento no banco (mm/aaaa)", errors);
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			YearMonth payMonth = due.Next;
			if (!_salaryManual)
			{
				_salary = Money.Num(SimulatorsViewModel.SalaryOn(payMonth));
				OnPropertyChanged("Salary");
			}
			decimal salary = Input.Dec(Salary, "Salário", errors);
			int hrs = Input.Int(Workload, "Jornada mensal", errors, 1, 300);
			int dn = Input.Int(RegularDays, "Dias normais", errors, 0, 31);
			int df = Input.Int(Holidays, "Feriados trabalhados", errors, 0, 31);
			int regularOt = 0;
			int holidayHrs = 0;
			int holidayOt = 0;
			if (!Overtime.TryParseHours(RegularOvertime, out regularOt))
			{
				errors.Add("HE por dia: use h:mm, ex.: 1:45.");
			}
			if (!Overtime.TryParseHours(HolidayHours, out holidayHrs))
			{
				errors.Add("Horas no feriado: use h:mm, ex.: 6:00.");
			}
			if (!Overtime.TryParseHours(HolidayOvertime, out holidayOt))
			{
				errors.Add("HE no feriado: use h:mm, ex.: 1:45.");
			}
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			MonthData data = Svc.Finance.GetMonth(payMonth);
			string key = $"{due.Month:00}/{due.Year % 100:00}";
			decimal otAlready = data.Entries.Where((Entry l) => l.Group == "HE " + key && l.Kind == 1).Sum((Entry l) => l.Amount);
			decimal currentBase = data.Payroll.Gross - data.Payroll.Absences - otAlready;
			_res = Overtime.Simulate(new OvertimeInput(salary, hrs, due, dn, regularOt, df, holidayHrs, holidayOt, currentBase), SimulatorsViewModel.Params(payMonth.Year), Svc.Config.Calendar());
			BreakdownText = _res.Breakdown.ToString();
			Warnings = string.Join("\n", _res.Warnings);
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
		if ((object)_res != null && Dialogs.Confirm($"Lançar as horas extras de {_res.Key} em Proventos de {_res.PaymentMonth}?"))
		{
			Svc.Finance.ApplyPlan(_res.Plan, "HE " + _res.Key, EntryOrigin.Overtime);
			Load();
		}
	}
}
