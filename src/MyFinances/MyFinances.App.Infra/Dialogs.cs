using System;
using System.Linq;
using System.Windows;
using MyFinances.App.Views;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;
using Microsoft.Win32;

namespace MyFinances.App.Infra;

public static class Dialogs
{
	private static Window? Owner
	{
		get
		{
			Application current = Application.Current;
			object obj = ((current != null) ? current.Windows.OfType<Window>().FirstOrDefault((Window w) => w.IsActive) : null);
			if (obj == null)
			{
				Application current2 = Application.Current;
				if (current2 == null)
				{
					return null;
				}
				obj = current2.MainWindow;
			}
			return (Window?)obj;
		}
	}

	private static MessageBoxResult Show(string text, string title, MessageBoxButton b, MessageBoxImage i, MessageBoxResult defaultValue = MessageBoxResult.None)
	{
		Window owner = Owner;
		if (owner != null)
		{
			return MessageBox.Show(owner, text, title, b, i, defaultValue);
		}
		return MessageBox.Show(text, title, b, i, defaultValue);
	}

	public static void Info(string text, string title = "MyFinances")
	{
		Show(text, title, MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	public static void Warn(string text, string title = "Atenção")
	{
		Show(text, title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
	}

	public static void Error(string text, string title = "Erro")
	{
		Show(text, title, MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	public static bool Confirm(string text, string title = "Confirmar")
	{
		return Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
	}

	public static bool? Choose(string text, string title = "Confirmar")
	{
		return Show(text, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) switch
		{
			MessageBoxResult.Yes => (bool?)true, 
			MessageBoxResult.No => false, 
			_ => null, 
		};
	}

	public static void Guard(Action action)
	{
		try
		{
			action();
		}
		catch (ValidationException ex)
		{
			Warn(string.Join(Environment.NewLine, ex.Errors), "Confira os dados");
		}
		catch (ArgumentException ex2)
		{
			Warn(ex2.Message, "Confira os dados");
		}
		catch (Exception ex3)
		{
			Log.Error("Screen action", ex3);
			Error("Não foi possível concluir a operação.\n\n" + ex3.Message + "\n\nSe o problema continuar, veja a pasta de logs (Configurações > Sobre).");
		}
	}

	public static string? OpenFile(string filter, string title)
	{
		OpenFileDialog d = new OpenFileDialog
		{
			Filter = filter,
			Title = title,
			CheckFileExists = true,
			Multiselect = false
		};
		if (d.ShowDialog(Owner) != true)
		{
			return null;
		}
		return d.FileName;
	}

	public static string? SaveFile(string filter, string title, string suggestedName)
	{
		SaveFileDialog d = new SaveFileDialog
		{
			Filter = filter,
			Title = title,
			FileName = suggestedName,
			OverwritePrompt = true,
			AddExtension = true
		};
		if (d.ShowDialog(Owner) != true)
		{
			return null;
		}
		return d.FileName;
	}

	public static string? AskText(string title, string question, bool password = false, string initialValue = "")
	{
		TextInputWindow w = new TextInputWindow(title, question, password, initialValue)
		{
			Owner = Owner
		};
		if (w.ShowDialog() != true)
		{
			return null;
		}
		return w.Answer;
	}
}
