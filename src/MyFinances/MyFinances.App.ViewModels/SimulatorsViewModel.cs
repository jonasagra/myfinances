using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;

namespace MyFinances.App.ViewModels;

public sealed class SimulatorsViewModel : ScreenViewModel
{
	public override string Title => "Simuladores CLT";

	public VacationSimVM Vacation { get; } = new VacationSimVM();

	public TerminationSimVM Termination { get; } = new TerminationSimVM();

	public OvertimeSimVM Overtime { get; } = new OvertimeSimVM();

	public PayrollLoanSimVM PayrollLoan { get; } = new PayrollLoanSimVM();

	public SalarySimVM Salary { get; } = new SalarySimVM();

	public string Assumptions => $"Tabelas {P.Inss.Year}: INSS {(P.Inss.Confirmed ? "confirmado" : "estimado")}, IRRF {(P.Irrf.Confirmed ? "confirmado" : "estimado")}, seguro-desemprego {(P.UnemploymentBenefit.Confirmed ? "confirmado" : "estimado")}. Simulação não substitui o cálculo da empresa.";

	private static LegalParameters P => Svc.Config.ParametersForYear(DateTime.Today.Year).Parameters;

	public override void Load()
	{
		Vacation.Load();
		Termination.Fill();
		Overtime.Load();
		PayrollLoan.Fill();
		OnPropertyChanged("Assumptions");
	}

	internal static LegalParameters Params(int year)
	{
		return Svc.Config.ParametersForYear(year).Parameters;
	}

	internal static void Fill<T>(ObservableCollection<T> c, IEnumerable<T> items)
	{
		c.Clear();
		foreach (T i in items)
		{
			c.Add(i);
		}
	}

	internal static decimal SalaryOn(YearMonth c)
	{
		return FinanceSummary.CurrentSalary(Svc.Finance.SalariesSet(), c);
	}
}
