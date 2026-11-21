using MyFinances.Core.Common;

namespace MyFinances.Core.Loans;

public sealed record LoanMonthlySeries(YearMonth Month, decimal ExpectedInterest, decimal InterestAndChargesReceived, decimal PrincipalReceived);
