using System;

namespace MyFinances.Core.Loans;

public sealed record LoanTerms
{
	public decimal Principal { get; init; }

	public DateOnly ContractDate { get; init; }

	public DateOnly FirstDueDate { get; init; }

	public int InstallmentCount { get; init; } = 1;

	public decimal InterestRatePct { get; init; }

	public Frequency Frequency { get; init; }

	public AmortizationMethod Method { get; init; }

	public int GracePeriods { get; init; }

	public GraceKind GraceKind { get; init; }

	public decimal SetupFee { get; init; }

	public LateRules LateRules { get; init; } = MyFinances.Core.Loans.LateRules.Default;
}
