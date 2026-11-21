using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyFinances.App.Infra;

public sealed class SignColorConverter : IValueConverter
{
	public object Convert(object? value, Type t, object? p, CultureInfo c)
	{
		decimal d = ((value is decimal x) ? x : 0m);
		Application app = Application.Current;
		if (d > 0m)
		{
			return app.FindResource("SuccessBrush");
		}
		if (d < 0m)
		{
			return app.FindResource("DangerBrush");
		}
		return app.FindResource("TextBrush");
	}

	public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
	{
		return Binding.DoNothing;
	}
}
