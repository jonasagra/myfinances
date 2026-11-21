using System;
using MyFinances.Core.Loans;

namespace MyFinances.App.ViewModels;

public sealed record InstallmentRow(int Version, int Number, DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee, decimal Total, decimal Paid, decimal Charges, decimal Outstanding, int DaysLate, InstallmentStatus Status, string Breakdown)
{
	public string StatusText => LoanEngine.Name(Status);
}
