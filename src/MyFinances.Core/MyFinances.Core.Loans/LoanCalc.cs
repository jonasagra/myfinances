using System;
using System.Collections.Generic;

namespace MyFinances.Core.Loans;

public sealed record LoanCalc(Guid Id, IReadOnlyList<VersionCalc> Versions, IReadOnlyList<PaymentCalc> Payments, SpecialState State = SpecialState.None, DateTime? StateDate = null);
