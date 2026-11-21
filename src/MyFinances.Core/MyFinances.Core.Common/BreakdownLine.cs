namespace MyFinances.Core.Common;

public sealed record BreakdownLine(string Label, string? Formula, decimal? Amount, bool TextOnly)
{
	public string AmountText
	{
		get
		{
			if (Amount.HasValue)
			{
				return Money.Fmt(Amount.Value);
			}
			return "";
		}
	}

	public override string ToString()
	{
		if (TextOnly)
		{
			return Label;
		}
		string f = (string.IsNullOrEmpty(Formula) ? "" : ("  [" + Formula + "]"));
		return Label + ": " + AmountText + f;
	}
}
