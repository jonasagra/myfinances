using MyFinances.Core.Labor;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed record SeriesRow(EntrySeries Series)
{
	public string Kind => Series.Kind switch
	{
		EntryKind.Earning => "Provento", 
		EntryKind.Deduction => "Desconto", 
		EntryKind.Inflow => "Entrada", 
		_ => "Despesa", 
	};

	public string Name => Series.Name;

	public string Months
	{
		get
		{
			if (!(Series.First == Series.Last))
			{
				return $"{Series.First.SheetName} → {Series.Last.SheetName} ({Series.Count})";
			}
			return Series.First.SheetName;
		}
	}

	public int FirstKey => Series.First.Year * 100 + Series.First.Month;

	public int Count => Series.Count;

	public string Situation
	{
		get
		{
			if (!Series.TotalInstallments.HasValue)
			{
				if (Series.Count != 1)
				{
					return "Fixo / repetido";
				}
				return "Só um mês";
			}
			return "Parcelado";
		}
	}

	public decimal LastAmount => Series.LastAmount;

	public string Installments
	{
		get
		{
			int? totalInstallments = Series.TotalInstallments;
			if (totalInstallments.HasValue)
			{
				int n = totalInstallments.GetValueOrDefault();
				return $"{Series.LastInstallment}/{n}";
			}
			return "";
		}
	}
}
