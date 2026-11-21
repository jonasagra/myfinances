using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record OvertimeResult(decimal HourlyRate, decimal Hour50, decimal Hour100, decimal Overtime50Amount, decimal Holiday100Amount, decimal HolidayOvertime100Amount, decimal TotalOvertime, int RestDays, int BusinessDays, decimal Dsr, decimal Earnings, decimal InssExtra, decimal NetExtra, YearMonth PaymentMonth, string Key, IReadOnlyList<PlanItem> Plan, IReadOnlyList<string> Warnings, CalcBreakdown Breakdown);
