using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyFinances.App.Infra;

public sealed class VisibleConverter : IValueConverter
{
	public object Convert(object? value, Type t, object? p, CultureInfo c)
	{
		bool flag;
		if (value is bool b)
		{
			flag = b;
		}
		else
		{
			flag = ((value is string s) ? (!string.IsNullOrWhiteSpace(s)) : (value != null && (!(value is int i) || i != 0)));
		}
		bool v = flag;
		if (p as string == "invert")
		{
			v = !v;
		}
		return (!v) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
	{
		return Binding.DoNothing;
	}
}
