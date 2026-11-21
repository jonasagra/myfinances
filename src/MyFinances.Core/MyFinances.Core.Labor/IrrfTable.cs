using System.Collections.Generic;

namespace MyFinances.Core.Labor;

public sealed record IrrfTable(int Year, IReadOnlyList<IrrfBand> Bands, decimal DependentDeduction, decimal SimplifiedDeduction, decimal RedExemptLimit, decimal RedMax, decimal RedUpperLimit, decimal RedConstant, decimal RedFactor, string Source, bool Confirmed);
