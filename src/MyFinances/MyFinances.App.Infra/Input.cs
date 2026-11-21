using System;
using System.Collections.Generic;
using System.Globalization;
using MyFinances.Core.Common;
using MyFinances.Data.Services;

namespace MyFinances.App.Infra;

public static class Input
{
	public static decimal Dec(string? txt, string field, List<string> errors, bool required = true, bool allowNegative = false, decimal? current = null)
	{
		if (string.IsNullOrWhiteSpace(txt))
		{
			if (required)
			{
				errors.Add(field + ": preencha o valor.");
			}
			return 0m;
		}
		if (MoneyExpression.IsCalculation(txt, current) ? (!MoneyExpression.TryEvaluate(txt, current, out var v)) : (!Money.TryParse(txt, out v)))
		{
			errors.Add(field + ": número ou conta inválida (ex.: 1.234,56 ou 10+5,50).");
			return 0m;
		}
		if (v < 0m && !allowNegative)
		{
			errors.Add(field + ": não pode ser negativo.");
			return 0m;
		}
		return v;
	}

	public static int Int(string? txt, string field, List<string> errors, int min, int max)
	{
		if (!int.TryParse((txt ?? "").Trim(), out var v) || v < min || v > max)
		{
			errors.Add($"{field}: número inteiro de {min} a {max}.");
			return min;
		}
		return v;
	}

	public static DateOnly Date(string? txt, string field, List<string> errors)
	{
		if (DateOnly.TryParseExact((txt ?? "").Trim(), new string[3] { "dd/MM/yyyy", "d/M/yyyy", "ddMMyyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
		{
			return d;
		}
		errors.Add(field + ": use dd/mm/aaaa (ex.: 26/02/2027).");
		return DateOnly.FromDateTime(DateTime.Today);
	}

	public static DateOnly? OptionalDate(string? txt, string field, List<string> errors)
	{
		if (!string.IsNullOrWhiteSpace(txt))
		{
			return Date(txt, field, errors);
		}
		return null;
	}

	public static DateTime Timestamp(string? date, string? time, string field, List<string> errors)
	{
		DateOnly d = Date(date, field, errors);
		TimeOnly h = TimeOnly.FromDateTime(DateTime.Now);
		if (!string.IsNullOrWhiteSpace(time) && !TimeOnly.TryParseExact(time.Trim(), new string[2] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out h))
		{
			errors.Add(field + ": hora no formato hh:mm.");
		}
		return d.ToDateTime(h);
	}

	public static YearMonth Month(string? txt, string field, List<string> errors)
	{
		string t = (txt ?? "").Trim();
		if (YearMonth.TryParseSheetName(t.ToUpperInvariant(), out var c))
		{
			return c;
		}
		string[] p = t.Split('/');
		if (p.Length == 2 && int.TryParse(p[0], out var m) && int.TryParse(p[1], out var a) && m >= 1 && m <= 12)
		{
			if (a < 100)
			{
				a += 2000;
			}
			if (a >= 1990 && a <= 2200)
			{
				return new YearMonth(a, m);
			}
		}
		errors.Add(field + ": use mm/aaaa (ex.: 10/2026).");
		return YearMonth.Today;
	}

	public static string Month(YearMonth c)
	{
		return $"{c.Month:00}/{c.Year}";
	}

	public static string Date(DateOnly d)
	{
		return d.ToString("dd/MM/yyyy");
	}

	public static string Num(decimal v)
	{
		return Money.Num(v);
	}

	public static void Check(List<string> errors)
	{
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
	}
}
