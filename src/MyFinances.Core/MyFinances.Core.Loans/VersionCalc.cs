using System;
using System.Collections.Generic;

namespace MyFinances.Core.Loans;

public sealed record VersionCalc(int Number, DateTime Start, VersionKind Kind, LoanTerms Terms, IReadOnlyList<ScheduledInstallment> Installments, decimal DiscountGranted = 0m, string? Reason = null);
