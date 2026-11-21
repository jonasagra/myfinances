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

public sealed class SalarySimVM : ObservableObject
{
	private string _month = Input.Month(YearMonth.Today);

	private string _draft = "";

	private string _months = "1";

	private string _breakdown = "";

	private string _error = "";

	private bool _retro;

	private RetroactiveResult? _res;

	public ObservableCollection<PlanRow> Plan { get; } = new ObservableCollection<PlanRow>();

	public string Month
	{
		get
		{
			return _month;
		}
		set
		{
			if (Set(ref _month, value, "Month"))
			{
				Calculate();
			}
		}
	}

	public string NewSalary
	{
		get
		{
			return _draft;
		}
		set
		{
			if (Set(ref _draft, value, "NewSalary"))
			{
				Calculate();
			}
		}
	}

	public bool Retroactive
	{
		get
		{
			return _retro;
		}
		set
		{
			if (Set(ref _retro, value, "Retroactive"))
			{
				Calculate();
			}
		}
	}

	public string MonthsLate
	{
		get
		{
			return _months;
		}
		set
		{
			if (Set(ref _months, value, "MonthsLate"))
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

	public RelayCommand ApplyCommand { get; }

	public SalarySimVM()
	{
		ApplyCommand = new RelayCommand(Apply);
	}

	private void Calculate()
	{
		_res = null;
		Plan.Clear();
		BreakdownText = "";
		Error = "";
		if (!Retroactive)
		{
			BreakdownText = "O salário novo vale a partir do mês escolhido. Os meses seguintes repetem até o próximo mês que tiver outro salário digitado. VT, INSS, faltas, 13º e simuladores acompanham.";
			return;
		}
		try
		{
			List<string> errors = new List<string>();
			YearMonth m = Input.Month(Month, "Mês", errors);
			decimal newValue = Input.Dec(NewSalary, "Salário novo", errors);
			int n = Input.Int(MonthsLate, "Meses em atraso", errors, 1, 24);
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			decimal old = SimulatorsViewModel.SalaryOn(m);
			MonthData d = Svc.Finance.GetMonth(m);
			decimal others = d.Entries.Where((Entry l) => l.Kind == 1 && !l.Description.StartsWith("Retroativo", StringComparison.OrdinalIgnoreCase)).Sum((Entry l) => l.Amount);
			_res = Salary.Retroactive(m, old, newValue, n, others, d.Payroll.Absences, SimulatorsViewModel.Params(m.Year).Inss);
			BreakdownText = _res.Breakdown.ToString();
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

	private void Apply()
	{
		List<string> errors = new List<string>();
		YearMonth m = Input.Month(Month, "Mês", errors);
		decimal newValue = Input.Dec(NewSalary, "Salário novo", errors);
		Input.Check(errors);
		if (Retroactive && (object)_res == null)
		{
			Dialogs.Warn((Error.Length > 0) ? Error : "Confira os dados do retroativo.");
		}
		else if (Dialogs.Confirm("Salário de " + Money.Fmt(newValue) + " a partir de " + m.SheetName + (Retroactive ? (" + retroativo de " + Money.Fmt(_res.RetroactiveAmount)) : "") + "?"))
		{
			if (Retroactive)
			{
				Svc.Finance.ApplyPlan(_res.Plan, "Retroativo " + m.Key, EntryOrigin.Retroactive);
			}
			Svc.Finance.SetSalary(m, newValue);
			Dialogs.Info("Salário atualizado.");
			Calculate();
		}
	}
}
