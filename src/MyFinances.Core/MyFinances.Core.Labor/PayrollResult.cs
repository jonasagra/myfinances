using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record PayrollResult(decimal BaseSalary, decimal Earnings, decimal Gross, decimal Absences, decimal Inss, decimal TransportVoucher, decimal Irrf, decimal ManualDeductions, decimal TotalDeductions, decimal Net, CalcBreakdown Breakdown);
