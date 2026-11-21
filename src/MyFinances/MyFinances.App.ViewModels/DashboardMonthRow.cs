using MyFinances.Core.Common;

namespace MyFinances.App.ViewModels;

public sealed record DashboardMonthRow(YearMonth Ym, decimal Net, decimal Inflows, decimal Thirteenth, decimal Expenses, decimal Balance, decimal Remaining, decimal RemainingOther, double BarHeight, bool Negative, bool Future)
{
	public string Month => Ym.SheetName;

	public int Key => Ym.Year * 100 + Ym.Month;

	public string ShortMonth => Ym.SheetName.Substring(0, 3) + "/" + (Ym.Year % 100).ToString("00");
}
