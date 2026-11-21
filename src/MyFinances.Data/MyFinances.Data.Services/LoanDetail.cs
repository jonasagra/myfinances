using System.Collections.Generic;
using MyFinances.Core.Loans;

namespace MyFinances.Data.Services;

public sealed record LoanDetail(Loan Entity, string Client, LoanCalc Calc, LoanPosition Position, IReadOnlyList<VersionCalc> Versions, IReadOnlyList<Payment> Payments);
