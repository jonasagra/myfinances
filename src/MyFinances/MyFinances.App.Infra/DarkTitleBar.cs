using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MyFinances.App.Infra;

public static class DarkTitleBar
{
	private const int DarkMode = 20;

	private const int DarkModeLegacy = 19;

	private const int CaptionColor = 35;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int amount, int size);

	public static void Enable(Window window)
	{
		window.SourceInitialized += (object? _, EventArgs _) =>
		{
			nint handle = new WindowInteropHelper(window).Handle;
			int amount = 1;
			if (DwmSetWindowAttribute(handle, 20, ref amount, 4) != 0)
			{
				DwmSetWindowAttribute(handle, 19, ref amount, 4);
			}
			int amount2 = 591623;
			DwmSetWindowAttribute(handle, 35, ref amount2, 4);
		};
	}
}
