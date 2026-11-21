using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using MyFinances.App.Infra;
using MyFinances.App.Views;
using MyFinances.Core.Common;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
	private NavItem _selected;

	private bool _menuCollapsed = File.Exists(MenuFile);

	private RelayCommand? _toggleMenu;

	public ObservableCollection<NavItem> Items { get; }

	public string Version => "v" + BackupService.AppVersion;

	public static MainViewModel? Instance { get; private set; }

	public NavItem Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (value != null && Set(ref _selected, value, "Selected"))
			{
				Dialogs.Guard(value.Screen.Load);
				OnPropertyChanged("CurrentScreen");
			}
		}
	}

	public ScreenViewModel CurrentScreen => _selected.Screen;

	private static string MenuFile => Path.Combine(AppPaths.Root, "menu-collapsed");

	public bool MenuCollapsed
	{
		get
		{
			return _menuCollapsed;
		}
		set
		{
			if (!Set(ref _menuCollapsed, value, "MenuCollapsed"))
			{
				return;
			}
			try
			{
				if (value)
				{
					File.WriteAllText(MenuFile, "");
				}
				else
				{
					File.Delete(MenuFile);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	public RelayCommand ToggleMenuCommand => _toggleMenu ?? (_toggleMenu = new RelayCommand(() =>
	{
		MenuCollapsed = !MenuCollapsed;
	}));

	public RelayCommand DonateCommand { get; }

	public MainViewModel()
	{
		Instance = this;
		Items = new ObservableCollection<NavItem>
		{
			new NavItem("Painel", "Icon.Dashboard", () => new DashboardViewModel()),
			new NavItem("Meses e lançamentos", "Icon.Calendar", () => new FinanceViewModel()),
			new NavItem("Gerenciar lançamentos", "Icon.List", () => new EntriesManagerViewModel()),
			new NavItem("Simuladores CLT", "Icon.Calculator", () => new SimulatorsViewModel()),
			new NavItem("Clientes", "Icon.Users", () => new ClientsViewModel()),
			new NavItem("Empréstimos", "Icon.Money", () => new LoansViewModel()),
			new NavItem("Importar planilha", "Icon.Sheet", () => new ImportViewModel()),
			new NavItem("Backup", "Icon.Database", () => new BackupViewModel()),
			new NavItem("Configurações", "Icon.Settings", () => new SettingsViewModel())
		};
		foreach (NavItem item in Items)
		{
			item.OnSelected = (NavItem item) =>
			{
				Selected = item;
			};
		}
		_selected = Items[0];
		Dialogs.Guard(_selected.Screen.Load);
		Items[0].Active = true;
		DonateCommand = new RelayCommand(() =>
		{
			DonationWindow donationWindow = new DonationWindow();
			donationWindow.Owner = Application.Current.MainWindow;
			donationWindow.ShowDialog();
		});
	}

	public void OpenMonth(YearMonth month)
	{
		NavItem item = Items.First((NavItem i) => i.Screen is FinanceViewModel);
		((FinanceViewModel)item.Screen).ShowMonth(month);
		if (item == _selected)
		{
			item.Screen.Load();
		}
		else
		{
			item.Active = true;
		}
	}
}
