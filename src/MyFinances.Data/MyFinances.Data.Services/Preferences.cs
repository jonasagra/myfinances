using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;
using MyFinances.Core.Loans;

namespace MyFinances.Data.Services;

public sealed record Preferences
{
	public PayrollConfig Payroll { get; init; } = new PayrollConfig();

	public decimal OpeningBalance { get; init; }

	public string FirstMonth { get; init; } = "2026-10";

	public ThirteenthScenario DefaultScenario { get; init; }

	public string[] LocalHolidays { get; init; } = Calendar.SpreadsheetDefaultLocals;

	public LateRules DefaultLateRules { get; init; } = LateRules.Default;

	public decimal? CapitalLimit { get; init; }

	public int MaxAttachmentMb { get; init; } = 15;

	public bool AutoBackup { get; init; } = true;

	public int AutoBackupsKept { get; init; } = 7;

	public string? PinHash { get; init; }

	public bool PinForAttachments { get; init; } = true;
}
