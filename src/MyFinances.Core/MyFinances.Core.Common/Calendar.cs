using System;
using System.Collections.Generic;

namespace MyFinances.Core.Common;

public sealed class Calendar
{
	public static readonly string[] FixedNational = new string[9] { "01/01", "21/04", "01/05", "07/09", "12/10", "02/11", "15/11", "20/11", "25/12" };

	public static readonly string[] SpreadsheetDefaultLocals = new string[3] { "24/06", "05/08", "08/12" };

	private readonly HashSet<string> _fixedRows;

	public bool GoodFriday { get; }

	public static Calendar Default { get; } = new Calendar();

	public IReadOnlyCollection<string> FixedHolidays => _fixedRows;

	public Calendar(IEnumerable<string>? localHolidays = null, bool goodFriday = true)
	{
		_fixedRows = new HashSet<string>(FixedNational);
		foreach (string item in localHolidays ?? SpreadsheetDefaultLocals)
		{
			string t = item.Trim();
			if (t.Length == 5 && t[2] == '/')
			{
				_fixedRows.Add(t);
			}
		}
		GoodFriday = goodFriday;
	}

	public bool IsHoliday(DateOnly d)
	{
		if (_fixedRows.Contains($"{d.Day:00}/{d.Month:00}"))
		{
			return true;
		}
		if (GoodFriday)
		{
			return d == Easter(d.Year).AddDays(-2);
		}
		return false;
	}

	public static DateOnly Easter(int y)
	{
		int a = y % 19;
		int b = y / 100;
		int num = y % 100;
		int d = b / 4;
		int e = b % 4;
		int f = (b + 8) / 25;
		int g = (b - f + 1) / 3;
		int h = (19 * a + b - d - g + 15) % 30;
		int i = num / 4;
		int k = num % 4;
		int l = (32 + 2 * e + 2 * i - h - k) % 7;
		int m = (a + 11 * h + 22 * l) / 451;
		int x = h + l - 7 * m + 114;
		return new DateOnly(y, x / 31, x % 31 + 1);
	}

	public DateOnly FifthBusinessDay(YearMonth month)
	{
		DateOnly d = month.FirstDay;
		int n = 0;
		while (true)
		{
			if (d.DayOfWeek != DayOfWeek.Sunday && !IsHoliday(d))
			{
				n++;
			}
			if (n == 5)
			{
				break;
			}
			d = d.AddDays(1);
		}
		while (true)
		{
			DayOfWeek dayOfWeek = d.DayOfWeek;
			bool flag = ((dayOfWeek == DayOfWeek.Sunday || dayOfWeek == DayOfWeek.Saturday) ? true : false);
			if (!flag && !IsHoliday(d))
			{
				break;
			}
			d = d.AddDays(-1);
		}
		return d;
	}

	public int RestDaysInMonth(YearMonth month)
	{
		int n = 0;
		DateOnly d = month.FirstDay;
		while (d <= month.LastDay)
		{
			if (d.DayOfWeek == DayOfWeek.Sunday || IsHoliday(d))
			{
				n++;
			}
			d = d.AddDays(1);
		}
		return n;
	}

	public string PayslipText(YearMonth receiptMonth)
	{
		YearMonth c = receiptMonth.Previous;
		return $"Contracheque de {c.MonthNameLong}/{c.Year} (pago até {FifthBusinessDay(receiptMonth):dd/MM/yyyy}, 5º dia útil). " + "Cada mês recebe o contracheque do mês anterior.";
	}
}
