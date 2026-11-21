using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record PlanItem(YearMonth Month, EntryKind Kind, string Description, decimal Amount);
