using System;
using System.Globalization;

namespace MyFinances.Core.Common;

public static class Money
{
	public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

	public static decimal R2(decimal v)
	{
		return Math.Round(v, 2, MidpointRounding.AwayFromZero);
	}

	public static decimal R(decimal v, int decimals)
	{
		return Math.Round(v, decimals, MidpointRounding.AwayFromZero);
	}

	public static decimal Trunc2(decimal v)
	{
		return Math.Truncate(v * 100m) / 100m;
	}

	public static string Fmt(decimal v)
	{
		return v.ToString("C2", PtBr);
	}

	public static string Num(decimal v)
	{
		return v.ToString("N2", PtBr);
	}

	public static string Pct(decimal points, int decimals = 2)
	{
		return points.ToString("N" + decimals, PtBr) + "%";
	}

	public static bool TryParse(string? text, out decimal amount)
	{
		amount = 0m;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string t = text.Trim().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "")
			.Replace("\u00a0", "");
		if (t.Length == 0)
		{
			return false;
		}
		if (t.Contains(','))
		{
			t = t.Replace(".", "");
			return decimal.TryParse(t, NumberStyles.Number, PtBr, out amount);
		}
		string[] parts = t.Split('.');
		if (parts.Length == 2 && parts[1].Length <= 2)
		{
			return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
		}
		t = t.Replace(".", "");
		return decimal.TryParse(t, NumberStyles.Number, PtBr, out amount);
	}

	public static decimal Pow(decimal b, int n)
	{
		if (n < 0)
		{
			return 1m / Pow(b, -n);
		}
		decimal r = 1m;
		for (int i = 0; i < n; i++)
		{
			r *= b;
		}
		return r;
	}
}
