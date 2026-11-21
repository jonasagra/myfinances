using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;

namespace MyFinances.App.Views;

public partial class EntryWindow : Window, IComponentConnector
{
	private readonly EntryViewModel _vm;

	public EntryViewModel Vm => _vm;

	public EntryWindow(EntryEdit e)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		DataContext = (_vm = new EntryViewModel(e));
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		Dialogs.Guard(() =>
		{
			if (_vm.Save())
			{
				DialogResult = true;
			}
		});
	}
}
