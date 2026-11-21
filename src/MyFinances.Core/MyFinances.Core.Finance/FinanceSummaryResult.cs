using System.Collections.Generic;
using MyFinances.Core.Labor;

namespace MyFinances.Core.Finance;

public sealed record FinanceSummaryResult(IReadOnlyList<MonthSummary> Months, IReadOnlyList<ThirteenthResult> ThirteenthOptions, decimal OpeningBalance);
