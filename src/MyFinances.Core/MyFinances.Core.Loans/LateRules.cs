namespace MyFinances.Core.Loans;

public sealed record LateRules
{
	public decimal PenaltyPct { get; init; } = 2m;

	public decimal MonthlyLateInterestPct { get; init; } = 1m;

	public int ToleranceDays { get; init; }

	public int DaysAtRisk { get; init; } = 90;

	public decimal LossProvisionPct { get; init; } = 100m;

	public static LateRules Default { get; } = new LateRules();
}
