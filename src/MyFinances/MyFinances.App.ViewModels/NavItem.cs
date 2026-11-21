using System;
using System.Windows;
using System.Windows.Media;
using MyFinances.App.Infra;

namespace MyFinances.App.ViewModels;

public sealed class NavItem(string title, string icon, Func<ScreenViewModel> create) : ObservableObject
{
	private ScreenViewModel? _screen;

	private bool _active;

	public string Title { get; } = title;

	public Geometry? Icon { get; } = Application.Current.TryFindResource(icon) as Geometry;

	public ScreenViewModel Screen => _screen ?? (_screen = create());

	internal Action<NavItem>? OnSelected { get; set; }

	public bool Active
	{
		get
		{
			return _active;
		}
		set
		{
			if (Set(ref _active, value, "Active") & value)
			{
				OnSelected?.Invoke(this);
			}
		}
	}
}
