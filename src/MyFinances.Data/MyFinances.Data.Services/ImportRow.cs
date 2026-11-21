using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Data.Services;

public sealed class ImportRow
{
	public YearMonth Month { get; init; }

	public string Sheet { get; init; } = "";

	public bool SheetHidden { get; init; }

	public string Table { get; init; } = "";

	public string Prefix { get; init; } = "";

	public int SpreadsheetRow { get; init; }

	public EntryKind? Kind { get; set; }

	public string Description { get; init; } = "";

	public decimal Amount { get; init; }

	public string AsRead { get; init; } = "";

	public string? Notice { get; set; }

	public bool Selected { get; set; } = true;

	public string KindText => Kind switch
	{
		EntryKind.Earning => "Provento", 
		EntryKind.Deduction => "Desconto", 
		EntryKind.Inflow => "Entrada", 
		EntryKind.Expense => "Despesa", 
		_ => "(ignorar)", 
	};
}
