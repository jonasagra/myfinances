using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyFinances.App.Infra;

public sealed class NoneVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type t, object? p, CultureInfo c)
	{
		return (!values.All((object v) => v is bool && !(bool)v)) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] t, object? p, CultureInfo c)
	{
		return Array.Empty<object>();
	}
}
