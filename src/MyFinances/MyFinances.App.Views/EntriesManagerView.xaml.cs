using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.App.ViewModels;

namespace MyFinances.App.Views;

public partial class EntriesManagerView : UserControl, IComponentConnector
{
	public EntriesManagerView()
	{
		InitializeComponent();
	}

	private void Occurrences_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		object obj = GridRows.RowItem(e);
		OccurrenceRow r = obj as OccurrenceRow;
		if (r == null)
		{
			return;
		}
		obj = DataContext;
		EntriesManagerViewModel vm = obj as EntriesManagerViewModel;
		if (vm != null)
		{
			Dialogs.Guard(() =>
			{
				vm.EditOccurrence(r);
			});
		}
	}
}
