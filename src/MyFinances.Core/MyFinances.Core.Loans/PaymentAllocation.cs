using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinances.Core.Loans;

public sealed record PaymentAllocation(Guid PaymentId, DateTime Timestamp, decimal Amount, int Version, IReadOnlyList<AllocationItem> Items, decimal Surplus, bool AfterWriteOff, string Breakdown)
{
	public decimal Total(Component c)
	{
		return Items.Where((AllocationItem i) => i.Component == c).Sum((AllocationItem i) => i.Amount);
	}
}
