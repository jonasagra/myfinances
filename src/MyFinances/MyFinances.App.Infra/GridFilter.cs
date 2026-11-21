using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MyFinances.App.Infra;

public static class GridFilter
{
	public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(GridFilter), new PropertyMetadata(false, OnEnabledChanged));

	public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(GridFilter), new PropertyMetadata(false));

	private static readonly DependencyProperty HiddenProperty = DependencyProperty.RegisterAttached("Hidden", typeof(Dictionary<DataGridColumn, HashSet<string>>), typeof(GridFilter));

	public static ICommand OpenCommand { get; } = new RelayCommand((object? p) =>
	{
		if (p is DataGridColumnHeader header)
		{
			Open(header);
		}
	});

	public static bool GetIsEnabled(DependencyObject o)
	{
		return (bool)o.GetValue(IsEnabledProperty);
	}

	public static void SetIsEnabled(DependencyObject o, bool v)
	{
		o.SetValue(IsEnabledProperty, v);
	}

	public static bool GetIsActive(DependencyObject o)
	{
		return (bool)o.GetValue(IsActiveProperty);
	}

	public static void SetIsActive(DependencyObject o, bool v)
	{
		o.SetValue(IsActiveProperty, v);
	}

	private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		DataGrid g = d as DataGrid;
		if (g == null)
		{
			return;
		}
		object newValue = e.NewValue;
		if (newValue is bool && (bool)newValue)
		{
			g.SetValue(HiddenProperty, new Dictionary<DataGridColumn, HashSet<string>>());
			g.Loaded += (object _, RoutedEventArgs _) =>
			{
				AddHeaders(g);
			};
		}
	}

	private static Dictionary<DataGridColumn, HashSet<string>> Hidden(DataGrid g)
	{
		return (Dictionary<DataGridColumn, HashSet<string>>)g.GetValue(HiddenProperty);
	}

	private static void AddHeaders(DataGrid g)
	{
		DataTemplate template = (DataTemplate)Application.Current.FindResource("FilterHeaderTemplate");
		foreach (DataGridColumn c in g.Columns)
		{
			if ((c is DataGridBoundColumn || !string.IsNullOrEmpty(c.SortMemberPath)) && c.Header is string { Length: >0 } && c.HeaderTemplate == null)
			{
				c.HeaderTemplate = template;
			}
		}
	}

	private static object? Resolve(object? item, string? path)
	{
		if (item == null || string.IsNullOrEmpty(path))
		{
			return item;
		}
		string[] array = path.Split('.');
		foreach (string part in array)
		{
			if (item == null)
			{
				return null;
			}
			item = item.GetType().GetProperty(part)?.GetValue(item);
		}
		return item;
	}

	private static string Display(DataGridColumn col, object item)
	{
		object v;
		if (col is DataGridBoundColumn { Binding: Binding b })
		{
			v = Resolve(item, b.Path?.Path);
			IValueConverter conv = b.Converter;
			if (conv != null)
			{
				v = conv.Convert(v, typeof(string), b.ConverterParameter, CultureInfo.CurrentCulture);
			}
		}
		else
		{
			if (string.IsNullOrEmpty(col.SortMemberPath))
			{
				return "";
			}
			v = Resolve(item, col.SortMemberPath);
		}
		if (v != null)
		{
			if (!(v is string s))
			{
				if (v is IFormattable f)
				{
					return f.ToString(null, CultureInfo.CurrentCulture);
				}
				return v.ToString() ?? "";
			}
			return s;
		}
		return "";
	}

	private static object? SortValue(DataGridColumn col, object item)
	{
		object item2 = item;
		string? path;
		if (string.IsNullOrEmpty(col.SortMemberPath))
		{
			path = ((!((col as DataGridBoundColumn)?.Binding is Binding { Path: var path2 })) ? null : path2?.Path);
		}
		else
		{
			path = col.SortMemberPath;
		}
		return Resolve(item2, path);
	}

	private static IEnumerable<object> SourceItems(DataGrid g)
	{
		IEnumerable itemsSource = g.ItemsSource;
		return ((itemsSource != null) ? itemsSource.Cast<object>() : null) ?? Array.Empty<object>();
	}

	private static void Apply(DataGrid g)
	{
		Dictionary<DataGridColumn, HashSet<string>> hidden = Hidden(g);
		foreach (DataGridColumn c in g.Columns)
		{
			SetIsActive(c, hidden.TryGetValue(c, out var h) && h.Count > 0);
		}
		List<KeyValuePair<DataGridColumn, HashSet<string>>> active = hidden.Where((KeyValuePair<DataGridColumn, HashSet<string>> kv) => kv.Value.Count > 0).ToList();
		g.Items.Filter = ((active.Count == 0) ? null : ((Predicate<object>)((object item) => active.All((KeyValuePair<DataGridColumn, HashSet<string>> kv) => !kv.Value.Contains(Display(kv.Key, item))))));
	}

	private static void Sort(DataGrid g, DataGridColumn col, ListSortDirection dir)
	{
		string path = ((!string.IsNullOrEmpty(col.SortMemberPath)) ? col.SortMemberPath : ((col as DataGridBoundColumn)?.Binding as Binding)?.Path?.Path);
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		g.Items.SortDescriptions.Clear();
		g.Items.SortDescriptions.Add(new SortDescription(path, dir));
		foreach (DataGridColumn column in g.Columns)
		{
			column.SortDirection = null;
		}
		col.SortDirection = dir;
	}

	public static void Clear(DataGrid g)
	{
		Hidden(g).Clear();
		Apply(g);
	}

	private static T? Ancestor<T>(DependencyObject? d) where T : DependencyObject
	{
		while (d != null && !(d is T))
		{
			d = VisualTreeHelper.GetParent(d);
		}
		return d as T;
	}

	private static void Open(DataGridColumnHeader header)
	{
		DataGridColumn col = header.Column;
		if (col == null)
		{
			return;
		}
		DataGrid g = Ancestor<DataGrid>(header);
		if (g == null)
		{
			return;
		}
		Dictionary<DataGridColumn, HashSet<string>> hidden = Hidden(g);
		hidden.TryGetValue(col, out HashSet<string> hiddenNow);
		List<(string Text, object Key)> values = (from i in SourceItems(g)
			select (Text: Display(col, i), Key: SortValue(col, i)) into x
			group x by x.Text into x
			select x.First()).ToList();
		try
		{
			values = values.OrderBy(((string Text, object Key) x) => x.Key, Comparer<object>.Default).ToList();
		}
		catch (ArgumentException)
		{
			values = values.OrderBy(((string Text, object Key) x) => x.Text, StringComparer.CurrentCultureIgnoreCase).ToList();
		}
		foreach (string gone in (hiddenNow ?? new HashSet<string>()).Where((string h) => values.All(((string Text, object Key) v) => v.Text != h)).ToList())
		{
			values.Add((gone, null));
		}
		Application res = Application.Current;
		Popup popup = new Popup
		{
			PlacementTarget = header,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			AllowsTransparency = true
		};
		StackPanel root = new StackPanel();
		Border border = new Border
		{
			Background = (Brush)res.FindResource("Surface2Brush"),
			BorderBrush = (Brush)res.FindResource("LineBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(10.0),
			Width = 270.0,
			Child = root,
			Effect = new DropShadowEffect
			{
				BlurRadius = 16.0,
				ShadowDepth = 3.0,
				Opacity = 0.5
			}
		};
		popup.Child = border;
		root.Children.Add(Link("↑  Ordenar do menor para o maior", () =>
		{
			Sort(g, col, ListSortDirection.Ascending);
		}));
		root.Children.Add(Link("↓  Ordenar do maior para o menor", () =>
		{
			Sort(g, col, ListSortDirection.Descending);
		}));
		root.Children.Add(new Border
		{
			Height = 1.0,
			Background = (Brush)res.FindResource("LineBrush"),
			Margin = new Thickness(0.0, 6.0, 0.0, 8.0)
		});
		TextBox search = new TextBox
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			ToolTip = "Buscar valor"
		};
		root.Children.Add(new TextBlock
		{
			Text = "Buscar",
			Style = (Style)res.FindResource("FieldLabel")
		});
		root.Children.Add(search);
		CheckBox all = new CheckBox
		{
			Content = "(Selecionar tudo)",
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(2.0, 4.0, 0.0, 4.0)
		};
		root.Children.Add(all);
		StackPanel list = new StackPanel();
		List<CheckBox> boxes = values.Select(((string Text, object Key) v) =>
		{
			CheckBox checkBox = new CheckBox();
			string content;
			if (v.Text.Length != 0)
			{
				(content, _) = v;
			}
			else
			{
				content = "(vazio)";
			}
			checkBox.Content = content;
			checkBox.Tag = v.Text;
			checkBox.IsChecked = hiddenNow == null || !hiddenNow.Contains(v.Text);
			checkBox.Margin = new Thickness(2.0, 2.0, 0.0, 2.0);
			return checkBox;
		}).ToList();
		foreach (CheckBox b in boxes)
		{
			list.Children.Add(b);
		}
		root.Children.Add(new Border
		{
			BorderBrush = (Brush)res.FindResource("LineBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(6.0, 4.0, 6.0, 4.0),
			Child = new ScrollViewer
			{
				MaxHeight = 240.0,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Content = list
			}
		});
		bool syncing = false;
		all.Click += (object _, RoutedEventArgs _) =>
		{
			bool valueOrDefault = all.IsChecked == true;
			foreach (CheckBox item in boxes.Where((CheckBox checkBox) => checkBox.Visibility == Visibility.Visible))
			{
				item.IsChecked = valueOrDefault;
			}
		};
		foreach (CheckBox item2 in boxes)
		{
			item2.Checked += (object _, RoutedEventArgs _) =>
			{
				if (!syncing)
				{
					SyncAll();
				}
			};
			item2.Unchecked += (object _, RoutedEventArgs _) =>
			{
				if (!syncing)
				{
					SyncAll();
				}
			};
		}
		search.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			string text = search.Text.Trim();
			foreach (CheckBox current2 in boxes)
			{
				current2.Visibility = ((text.Length != 0 && !((string)current2.Tag).Contains(text, StringComparison.CurrentCultureIgnoreCase)) ? Visibility.Collapsed : Visibility.Visible);
			}
			SyncAll();
		};
		SyncAll();
		StackPanel buttons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Button clear = new Button
		{
			Content = "Limpar",
			Style = (Style)res.FindResource("SecondaryButton")
		};
		clear.Click += (object _, RoutedEventArgs _) =>
		{
			hidden.Remove(col);
			Apply(g);
			popup.IsOpen = false;
		};
		Button ok = new Button
		{
			Content = "Aplicar",
			Margin = new Thickness(0.0),
			IsDefault = true
		};
		ok.Click += (object _, RoutedEventArgs _) =>
		{
			string t = search.Text.Trim();
			HashSet<string> hashSet = (from checkBox in boxes
				where checkBox.IsChecked != true || (t.Length > 0 && checkBox.Visibility != Visibility.Visible)
				select (string)checkBox.Tag).ToHashSet();
			if (hashSet.Count == 0)
			{
				hidden.Remove(col);
			}
			else
			{
				hidden[col] = hashSet;
			}
			Apply(g);
			popup.IsOpen = false;
		};
		buttons.Children.Add(clear);
		buttons.Children.Add(ok);
		root.Children.Add(buttons);
		popup.Opened += (object? _, EventArgs _) =>
		{
			search.Focus();
		};
		popup.KeyDown += (object _, KeyEventArgs e) =>
		{
			if (e.Key == Key.Escape)
			{
				popup.IsOpen = false;
			}
		};
		popup.IsOpen = true;
		Button Link(string text, Action act)
		{
			Button button = new Button();
			button.Content = text;
			button.Style = (Style)res.FindResource("LinkButton");
			button.HorizontalAlignment = HorizontalAlignment.Stretch;
			button.HorizontalContentAlignment = HorizontalAlignment.Left;
			button.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
			button.Click += (object _, RoutedEventArgs _) =>
			{
				act();
				popup.IsOpen = false;
			};
			return button;
		}
		void SyncAll()
		{
			syncing = true;
			List<CheckBox> visible = boxes.Where((CheckBox checkBox2) => checkBox2.Visibility == Visibility.Visible).ToList();
			CheckBox checkBox = all;
			bool? isChecked;
			if (visible.All((CheckBox checkBox2) => checkBox2.IsChecked == true))
			{
				isChecked = true;
			}
			else
			{
				isChecked = (visible.All((CheckBox checkBox2) => checkBox2.IsChecked != true) ? new bool?(false) : ((bool?)null));
			}
			checkBox.IsChecked = isChecked;
			syncing = false;
		}
	}
}
