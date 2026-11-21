using System;
using MyFinances.Core.Loans;

namespace MyFinances.Data.Services;

public sealed record LoanSummary(Guid Id, int Code, Guid ClientId, string Client, DateOnly ContractDate, decimal Principal, decimal OutstandingPrincipal, decimal TotalBalance, LoanStatus Status, int OverdueInstallments, int LongestDelay, DateOnly? NextDueDate, decimal NextValue, int Version)
{
	public string StatusText => LoanEngine.Name(Status);
}
