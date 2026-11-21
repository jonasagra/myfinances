using System;
using System.Windows.Input;

namespace MyFinances.App.Infra;

public sealed class RelayCommand(Action<object?> run, Func<object?, bool>? can = null) : ICommand
{
	public event EventHandler? CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	public RelayCommand(Action run, Func<bool>? can = null)
		: this((object? _) =>
		{
			run();
		}, (can == null) ? null : ((Func<object, bool>)((object? _) => can())))
	{
	}

	public bool CanExecute(object? parameter)
	{
		return can?.Invoke(parameter) ?? true;
	}

	public void Execute(object? parameter)
	{
		Dialogs.Guard(() =>
		{
			run(parameter);
		});
	}
}
