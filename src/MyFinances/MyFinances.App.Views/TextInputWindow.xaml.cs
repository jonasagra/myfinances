using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;

namespace MyFinances.App.Views;

public partial class TextInputWindow : Window, IComponentConnector
{
	private readonly bool _password;

	public string? Answer { get; private set; }

	public TextInputWindow(string title, string question, bool password, string initialValue)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		Title = title;
		Question.Text = question;
		_password = password;
		if (password)
		{
			AnswerBox.Visibility = Visibility.Collapsed;
			PasswordInput.Visibility = Visibility.Visible;
			Loaded += (object _, RoutedEventArgs _) =>
			{
				PasswordInput.Focus();
			};
		}
		else
		{
			AnswerBox.Text = initialValue;
			Loaded += (object _, RoutedEventArgs _) =>
			{
				AnswerBox.Focus();
				AnswerBox.SelectAll();
			};
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		Answer = (_password ? PasswordInput.Password : AnswerBox.Text);
		DialogResult = true;
	}
}
