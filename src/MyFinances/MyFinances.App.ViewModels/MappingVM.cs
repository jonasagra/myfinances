using System.Collections.Generic;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Labor;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class MappingVM(TableMapping m) : ObservableObject
{
	public TableMapping Model { get; } = m;

	public string Prefix => Model.Prefix;

	public string Quantity => $"{Model.TableCount} tabela(s)";

	public List<string> Columns => Model.Columns;

	public static IReadOnlyList<Option<EntryKind?>> Kinds { get; } = new _003C_003Ez__ReadOnlyArray<Option<EntryKind?>>(new Option<EntryKind?>[5]
	{
		new Option<EntryKind?>(null, "(ignorar)"),
		new Option<EntryKind?>(EntryKind.Earning, "Provento"),
		new Option<EntryKind?>(EntryKind.Deduction, "Desconto"),
		new Option<EntryKind?>(EntryKind.Inflow, "Entrada"),
		new Option<EntryKind?>(EntryKind.Expense, "Despesa")
	});

	public IReadOnlyList<Option<EntryKind?>> KindOptions => Kinds;

	public Option<EntryKind?> Kind
	{
		get
		{
			return Kinds.First((Option<EntryKind?> t) => t.Amount == Model.Kind);
		}
		set
		{
			Model.Kind = value?.Amount;
			OnPropertyChanged("Kind");
		}
	}

	public string DescriptionColumn
	{
		get
		{
			return Model.DescriptionColumn;
		}
		set
		{
			Model.DescriptionColumn = value;
			OnPropertyChanged("DescriptionColumn");
		}
	}

	public string AmountColumn
	{
		get
		{
			return Model.AmountColumn;
		}
		set
		{
			Model.AmountColumn = value;
			OnPropertyChanged("AmountColumn");
		}
	}
}
