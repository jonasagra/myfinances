using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyFinances.App.Infra;

public static class GridRows
{
	public static object? RowItem(MouseEventArgs e)
	{
		DependencyObject d = (e.MouseDevice.DirectlyOver as DependencyObject) ?? (e.OriginalSource as DependencyObject);
		while (d != null && !(d is DataGridRow) && !(d is DataGridColumnHeader))
		{
			bool flag = ((d is Visual || d is Visual3D) ? true : false);
			d = (flag ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d));
		}
		return (d as DataGridRow)?.Item;
	}
}
