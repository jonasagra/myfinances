using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;

namespace MyFinances.App.Views;

public partial class PayslipImportWindow : Window, IComponentConnector
{
	public PayslipImportViewModel Vm { get; }

	public PayslipImportWindow(PayslipImportViewModel vm)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		DataContext = (Vm = vm);
	}

	private void Import_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.Target.HasValue)
		{
			DialogResult = true;
		}
	}
}
