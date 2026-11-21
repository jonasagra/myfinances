using System;
using System.Collections.Generic;

namespace MyFinances.Core.Loans;

public sealed record LoanIndicators(DateOnly Start, DateOnly End, DateOnly Reference, int ActiveLoans, int LateLoans, int AtRiskLoans, decimal CapitalLentInPeriod, decimal PrincipalAmortizedInPeriod, decimal PrincipalOwed, decimal TotalOwed, int OverdueInstallments, decimal OverdueAmount, decimal ExpectedInterestInPeriod, decimal InterestReceivedInPeriod, decimal ChargesReceivedInPeriod, decimal FeesReceivedInPeriod, decimal RecoveredAfterWriteOff, decimal WriteOffsInPeriod, decimal DiscountsGranted, decimal RealizedProfit, decimal InterestReceivable, decimal EstimatedLoss, decimal ProjectedProfit, decimal? CapitalLimit, decimal? AvailableCapital, IReadOnlyList<LoanMonthlySeries> Series, string Formulas);
