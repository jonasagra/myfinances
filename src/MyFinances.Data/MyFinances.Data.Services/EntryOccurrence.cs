using System;
using MyFinances.Core.Common;

namespace MyFinances.Data.Services;

public sealed record EntryOccurrence(Guid Id, YearMonth Month, string Description, decimal Amount);
