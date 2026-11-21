using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record ThirteenthResult(int Year, decimal Salary, decimal OvertimeAverage, int Twelfths, decimal Gross, decimal FirstInstallment, decimal Inss, decimal SecondInstallment, decimal TotalNet, CalcBreakdown Breakdown);
