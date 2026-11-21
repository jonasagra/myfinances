using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record RetroactiveResult(decimal Difference, decimal RetroactiveAmount, decimal Difference13th, decimal InssAdjustment, IReadOnlyList<PlanItem> Plan, CalcBreakdown Breakdown);
