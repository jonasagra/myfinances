using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;

namespace MyFinances.App.Views;

public partial class DashboardView : UserControl, IComponentConnector
{
	public DashboardView()
	{
		InitializeComponent();
	}

	private void MonthsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (GridRows.RowItem(e) is DashboardMonthRow r)
		{
			MainViewModel.Instance?.OpenMonth(r.Ym);
		}
	}
}
