using System;

namespace MyFinances.Core.Labor;

public sealed record PayrollConfig
{
	public int MonthlyHours { get; init; } = 180;

	public bool TransportVoucherActive { get; init; } = true;

	public decimal TransportVoucherPct { get; init; } = 6m;

	public bool IrrfActive { get; init; } = true;

	public int Dependents { get; init; }

	public DateOnly? HireDate { get; init; } = new DateOnly(2024, 9, 7);
}
