using System;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.App.ViewModels;

public sealed record EntryEdit(Guid? Id, EntryKind Kind, YearMonth Month, string Description, decimal? Amount);
