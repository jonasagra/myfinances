using System.Collections.Generic;
using MyFinances.Core.Labor;

namespace MyFinances.Data.Services;

public sealed class TableMapping
{
	public string Prefix { get; set; } = "";

	public EntryKind? Kind { get; set; }

	public string DescriptionColumn { get; set; } = "Lançamento";

	public string AmountColumn { get; set; } = "Valor";

	public List<string> Columns { get; set; } = new List<string>();

	public int TableCount { get; set; }
}
