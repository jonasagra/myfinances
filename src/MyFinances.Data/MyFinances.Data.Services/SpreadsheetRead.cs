using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Data.Services;

public sealed class SpreadsheetRead
{
	public string File { get; init; } = "";

	public string Sha256 { get; init; } = "";

	public List<ImportMonth> Months { get; } = new List<ImportMonth>();

	public List<TableMapping> Mappings { get; } = new List<TableMapping>();

	internal List<(string Sheet, bool Hidden, YearMonth Month, string Table, string Prefix, int Row, Dictionary<string, (string Text, decimal? Number, string How)> Columns)> Raw { get; } = new List<(string, bool, YearMonth, string, string, int, Dictionary<string, (string, decimal?, string)>)>();

	public YearMonth? FirstMonth { get; init; }

	public bool HadMacros { get; init; }

	public List<string> Notes { get; } = new List<string>();
}
