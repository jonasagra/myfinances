using System;

namespace MyFinances.Core.Loans;

public sealed record PaymentCalc(Guid Id, DateTime Timestamp, decimal Amount, bool Reversed = false, bool ExtraAmortization = false);
