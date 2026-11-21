using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record PayrollLoanResult(decimal Installment, decimal TotalPaid, decimal TotalInterest, decimal FinancedAmount, decimal MonthlyCetPct, decimal AnnualCetPct, decimal Margin, decimal AvailableMargin, bool FitsMargin, IReadOnlyList<(int Number, decimal Interest, decimal Amortization, decimal Balance)> Table, IReadOnlyList<PlanItem> Plan, CalcBreakdown Breakdown);
