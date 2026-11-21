using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using MyFinances.App.Infra;
using MyFinances.App.Views;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App;

public partial class App : Application
{
	private Mutex? _instance;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		CultureInfo pt = (CultureInfo.DefaultThreadCurrentUICulture = (CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("pt-BR")));
		Thread.CurrentThread.CurrentCulture = pt;
		Thread.CurrentThread.CurrentUICulture = pt;
		FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("pt-BR")));
		DispatcherUnhandledException += UnhandledError;
		AppDomain.CurrentDomain.UnhandledException += (object _, UnhandledExceptionEventArgs a) =>
		{
			if (a.ExceptionObject is Exception ex2)
			{
				Log.Error("Fatal error", ex2);
			}
		};
		string[] args = e.Args.Select((string a) => a.ToLowerInvariant()).ToArray();
		_instance = new Mutex(initiallyOwned: true, "MyFinances-" + Environment.UserName, out var first);
		if (!first && !Wait(_instance, args.Contains("--restarting") ? TimeSpan.FromSeconds(15L) : TimeSpan.Zero))
		{
			MessageBox.Show("O MyFinances já está aberto.", "MyFinances", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			Shutdown();
			return;
		}
		if (args.Contains("--remove-data"))
		{
			RemoveData();
			Shutdown();
			return;
		}
		try
		{
			Bootstrap.Run(Svc.Config, Svc.Backup);
		}
		catch (Exception ex)
		{
			Log.Error("Startup", ex);
			MessageBox.Show("Não consegui abrir os dados do MyFinances.\n\n" + ex.Message + "\n\nPasta de dados: " + AppPaths.Root, "MyFinances", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown();
			return;
		}
		if (args.Contains("--creator-mode") && MessageBox.Show("Ativar o MODO CRIADOR neste usuário do Windows?\n\nIsso só esconde a tela de doação (cria creator-mode.txt na pasta de dados). Nenhuma função muda.", "MyFinances — modo criador", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
		{
			Bootstrap.SetCreatorMode(active: true);
		}
		if (args.Contains("--reset-pin") && MessageBox.Show("Remover o PIN de abertura do MyFinances?", "MyFinances", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
		{
			Svc.Config.Save(Svc.Config.Preferences() with
			{
				PinHash = null
			});
		}
		Preferences prefs = Svc.Config.Preferences();
		if (!string.IsNullOrEmpty(prefs.PinHash) && new PinWindow(prefs.PinHash).ShowDialog() != true)
		{
			Shutdown();
			return;
		}
		MainWindow main = new MainWindow();
		MainWindow = main;
		ShutdownMode = ShutdownMode.OnMainWindowClose;
		main.Show();
		if (!Bootstrap.CreatorMode && DonationWindow.ShouldShow())
		{
			main.Dispatcher.BeginInvoke((Func<bool?>)(() => new DonationWindow
			{
				Owner = main
			}.ShowDialog()), DispatcherPriority.ApplicationIdle);
		}
	}

	private static bool Wait(Mutex m, TimeSpan t)
	{
		try
		{
			return m.WaitOne(t);
		}
		catch (AbandonedMutexException)
		{
			return true;
		}
	}

	private static void RemoveData()
	{
		if (MessageBox.Show("REMOVER DADOS LOCAIS DO MYFINANCES\n\nIsso apaga, deste usuário do Windows: banco de dados, anexos de clientes, chave de proteção, backups automáticos e logs.\n\nPasta: " + AppPaths.Root + "\n\nRecomendado: antes, abra o MyFinances e faça um backup portátil (.mfbak).\n\nApagar agora?", "MyFinances", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) != MessageBoxResult.Yes)
		{
			return;
		}
		TextInputWindow w = new TextInputWindow("Confirmar", "Para confirmar, digite APAGAR:", password: false, "");
		if (w.ShowDialog() != true || w.Answer?.Trim() != "APAGAR")
		{
			MessageBox.Show("Nada foi apagado.");
			return;
		}
		try
		{
			Bootstrap.DeleteLocalData();
			MessageBox.Show("Dados apagados.", "MyFinances");
		}
		catch (Exception ex)
		{
			MessageBox.Show("Não foi possível apagar tudo (feche o MyFinances e tente de novo).\n" + ex.Message, "MyFinances");
		}
	}

	private void UnhandledError(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		Log.Error("Unhandled error", e.Exception);
		if (e.Exception is ValidationException v)
		{
			Dialogs.Warn(string.Join(Environment.NewLine, v.Errors));
		}
		else
		{
			Dialogs.Error("Aconteceu um erro inesperado e a operação foi interrompida.\n\n" + e.Exception.Message + "\n\nDetalhes técnicos estão no log (Configurações > Sobre).");
		}
		e.Handled = true;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		ClientsService.ClearTemp();
		try
		{
			_instance?.ReleaseMutex();
		}
		catch
		{
		}
		base.OnExit(e);
	}
}
