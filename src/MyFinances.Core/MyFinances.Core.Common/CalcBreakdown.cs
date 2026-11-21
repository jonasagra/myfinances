using System.Collections.Generic;
using System.Text;

namespace MyFinances.Core.Common;

public sealed class CalcBreakdown
{
	private readonly List<BreakdownLine> _rows = new List<BreakdownLine>();

	public IReadOnlyList<BreakdownLine> Rows => _rows;

	public CalcBreakdown Add(string label, decimal amount, string? formula = null)
	{
		_rows.Add(new BreakdownLine(label, formula, amount, TextOnly: false));
		return this;
	}

	public CalcBreakdown Text(string text)
	{
		_rows.Add(new BreakdownLine(text, null, null, TextOnly: true));
		return this;
	}

	public CalcBreakdown Join(CalcBreakdown? other, string? prefix = null)
	{
		if (other == null)
		{
			return this;
		}
		foreach (BreakdownLine l in other._rows)
		{
			_rows.Add((prefix == null) ? l : l with
			{
				Label = prefix + l.Label
			});
		}
		return this;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		foreach (BreakdownLine l in _rows)
		{
			sb.AppendLine(l.ToString());
		}
		return sb.ToString().TrimEnd();
	}
}
