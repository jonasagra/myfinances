using MyFinances.Core.Labor;

namespace MyFinances.App.ViewModels;

public sealed record PlanRow(string Month, string Table, string Description, decimal Amount)
{
	public static PlanRow From(PlanItem i)
	{
		string sheetName = i.Month.SheetName;
		return new PlanRow(sheetName, i.Kind switch
		{
			EntryKind.Earning => "Proventos", 
			EntryKind.Deduction => "Descontos", 
			EntryKind.Inflow => "Entradas", 
			_ => "Despesas", 
		}, i.Description, i.Amount);
	}
}
