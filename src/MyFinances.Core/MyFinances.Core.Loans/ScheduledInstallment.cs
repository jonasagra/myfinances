using System;

namespace MyFinances.Core.Loans;

public sealed record ScheduledInstallment(int Number, DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee, bool Grace, decimal BalanceAfter, string Breakdown)
{
	public decimal Total => Principal + Interest + Fee;
}
