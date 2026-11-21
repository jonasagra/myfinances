using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyFinances.Data.Services;

public static class Arithmetic
{
	public static bool TryEvaluate(string? formula, out decimal amount)
	{
		amount = 0m;
		if (string.IsNullOrWhiteSpace(formula))
		{
			return false;
		}
		string s = formula.Trim().TrimStart('=').Replace(" ", "");
		if (s.Length == 0 || !Regex.IsMatch(s, "^[0-9+\\-*/().]+$"))
		{
			return false;
		}
		int i = 0;
		try
		{
			amount = Expr(s, ref i);
			return i == s.Length;
		}
		catch
		{
			return false;
		}
	}

	private static decimal Expr(string s, ref int i)
	{
		decimal v = Term(s, ref i);
		while (i < s.Length && (s[i] == '+' || s[i] == '-'))
		{
			char c = s[i++];
			decimal t = Term(s, ref i);
			v = ((c == '+') ? (v + t) : (v - t));
		}
		return v;
	}

	private static decimal Term(string s, ref int i)
	{
		decimal v = Factor(s, ref i);
		while (i < s.Length && (s[i] == '*' || s[i] == '/'))
		{
			char c = s[i++];
			decimal f = Factor(s, ref i);
			v = ((c == '*') ? (v * f) : (v / f));
		}
		return v;
	}

	private static decimal Factor(string s, ref int i)
	{
		if (s[i] == '-')
		{
			i++;
			return -Factor(s, ref i);
		}
		if (s[i] == '+')
		{
			i++;
			return Factor(s, ref i);
		}
		if (s[i] == '(')
		{
			i++;
			decimal result = Expr(s, ref i);
			if (i >= s.Length || s[i] != ')')
			{
				throw new FormatException();
			}
			i++;
			return result;
		}
		int start = i;
		while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
		{
			i++;
		}
		int num = start;
		return decimal.Parse(s.Substring(num, i - num), CultureInfo.InvariantCulture);
	}
}
