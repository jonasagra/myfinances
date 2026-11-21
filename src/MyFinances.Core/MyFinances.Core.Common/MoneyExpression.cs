using System;
using System.Linq;

namespace MyFinances.Core.Common;

public static class MoneyExpression
{
	private sealed class Parser(string s)
	{
		private int _i;

		public bool End => _i >= s.Length;

		private char Peek
		{
			get
			{
				if (!End)
				{
					return s[_i];
				}
				return '\0';
			}
		}

		public decimal Expr()
		{
			decimal v = Term();
			while (true)
			{
				char peek = Peek;
				if ((peek != '+' && peek != '-') || 1 == 0)
				{
					break;
				}
				v = ((s[_i++] == '+') ? (v + Term()) : (v - Term()));
			}
			return v;
		}

		private decimal Term()
		{
			decimal v = Factor();
			while (true)
			{
				char peek = Peek;
				if ((peek != '*' && peek != '/' && peek != 'x') || 1 == 0)
				{
					break;
				}
				char c = s[_i++];
				decimal r = Factor();
				v = ((c == '/') ? (v / r) : (v * r));
			}
			return v;
		}

		private decimal Factor()
		{
			if (Peek == '-')
			{
				_i++;
				return -Factor();
			}
			if (Peek == '+')
			{
				_i++;
				return Factor();
			}
			if (Peek == '(')
			{
				_i++;
				decimal result = Expr();
				if (Peek != ')')
				{
					throw new FormatException();
				}
				_i++;
				return result;
			}
			int start = _i;
			while (true)
			{
				bool flag = !End;
				if (flag)
				{
					bool flag2 = char.IsDigit(Peek);
					if (!flag2)
					{
						char peek = Peek;
						bool flag3 = ((peek == ',' || peek == '.') ? true : false);
						flag2 = flag3;
					}
					flag = flag2;
				}
				if (!flag)
				{
					break;
				}
				_i++;
			}
			if (_i != start)
			{
				string text = s;
				int num = start;
				if (Money.TryParse(text.Substring(num, _i - num), out var n))
				{
					return n;
				}
			}
			throw new FormatException();
		}
	}

	public static bool IsCalculation(string? text, decimal? current = null)
	{
		string t = Clean(text);
		if (t.Length == 0)
		{
			return false;
		}
		if (current.HasValue && IsOperator(t[0]))
		{
			return true;
		}
		if (!t.Skip(1).Any((char c) =>
		{
			bool flag = IsOperator(c);
			if (!flag)
			{
				bool flag2 = ((c == '(' || c == ')') ? true : false);
				flag = flag2;
			}
			return flag;
		}))
		{
			return t[0] == '(';
		}
		return true;
	}

	public static bool TryEvaluate(string? text, decimal? current, out decimal result)
	{
		result = 0m;
		string t = Clean(text);
		if (t.Length == 0)
		{
			return false;
		}
		if (current.HasValue)
		{
			decimal c = current.GetValueOrDefault();
			if (IsOperator(t[0]))
			{
				t = Money.Num(c).Replace(".", "") + t;
			}
		}
		Parser p = new Parser(t);
		try
		{
			decimal v = p.Expr();
			if (!p.End)
			{
				return false;
			}
			result = Money.R2(v);
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
		catch (DivideByZeroException)
		{
			return false;
		}
		catch (OverflowException)
		{
			return false;
		}
	}

	private static bool IsOperator(char c)
	{
		switch (c)
		{
		case '*':
		case '+':
		case '-':
		case '/':
		case 'x':
			return true;
		default:
			return false;
		}
	}

	private static string Clean(string? text)
	{
		return (text ?? "").Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "").Replace(" ", "")
			.Replace('X', 'x')
			.Replace('×', 'x')
			.Replace('÷', '/')
			.Trim();
	}
}
