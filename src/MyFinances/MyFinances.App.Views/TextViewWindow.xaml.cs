using System.Windows;
using System.Windows.Markup;
using MyFinances.App.Infra;

namespace MyFinances.App.Views;

public partial class TextViewWindow : Window, IComponentConnector
{
	public TextViewWindow(string title, string text)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		Title = title;
		Heading.Text = title;
		Body.Text = text;
	}
}
