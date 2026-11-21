using System;
using System.Globalization;
using System.Windows.Data;
using MyFinances.Core.Common;

namespace MyFinances.App.Infra;

public sealed class CurrencyConverter : IValueConverter
{
	public object Convert(object? value, Type t, object? p, CultureInfo c)
	{
		if (!(value is decimal d))
		{
			if (value == null)
			{
				return "";
			}
			return value.ToString() ?? "";
		}
		return Money.Fmt(d);
	}

	public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
	{
		if (!Money.TryParse(value as string, out var v))
		{
			return Binding.DoNothing;
		}
		return v;
	}
}
