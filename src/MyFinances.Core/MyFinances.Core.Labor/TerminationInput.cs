using System;

namespace MyFinances.Core.Labor;

public sealed record TerminationInput
{
	public TerminationKind Kind { get; init; }

	public NoticeKind Notice { get; init; }

	public DateOnly LastDay { get; init; }

	public DateOnly HireDate { get; init; }

	public decimal Salary { get; init; }

	public decimal OvertimeAverage { get; init; }

	public ThirteenthAlreadyReceived Thirteenth { get; init; }

	public decimal FirstInstallmentPaid { get; init; }

	public int DueVacationDays { get; init; }

	public int BenefitPriorClaims { get; init; }

	public decimal FgtsBalance { get; init; }

	public bool BirthdayWithdrawal { get; init; }

	public decimal PayrollLoanBalance { get; init; }

	public bool PayrollLoanFgtsCollateral { get; init; } = true;

	public bool WorksNoticeOnResignation { get; init; } = true;
}
