using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.Data.Infra;

namespace MyFinances.App.Views;

public partial class PinWindow : Window, IComponentConnector
{
	private readonly string _hash;

	private int _attempts;

	public PinWindow(string hash)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		_hash = hash;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PinBox.Focus();
		};
	}

	private async void Enter_Click(object sender, RoutedEventArgs e)
	{
		if (Pin.Check(PinBox.Password, _hash))
		{
			DialogResult = true;
			return;
		}
		_attempts++;
		PinBox.Clear();
		Message.Text = "PIN incorreto.";
		if (_attempts >= 3)
		{
			IsEnabled = false;
			Message.Text = $"PIN incorreto. Aguarde {_attempts * 2} segundos.";
			await Task.Delay(_attempts * 2000);
			IsEnabled = true;
			PinBox.Focus();
		}
	}
}
