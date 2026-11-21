using System.Collections.Generic;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Core.Finance;

public sealed record MonthCalc(YearMonth Month, decimal BaseSalary, MonthAbsences Absences, IReadOnlyList<EntryCalc> Entries);
