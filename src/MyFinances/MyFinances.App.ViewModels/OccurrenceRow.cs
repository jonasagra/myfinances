using MyFinances.App.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class OccurrenceRow(EntryOccurrence o) : ObservableObject
{
	private bool _checked = true;

	public EntryOccurrence Occurrence { get; } = o;

	public string Month => Occurrence.Month.SheetName;

	public int Key => Occurrence.Month.Year * 100 + Occurrence.Month.Month;

	public string Description => Occurrence.Description;

	public decimal Amount => Occurrence.Amount;

	public bool Checked
	{
		get
		{
			return _checked;
		}
		set
		{
			Set(ref _checked, value, "Checked");
		}
	}
}
