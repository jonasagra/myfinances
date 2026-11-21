using System;
using System.Linq;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Irrf
{
	public static (decimal Amount, CalcBreakdown Breakdown) Calculate(decimal taxableIncome, decimal inss, int dependents, decimal alimony, IrrfTable t, bool applyReduction = true)
	{
		CalcBreakdown bd = new CalcBreakdown();
		if (taxableIncome <= 0m)
		{
			bd.Add("IRRF", 0m);
			return (Amount: 0m, Breakdown: bd);
		}
		decimal legal = inss + (decimal)dependents * t.DependentDeduction + alimony;
		bool useSimplified = t.SimplifiedDeduction > legal;
		decimal deduction = (useSimplified ? t.SimplifiedDeduction : legal);
		decimal irBase = Math.Max(0m, taxableIncome - deduction);
		bd.Add("Rendimento tributável", taxableIncome);
		bd.Add(useSimplified ? "(-) Desconto simplificado" : "(-) Deduções legais (INSS + dependentes + pensão)", deduction);
		bd.Add("= Base do IRRF", irBase);
		IrrfBand band = t.Bands.First((IrrfBand f) => irBase <= f.Until);
		decimal tax = Money.R2(Math.Max(0m, irBase * band.RatePct / 100m - band.DeductibleAmount));
		bd.Add("Imposto pela tabela", tax, $"{Money.Num(irBase)} × {Money.Pct(band.RatePct, 1)} − {Money.Num(band.DeductibleAmount)}");
		if (applyReduction && tax > 0m)
		{
			decimal red = 0m;
			if (taxableIncome <= t.RedExemptLimit)
			{
				red = Math.Min(tax, t.RedMax);
			}
			else if (taxableIncome <= t.RedUpperLimit)
			{
				red = Math.Min(tax, Math.Max(0m, Money.R2(t.RedConstant - t.RedFactor * taxableIncome)));
			}
			if (red > 0m)
			{
				bd.Add("(-) Redução Lei 15.270/2025", red);
			}
			tax -= red;
		}
		bd.Add("= IRRF", tax);
		return (Amount: tax, Breakdown: bd);
	}
}
