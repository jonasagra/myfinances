using System.Collections.Generic;

namespace MyFinances.Core.Labor;

public sealed record InssTable(int Year, IReadOnlyList<InssBand> Bands, string Source, bool Confirmed, InssRounding Rounding = InssRounding.TruncatePerBand)
{
	public decimal Ceiling
	{
		get
		{
			IReadOnlyList<InssBand> bands = Bands;
			return bands[bands.Count - 1].Until;
		}
	}
}
