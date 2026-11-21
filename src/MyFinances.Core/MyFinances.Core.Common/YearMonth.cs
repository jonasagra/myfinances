using System;
using System.Collections.Generic;

namespace MyFinances.Core.Common;

public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
	public static YearMonth Today => From(DateTime.Today);

	public DateOnly FirstDay => new DateOnly(Year, Month, 1);

	public DateOnly LastDay => FirstDay.AddMonths(1).AddDays(-1);

	public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

	public YearMonth Next => AddMonths(1);

	public YearMonth Previous => AddMonths(-1);

	public string Key => $"{Year:0000}-{Month:00}";

	public string SheetName => $"{Abbreviations[Month - 1]} {Year}";

	public string MonthNameLong => Names[Month - 1];

	private static readonly string[] Abbreviations = new string[12]
	{
		"JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT",
		"NOV", "DEZ"
	};

	private static readonly string[] Names = new string[12]
	{
		"JANEIRO", "FEVEREIRO", "MARÇO", "ABRIL", "MAIO", "JUNHO", "JULHO", "AGOSTO", "SETEMBRO", "OUTUBRO",
		"NOVEMBRO", "DEZEMBRO"
	};

	public static YearMonth From(DateOnly d)
	{
		return new YearMonth(d.Year, d.Month);
	}

	public static YearMonth From(DateTime d)
	{
		return new YearMonth(d.Year, d.Month);
	}

	public YearMonth AddMonths(int months)
	{
		DateOnly d = FirstDay.AddMonths(months);
		return new YearMonth(d.Year, d.Month);
	}

	public static int Difference(YearMonth a, YearMonth b)
	{
		return (b.Year - a.Year) * 12 + (b.Month - a.Month);
	}

	public override string ToString()
	{
		return SheetName;
	}

	public int CompareTo(YearMonth o)
	{
		return (Year * 12 + Month).CompareTo(o.Year * 12 + o.Month);
	}

	public static bool operator <(YearMonth a, YearMonth b)
	{
		return a.CompareTo(b) < 0;
	}

	public static bool operator >(YearMonth a, YearMonth b)
	{
		return a.CompareTo(b) > 0;
	}

	public static bool operator <=(YearMonth a, YearMonth b)
	{
		return a.CompareTo(b) <= 0;
	}

	public static bool operator >=(YearMonth a, YearMonth b)
	{
		return a.CompareTo(b) >= 0;
	}

	public static bool TryParseKey(string? s, out YearMonth c)
	{
		c = default;
		if (string.IsNullOrWhiteSpace(s) || s.Length != 7 || s[4] != '-')
		{
			return false;
		}
		int num;
		int m = default;
		if (int.TryParse(s.Substring(0, 4), out var a))
		{
			num = ((!int.TryParse(s.Substring(5, s.Length - 5), out m)) ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		bool flag = (byte)num != 0;
		if (!flag)
		{
			bool flag2 = ((m < 1 || m > 12) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		c = new YearMonth(a, m);
		return true;
	}

	public static YearMonth ParseKey(string s)
	{
		if (!TryParseKey(s, out var c))
		{
			throw new FormatException("Competência inválida: " + s);
		}
		return c;
	}

	public static bool TryParseSheetName(string? s, out YearMonth c)
	{
		c = default;
		if (string.IsNullOrWhiteSpace(s))
		{
			return false;
		}
		string t = s.Trim();
		if (t.Length != 8 || t[3] != ' ')
		{
			return false;
		}
		int i = Array.FindIndex(Abbreviations, (string x) => string.Equals(x, t.Substring(0, 3), StringComparison.OrdinalIgnoreCase));
		if (i >= 0)
		{
			string text = t;
			if (int.TryParse(text.Substring(4, text.Length - 4), out var year))
			{
				c = new YearMonth(year, i + 1);
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<YearMonth> Range(YearMonth a, YearMonth b)
	{
		YearMonth c = a;
		while (c <= b)
		{
			yield return c;
			c = c.Next;
		}
	}
}
