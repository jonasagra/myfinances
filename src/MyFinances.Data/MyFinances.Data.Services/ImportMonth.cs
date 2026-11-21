using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Data.Services;

public sealed class ImportMonth
{
	public YearMonth Month { get; init; }

	public string Sheet { get; init; } = "";

	public bool Hidden { get; init; }

	public decimal? Salary { get; init; }

	public bool SalaryTyped { get; init; }

	public MonthAbsences Absences { get; init; } = MonthAbsences.None;

	public bool Selected { get; set; } = true;
}
