using System;
using System.Globalization;
using System.Windows.Data;

namespace MyFinances.App.Infra;

public sealed class DateConverter : IValueConverter
{
	public object Convert(object? value, Type t, object? p, CultureInfo c)
	{
		if (!(value is DateOnly d))
		{
			if (!(value is DateTime d2))
			{
				if (value == null)
				{
					return "";
				}
				return value.ToString() ?? "";
			}
			return d2.ToString((p as string) ?? "dd/MM/yyyy HH:mm");
		}
		return d.ToString("dd/MM/yyyy");
	}

	public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
	{
		return Binding.DoNothing;
	}
}
