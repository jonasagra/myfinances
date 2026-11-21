using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;
using MyFinances.Core.Common;

namespace MyFinances.App.Views;

public partial class FinanceView : UserControl, IComponentConnector, IStyleConnector
{
	private sealed class DragGhost : Adorner
	{
		private readonly Border _card;

		private Point _at;

		protected override int VisualChildrenCount => 1;

		public DragGhost(UIElement owner, EntryRow row)
			: base(owner)
		{
			IsHitTestVisible = false;
			Application res = Application.Current;
			TextBlock amount = new TextBlock
			{
				Text = Money.Fmt(row.Amount),
				FontWeight = FontWeights.SemiBold,
				Margin = new Thickness(16.0, 0.0, 0.0, 0.0)
			};
			DockPanel.SetDock(amount, Dock.Right);
			_card = new Border
			{
				Background = (Brush)res.FindResource("Surface3Brush"),
				BorderBrush = (Brush)res.FindResource("AccentBrush"),
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(12.0, 7.0, 12.0, 7.0),
				Opacity = 0.95,
				MaxWidth = 380.0,
				Effect = new DropShadowEffect
				{
					BlurRadius = 18.0,
					ShadowDepth = 4.0,
					Opacity = 0.55
				},
				Child = new DockPanel
				{
					Children = 
					{
						(UIElement)amount,
						(UIElement)new TextBlock
						{
							Text = row.Description,
							TextTrimming = TextTrimming.CharacterEllipsis
						}
					}
				}
			};
			TextElement.SetForeground(_card, (Brush)res.FindResource("TextBrush"));
			AddVisualChild(_card);
		}

		public void MoveTo(Point p)
		{
			_at = p;
			InvalidateArrange();
		}

		protected override Visual GetVisualChild(int index)
		{
			return _card;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			_card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return AdornedElement.RenderSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			_card.Arrange(new Rect(new Point(_at.X + 14.0, _at.Y + 10.0), _card.DesiredSize));
			return finalSize;
		}
	}

	private const double SideBySideWidth = 1500.0;

	private List<object>? _selectionBeforeCheck;

	private Point _pressAt;

	private EntryRow? _pressed;

	private DragGhost? _ghost;

	private Border? _hoverPanel;

	private DataGridRow? _hoverRow;

	private FinanceViewModel? Vm => DataContext as FinanceViewModel;

	public FinanceView()
	{
		FinanceView financeView = this;
		InitializeComponent();
		Window window = null;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			window = Window.GetWindow(financeView);
			if (window != null)
			{
				window.KeyDown += financeView.Window_KeyDown;
			}
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			if (window != null)
			{
				window.KeyDown -= financeView.Window_KeyDown;
			}
		};
	}

	private void Window_KeyDown(object sender, KeyEventArgs e)
	{
		if (Keyboard.Modifiers != ModifierKeys.Control || !IsVisible)
		{
			return;
		}
		FinanceViewModel vm = Vm;
		if (vm != null)
		{
			if (e.Key == Key.Z)
			{
				Dialogs.Guard(vm.Undo);
				e.Handled = true;
			}
			else if (e.Key == Key.M)
			{
				Dialogs.Guard(vm.MergeShortcut);
				e.Handled = true;
			}
			else if (Clipboard(e.Key, vm))
			{
				e.Handled = true;
			}
		}
	}

	private bool Clipboard(Key key, FinanceViewModel vm)
	{
		switch (key)
		{
		case Key.C:
			Dialogs.Guard(() =>
			{
				vm.Take(AllSelected(), cut: false);
			});
			return true;
		case Key.X:
			Dialogs.Guard(() =>
			{
				vm.Take(AllSelected(), cut: true);
			});
			return true;
		case Key.V:
			Dialogs.Guard(vm.Paste);
			return true;
		default:
			return false;
		}
	}

	private IEnumerable<DataGrid> Grids()
	{
		List<DataGrid> found = new List<DataGrid>();
		Walk(TablesHost);
		return found;
		void Walk(DependencyObject d)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
			{
				DependencyObject c = VisualTreeHelper.GetChild(d, i);
				if (c is DataGrid g)
				{
					found.Add(g);
				}
				else
				{
					Walk(c);
				}
			}
		}
	}

	private List<EntryRow> AllSelected()
	{
		return Grids().SelectMany((DataGrid g) => g.SelectedItems.OfType<EntryRow>()).Distinct().ToList();
	}

	private static DataGridRow? RowOf(object sender)
	{
		DependencyObject d = (DependencyObject)sender;
		while (d != null && !(d is DataGridRow))
		{
			d = VisualTreeHelper.GetParent(d);
		}
		return d as DataGridRow;
	}

	private void RowCheck_PreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		DataGridRow row = RowOf(sender);
		if (row != null && ItemsControl.ItemsControlFromItemContainer(row) is DataGrid g)
		{
			_selectionBeforeCheck = g.SelectedItems.Cast<object>().ToList();
		}
	}

	private void RowCheck_Click(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		DataGridRow row = RowOf(sender);
		if (row == null || !(ItemsControl.ItemsControlFromItemContainer(row) is DataGrid g))
		{
			return;
		}
		List<object> keep = _selectionBeforeCheck ?? g.SelectedItems.Cast<object>().ToList();
		_selectionBeforeCheck = null;
		if (!keep.Remove(row.Item))
		{
			keep.Add(row.Item);
		}
		g.SelectedItems.Clear();
		foreach (object i in keep)
		{
			g.SelectedItems.Add(i);
		}
	}

	private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is DataGrid { DataContext: EntryTable t } sg)
		{
			t.SetSelection(sg.SelectedItems.OfType<EntryRow>().ToList());
		}
		if (sender is DataGrid g)
		{
			FinanceViewModel vm = Vm;
			if (vm != null && (g.IsKeyboardFocusWithin | g.IsMouseOver))
			{
				vm.SelectedRows = AllSelected();
			}
		}
	}

	private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (GridRows.RowItem(e) is EntryRow { CanEdit: not false } r)
		{
			Vm?.EditCommand.Execute(r);
		}
	}

	private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (Keyboard.Modifiers == ModifierKeys.Control)
		{
			FinanceViewModel cv = Vm;
			if (cv != null && Clipboard(e.Key, cv))
			{
				e.Handled = true;
				return;
			}
		}
		if (sender is DataGrid { SelectedItem: EntryRow { CanEdit: not false } r } && Vm != null)
		{
			if (e.Key == Key.Delete)
			{
				Vm.DeleteCommand.Execute(r);
				e.Handled = true;
			}
			else if (e.Key == Key.Return)
			{
				Vm.EditCommand.Execute(r);
				e.Handled = true;
			}
		}
	}

	private void Search_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			FinanceViewModel vm = Vm;
			if (vm != null)
			{
				vm.Search = "";
				e.Handled = true;
			}
		}
	}

	private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_pressed = null;
		DependencyObject d = e.OriginalSource as DependencyObject;
		while (d != null && !(d is DataGridRow))
		{
			if (d is ButtonBase)
			{
				return;
			}
			d = VisualTreeHelper.GetParent(d);
		}
		if (Keyboard.Modifiers == ModifierKeys.None)
		{
			DataGrid me = sender as DataGrid;
			if (me != null)
			{
				foreach (DataGrid item in from g in Grids()
					where g != me
					select g)
				{
					item.UnselectAll();
				}
			}
		}
		if (GridRows.RowItem(e) is EntryRow { CanEdit: not false } r)
		{
			_pressed = r;
			_pressAt = e.GetPosition(this);
		}
	}

	private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		EntryRow row = _pressed;
		if ((object)row == null || e.LeftButton != MouseButtonState.Pressed)
		{
			_pressed = null;
			return;
		}
		Point p = e.GetPosition(this);
		if (Math.Abs(p.X - _pressAt.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y - _pressAt.Y) < SystemParameters.MinimumVerticalDragDistance)
		{
			return;
		}
		_pressed = null;
		AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
		_ghost = new DragGhost(this, row);
		_ghost.MoveTo(p);
		layer?.Add(_ghost);
		try
		{
			DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(EntryRow), row), DragDropEffects.Move);
		}
		finally
		{
			layer?.Remove(_ghost);
			_ghost = null;
			SetHover(null);
			SetHoverRow(null);
		}
	}

	private static EntryRow? Dragged(DragEventArgs e)
	{
		return e.Data.GetData(typeof(EntryRow)) as EntryRow;
	}

	private bool Accepts(object sender, DragEventArgs e, out EntryTable? table, out DataGridRow? onto)
	{
		table = (sender as FrameworkElement)?.DataContext as EntryTable;
		onto = null;
		EntryRow row = Dragged(e);
		if ((object)row != null && table != null)
		{
			EntryTable from = Vm?.Tables.FirstOrDefault((EntryTable t) => t.Rows.Contains(row));
			if (from != null)
			{
				if (from != table)
				{
					return true;
				}
				for (DependencyObject d = e.OriginalSource as DependencyObject; d != null; d = VisualTreeHelper.GetParent(d))
				{
					if (d is DataGridRow r)
					{
						onto = r;
						break;
					}
				}
				if (onto?.Item is EntryRow { CanEdit: not false } target && target != row)
				{
					return true;
				}
				onto = null;
				return false;
			}
		}
		return false;
	}

	private static string? PdfOf(DragEventArgs e)
	{
		if (!(e.Data.GetData(DataFormats.FileDrop) is string[] f) || f.Length != 1 || !f[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return f[0];
	}

	private void Root_Drop(object sender, DragEventArgs e)
	{
		string path = PdfOf(e);
		if (path == null)
		{
			return;
		}
		FinanceViewModel vm = Vm;
		if (vm == null)
		{
			return;
		}
		e.Handled = true;
		Dispatcher.BeginInvoke((Action)(() =>
		{
			Dialogs.Guard(() =>
			{
				vm.ImportPayslip(path);
			});
		}));
	}

	private void Panel_DragOver(object sender, DragEventArgs e)
	{
		if (PdfOf(e) == null)
		{
			bool ok = Accepts(sender, e, out EntryTable _, out DataGridRow onto);
			e.Effects = (ok ? DragDropEffects.Move : DragDropEffects.None);
			SetHover((ok && onto == null) ? (sender as Border) : null);
			SetHoverRow(onto);
			e.Handled = true;
		}
	}

	private void SetHoverRow(DataGridRow? row)
	{
		if (row != _hoverRow)
		{
			if (_hoverRow != null)
			{
				_hoverRow.ClearValue(Control.BorderBrushProperty);
				_hoverRow.ClearValue(Control.BorderThicknessProperty);
			}
			_hoverRow = row;
			if (row != null)
			{
				row.BorderBrush = (Brush)FindResource("AccentBrush");
				row.BorderThickness = new Thickness(1.5);
			}
		}
	}

	private void Panel_DragLeave(object sender, DragEventArgs e)
	{
		if (sender == _hoverPanel)
		{
			SetHover(null);
		}
	}

	private void Panel_Drop(object sender, DragEventArgs e)
	{
		if (PdfOf(e) != null)
		{
			return;
		}
		SetHover(null);
		SetHoverRow(null);
		if (!Accepts(sender, e, out EntryTable table, out DataGridRow onto))
		{
			return;
		}
		EntryRow row = Dragged(e);
		if ((object)row == null)
		{
			return;
		}
		FinanceViewModel vm = Vm;
		if (vm == null)
		{
			return;
		}
		e.Handled = true;
		object obj = onto?.Item;
		EntryRow target = obj as EntryRow;
		if ((object)target != null)
		{
			Dialogs.Guard(() =>
			{
				vm.MergeRows(new _003C_003Ez__ReadOnlyArray<EntryRow>(new EntryRow[2] { target, row }), target.Description);
			});
		}
		else
		{
			Dialogs.Guard(() =>
			{
				vm.MoveEntry(row, table.Kind);
			});
		}
	}

	private void Root_PreviewDragOver(object sender, DragEventArgs e)
	{
		_ghost?.MoveTo(e.GetPosition(this));
	}

	private void Root_DragOver(object sender, DragEventArgs e)
	{
		e.Effects = ((PdfOf(e) != null) ? DragDropEffects.Copy : DragDropEffects.None);
		SetHover(null);
		SetHoverRow(null);
		e.Handled = true;
	}

	private void SetHover(Border? panel)
	{
		if (panel != _hoverPanel)
		{
			_hoverPanel?.ClearValue(Border.BorderBrushProperty);
			_hoverPanel?.ClearValue(Border.BackgroundProperty);
			_hoverPanel = panel;
			if (panel != null)
			{
				panel.BorderBrush = (Brush)FindResource("AccentBrush");
				panel.Background = (Brush)FindResource("Surface2Brush");
			}
		}
	}

	private void TablesHost_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UniformGrid panel = FindChild<UniformGrid>(TablesHost);
		if (panel != null)
		{
			panel.Columns = ((e.NewSize.Width >= 1500.0) ? 4 : 2);
		}
	}

	private static T? FindChild<T>(DependencyObject d) where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
		{
			DependencyObject c = VisualTreeHelper.GetChild(d, i);
			if (c is T t)
			{
				return t;
			}
			T found = FindChild<T>(c);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}
}
