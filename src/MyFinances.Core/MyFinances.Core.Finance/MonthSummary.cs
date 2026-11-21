using System.Collections.Generic;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Core.Finance;

public sealed record MonthSummary(YearMonth Month, PayrollResult Payroll, decimal Inflows, decimal Thirteenth, decimal Expenses, decimal MonthBalanceWith13th, decimal MonthBalanceWithout13th, decimal RemainingWith13th, decimal RemainingWithout13th, IReadOnlyDictionary<string, decimal> ExpensesByCategory)
{
	public decimal Revenue(ThirteenthScenario c)
	{
		return Payroll.Net + Inflows + ((c == ThirteenthScenario.With13th) ? Thirteenth : 0m);
	}

	public decimal Balance(ThirteenthScenario c)
	{
		if (c != ThirteenthScenario.With13th)
		{
			return MonthBalanceWithout13th;
		}
		return MonthBalanceWith13th;
	}

	public decimal Remaining(ThirteenthScenario c)
	{
		if (c != ThirteenthScenario.With13th)
		{
			return RemainingWithout13th;
		}
		return RemainingWith13th;
	}
}
