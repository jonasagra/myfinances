using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Data.Services;

public sealed record EntrySeries(EntryKind Kind, string Name, int Count, YearMonth First, YearMonth Last, decimal LastAmount, int? LastInstallment, int? TotalInstallments)
{
	public bool HasMissingInstallments
	{
		get
		{
			int? lastInstallment = LastInstallment;
			if (lastInstallment.HasValue)
			{
				int k = lastInstallment.GetValueOrDefault();
				lastInstallment = TotalInstallments;
				if (lastInstallment.HasValue)
				{
					int n = lastInstallment.GetValueOrDefault();
					return k < n;
				}
			}
			return false;
		}
	}
}
