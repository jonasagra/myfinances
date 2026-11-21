using System;
using MyFinances.App.Infra;
using MyFinances.Core.Labor;

namespace MyFinances.App.ViewModels;

public sealed class PayslipImportRow(PayslipLine line, Action changed) : ObservableObject
{
	private bool _include = true;

	private bool _posted;

	public PayslipLine Line { get; } = line;

	public EntryKind Kind
	{
		get
		{
			if (Line.Kind != PayslipLineKind.Earning)
			{
				return EntryKind.Deduction;
			}
			return EntryKind.Earning;
		}
	}

	public string Table
	{
		get
		{
			if (Kind != EntryKind.Earning)
			{
				return "Desconto";
			}
			return "Provento";
		}
	}

	public string Name => Line.FriendlyName;

	public decimal Amount => Line.Amount;

	public bool Include
	{
		get
		{
			return _include;
		}
		set
		{
			if (Set(ref _include, value, "Include"))
			{
				changed();
			}
		}
	}

	public bool Posted
	{
		get
		{
			return _posted;
		}
		set
		{
			Set(ref _posted, value, "Posted");
			OnPropertyChanged("Status");
		}
	}

	public string Status
	{
		get
		{
			if (!_posted)
			{
				return "";
			}
			return "já está no mês";
		}
	}
}
