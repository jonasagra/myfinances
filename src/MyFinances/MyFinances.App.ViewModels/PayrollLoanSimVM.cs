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

public sealed class PayrollLoanSimVM : ObservableObject
{
	public sealed record TableRow(int Number, decimal Interest, decimal Amortization, decimal Balance);

	private string _amount = "1.200,00";

	private string _rate = "3,00";

	private string _installmentCount = "12";

	private string _contractDate = "";

	private string _firstDue = "";

	private string _pay = "";

	private string _mandatoryDeductions = "";

	private string _current = "";

	private string _iof = "0";

	private string _fees = "0";

	private string _contractInstallment = "";

	private string _description = "Empréstimo consignado (CLT)";

	private string _breakdown = "";

	private string _error = "";

	private PayrollLoanResult? _res;

	public ObservableCollection<PlanRow> Plan { get; } = new ObservableCollection<PlanRow>();

	public ObservableCollection<TableRow> Table { get; } = new ObservableCollection<TableRow>();

	public string Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (Set(ref _amount, value, "Amount"))
			{
				Calculate();
			}
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
			if (Set(ref _rate, value, "Rate"))
			{
				Calculate();
			}
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
			if (Set(ref _installmentCount, value, "Installments"))
			{
				Calculate();
			}
		}
	}

	public string ContractDate
	{
		get
		{
			return _contractDate;
		}
		set
		{
			if (Set(ref _contractDate, value, "ContractDate"))
			{
				Calculate();
			}
		}
	}

	public string FirstDueDate
	{
		get
		{
			return _firstDue;
		}
		set
		{
			if (Set(ref _firstDue, value, "FirstDueDate"))
			{
				Calculate();
			}
		}
	}

	public string Pay
	{
		get
		{
			return _pay;
		}
		set
		{
			if (Set(ref _pay, value, "Pay"))
			{
				Calculate();
			}
		}
	}

	public string MandatoryDeductions
	{
		get
		{
			return _mandatoryDeductions;
		}
		set
		{
			if (Set(ref _mandatoryDeductions, value, "MandatoryDeductions"))
			{
				Calculate();
			}
		}
	}

	public string CurrentInstallments
	{
		get
		{
			return _current;
		}
		set
		{
			if (Set(ref _current, value, "CurrentInstallments"))
			{
				Calculate();
			}
		}
	}

	public string Iof
	{
		get
		{
			return _iof;
		}
		set
		{
			if (Set(ref _iof, value, "Iof"))
			{
				Calculate();
			}
		}
	}

	public string Fees
	{
		get
		{
			return _fees;
		}
		set
		{
			if (Set(ref _fees, value, "Fees"))
			{
				Calculate();
			}
		}
	}

	public string ContractInstallment
	{
		get
		{
			return _contractInstallment;
		}
		set
		{
			if (Set(ref _contractInstallment, value, "ContractInstallment"))
			{
				Calculate();
			}
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (Set(ref _description, value, "Description"))
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

	public RelayCommand PostCommand { get; }

	public RelayCommand FillCommand { get; }

	public PayrollLoanSimVM()
	{
		PostCommand = new RelayCommand(Post, () => (object)_res != null);
		FillCommand = new RelayCommand(Fill);
	}

	public void Fill()
	{
		DateOnly today = DateOnly.FromDateTime(DateTime.Today);
		if (string.IsNullOrWhiteSpace(_contractDate))
		{
			_contractDate = Input.Date(today);
		}
		if (string.IsNullOrWhiteSpace(_firstDue))
		{
			_firstDue = Input.Date(today.AddMonths(1));
		}
		MonthData d = Svc.Finance.GetMonth(YearMonth.Today);
		_pay = Money.Num(d.Payroll.Gross);
		_mandatoryDeductions = Money.Num(d.Payroll.Inss + d.Payroll.Irrf);
		_current = Money.Num(d.Entries.Where((Entry l) => l.Kind == 2 && l.Amount > 0m && l.Description.Contains("consig", StringComparison.OrdinalIgnoreCase)).Sum((Entry l) => l.Amount));
		string[] array = new string[5] { "ContractDate", "FirstDueDate", "Pay", "MandatoryDeductions", "CurrentInstallments" };
		foreach (string n in array)
		{
			OnPropertyChanged(n);
		}
		Calculate();
	}

	private void Calculate()
	{
		_res = null;
		Plan.Clear();
		Table.Clear();
		BreakdownText = "";
		Error = "";
		try
		{
			List<string> errors = new List<string>();
			PayrollLoanInput e = new PayrollLoanInput(Input.Dec(Amount, "Valor liberado", errors), Input.Dec(Rate, "Taxa ao mês", errors, required: false), Input.Int(Installments, "Parcelas", errors, 1, 120), Input.Date(ContractDate, "Data da contratação", errors), Input.Date(FirstDueDate, "Vencimento da 1ª parcela", errors), Input.Dec(Pay, "Remuneração bruta", errors, required: false), Input.Dec(MandatoryDeductions, "Descontos obrigatórios", errors, required: false), Input.Dec(CurrentInstallments, "Parcelas atuais", errors, required: false), Input.Dec(Iof, "IOF (R$)", errors, required: false), Input.Dec(Fees, "Tarifas", errors, required: false), Input.Dec(ContractInstallment, "Parcela do contrato", errors, required: false), string.IsNullOrWhiteSpace(Description) ? "Empréstimo consignado (CLT)" : Description.Trim());
			if (errors.Count > 0)
			{
				Error = string.Join(" ", errors);
				return;
			}
			_res = PayrollLoan.Simulate(e, SimulatorsViewModel.Params(e.ContractDate.Year));
			BreakdownText = _res.Breakdown.ToString();
			foreach (PlanItem i in _res.Plan)
			{
				Plan.Add(PlanRow.From(i));
			}
			foreach (var t in _res.Table)
			{
				Table.Add(new TableRow(t.Number, t.Interest, t.Amortization, t.Balance));
			}
		}
		catch (ArgumentException ex)
		{
			Error = ex.Message;
		}
	}

	private void Post()
	{
		if ((object)_res != null && Dialogs.Confirm($"Lançar o crédito em Entradas e {_res.Plan.Count - 1} parcela(s) em Descontos no salário?"))
		{
			Svc.Finance.ApplyPlan(_res.Plan, "Consignado " + Guid.NewGuid().ToString("N").Substring(0, 8), EntryOrigin.PayrollLoan);
			Dialogs.Info("Consignado lançado. Para desfazer, use \"Desfazer grupo\" em Meses e lançamentos.");
		}
	}
}
