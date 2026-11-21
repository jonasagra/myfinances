using System;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Inss
{
	public static decimal Calculate(decimal calcBase, InssTable table)
	{
		return CalculateWithBreakdown(calcBase, table).Amount;
	}

	public static (decimal Amount, CalcBreakdown Breakdown) CalculateWithBreakdown(decimal calcBase, InssTable table)
	{
		CalcBreakdown bd = new CalcBreakdown();
		if (calcBase <= 0m)
		{
			bd.Add("INSS (base zero)", 0m);
			return (Amount: 0m, Breakdown: bd);
		}
		decimal b = Math.Min(calcBase, table.Ceiling);
		if (calcBase > table.Ceiling)
		{
			bd.Text("Base " + Money.Fmt(calcBase) + " limitada ao teto " + Money.Fmt(table.Ceiling));
		}
		decimal previous = 0m;
		decimal total = 0m;
		decimal totalGross = 0m;
		foreach (InssBand f in table.Bands)
		{
			if (b <= previous)
			{
				break;
			}
			decimal part = Math.Min(b, f.Until) - previous;
			decimal gross = part * f.RatePct / 100m;
			decimal v = ((table.Rounding == InssRounding.TruncatePerBand) ? Money.Trunc2(gross) : gross);
			bd.Add("Faixa até " + Money.Fmt(f.Until), Money.R2(v), Money.Num(part) + " × " + Money.Pct(f.RatePct, 1) + ((table.Rounding == InssRounding.TruncatePerBand) ? ", truncado" : ""));
			total += v;
			totalGross += gross;
			previous = f.Until;
		}
		decimal final = ((table.Rounding == InssRounding.TruncatePerBand) ? total : Money.R2(totalGross));
		bd.Add("= INSS", final);
		return (Amount: final, Breakdown: bd);
	}
}
