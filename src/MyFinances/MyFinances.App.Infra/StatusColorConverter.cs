using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MyFinances.Core.Loans;

namespace MyFinances.App.Infra;

public sealed class StatusColorConverter : IValueConverter
{
	public object Convert(object? value, Type t, object? p, CultureInfo c)
	{
		if (value is LoanStatus)
		{
			switch ((LoanStatus)value)
			{
			case LoanStatus.OnTime:
				break;
			case LoanStatus.Late:
				goto IL_005e;
			case LoanStatus.AtRisk:
			case LoanStatus.WrittenOff:
				goto IL_0066;
			case LoanStatus.PaidOff:
				goto IL_006e;
			default:
				goto IL_0076;
			}
		}
		else
		{
			if (!(value is InstallmentStatus))
			{
				goto IL_0076;
			}
			switch ((InstallmentStatus)value)
			{
			case InstallmentStatus.Upcoming:
				break;
			case InstallmentStatus.Partial:
				goto IL_005e;
			case InstallmentStatus.Overdue:
				goto IL_0066;
			case InstallmentStatus.Paid:
				goto IL_006e;
			default:
				goto IL_0076;
			}
		}
		string text = "InfoBrush";
		goto IL_007c;
		IL_0076:
		text = "MutedTextBrush";
		goto IL_007c;
		IL_007c:
		string key = text;
		return Application.Current.FindResource(key);
		IL_0066:
		text = "DangerBrush";
		goto IL_007c;
		IL_006e:
		text = "SuccessBrush";
		goto IL_007c;
		IL_005e:
		text = "WarningBrush";
		goto IL_007c;
	}

	public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
	{
		return Binding.DoNothing;
	}
}
