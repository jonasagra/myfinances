using System.Collections.Generic;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;

namespace MyFinances.Data.Services;

public sealed record MonthData(YearMonth Month, decimal Salary, bool SalaryTyped, MonthAbsences Absences, string? Note, IReadOnlyList<Entry> Entries, PayrollResult Payroll, MonthSummary? Summary, bool EstimatedParameters);
