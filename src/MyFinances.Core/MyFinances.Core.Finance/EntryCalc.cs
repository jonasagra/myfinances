using MyFinances.Core.Labor;

namespace MyFinances.Core.Finance;

public sealed record EntryCalc(EntryKind Kind, string Description, decimal Amount, string? Category = null);
