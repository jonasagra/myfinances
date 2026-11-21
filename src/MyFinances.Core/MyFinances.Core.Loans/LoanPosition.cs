using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinances.Core.Loans;

public sealed record LoanPosition(Guid LoanId, DateOnly Reference, LoanStatus Status, int CurrentVersion, IReadOnlyList<InstallmentState> Installments, IReadOnlyList<PaymentAllocation> Allocations, decimal ContractedPrincipal, decimal OutstandingPrincipal, decimal ExpectedInterestTotal, decimal OverdueInterestOutstanding, decimal OutstandingCharges, decimal OverdueOutstanding, int OverdueInstallments, int LongestDelayDays, decimal WrittenOffLoss, DateOnly? WriteOffDate, decimal DiscountsGranted, decimal Surpluses)
{
	public decimal ReceivedTotal => Allocations.Sum((PaymentAllocation a) => a.Amount - a.Surplus);

	public decimal TotalOwed => Installments.Where((InstallmentState p) => !p.Superseded).Sum((InstallmentState p) => p.OutstandingTotal);

	public decimal Received(Component c)
	{
		return Allocations.Sum((PaymentAllocation a) => a.Total(c));
	}
}
