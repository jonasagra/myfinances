namespace MyFinances.Core.Labor;

public sealed record UnemploymentBenefitTable(int Year, decimal Band1, decimal Band2, decimal Fixed2, decimal Ceiling, string Source, bool Confirmed)
{
	public const decimal Pct1 = 0.8m;

	public const decimal Pct2 = 0.5m;
}
