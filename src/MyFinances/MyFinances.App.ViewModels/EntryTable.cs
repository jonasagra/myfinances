using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.App.ViewModels;

public sealed class EntryTable(string title, EntryKind kind, string itemName, string totalLabel) : ObservableObject
{
	private decimal _total;

	private IReadOnlyList<EntryRow> _picked = Array.Empty<EntryRow>();

	public string Title { get; } = title;

	public EntryKind Kind { get; } = kind;

	public string KindKey => Kind.ToString();

	public string AddTip => "Adicionar " + itemName.ToLowerInvariant() + " neste mês";

	public string ItemName { get; } = itemName;

	public string TotalLabel { get; } = totalLabel;

	public decimal Total
	{
		get
		{
			return _total;
		}
		set
		{
			if (Set(ref _total, value, "Total"))
			{
				OnPropertyChanged("ShownTotal");
			}
		}
	}

	public ObservableCollection<EntryRow> Rows { get; } = new ObservableCollection<EntryRow>();

	public bool IsSelectionTotal => _picked.Count > 1;

	public decimal ShownTotal
	{
		get
		{
			if (!IsSelectionTotal)
			{
				return Total;
			}
			return _picked.Sum((EntryRow r) => r.Amount);
		}
	}

	public string ShownTotalTip
	{
		get
		{
			if (!IsSelectionTotal)
			{
				return TotalLabel;
			}
			return $"Soma dos {_picked.Count} selecionados (total da tabela: {Money.Fmt(Total)})";
		}
	}

	public void SetSelection(IReadOnlyList<EntryRow> rows)
	{
		_picked = rows;
		OnPropertyChanged("IsSelectionTotal");
		OnPropertyChanged("ShownTotal");
		OnPropertyChanged("ShownTotalTip");
	}
}
