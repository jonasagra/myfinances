using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record TerminationResult(int YearsOfService, int MonthsOfService, int NoticeDays, int PaidNoticeDays, DateOnly ProjectedEnd, DateOnly PaymentDeadline, decimal SalaryBalance, int BalanceDays, decimal PaidNotice, decimal Thirteenth, decimal ProportionalVacation, decimal ProportionalThird, decimal DueVacation, decimal DueThird, decimal NoticeDeduction, decimal Gross, decimal InssBalance, decimal Inss13, decimal PayrollLoanWithholding, decimal SeveranceNet, decimal FgtsDeposit, decimal FgtsPenalty, decimal PayrollLoanFgtsCollateral, decimal FgtsWithdrawal, int BenefitInstallments, decimal BenefitInstallmentAmount, decimal RemainingPayrollLoan, decimal TotalIncoming, IReadOnlyList<string> Warnings, CalcBreakdown SeveranceItems, CalcBreakdown Others);
