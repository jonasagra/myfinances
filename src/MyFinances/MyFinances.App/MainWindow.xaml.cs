using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;

namespace MyFinances.App;

public partial class MainWindow : Window, IComponentConnector
{
	public MainWindow()
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		DataContext = new MainViewModel();
	}
}
