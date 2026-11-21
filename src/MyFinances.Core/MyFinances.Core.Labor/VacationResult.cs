using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record VacationResult(DateOnly Start, DateOnly End, DateOnly Payment, int DaysMonth1, int DaysMonth2, decimal VacationAmount, decimal AverageAmount, decimal Third, decimal Gross, decimal Inss1, decimal Inss2, decimal PayrollLoan1, decimal PayrollLoan2, decimal Net, decimal Saved1, decimal Saved2, string? Notice, IReadOnlyList<PlanItem> Plan, CalcBreakdown Breakdown, string Key);
