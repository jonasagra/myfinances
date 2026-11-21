using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFinances.App.Infra;

public abstract class ObservableObject : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	protected bool Set<T>(ref T field, T amount, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, amount))
		{
			return false;
		}
		field = amount;
		OnPropertyChanged(name);
		return true;
	}
}
