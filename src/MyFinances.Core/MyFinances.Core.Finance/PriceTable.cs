using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Finance;

public static class PriceTable
{
	public static double ImpliedRate(decimal financed, decimal installment, int count)
	{
		if (financed <= 0m || installment <= 0m || count < 1)
		{
			throw new ArgumentException("Preencha valor financiado, parcela e número de parcelas.");
		}
		if (installment * (decimal)count <= financed)
		{
			return 0.0;
		}
		double pv = (double)financed;
		double pmt = (double)installment;
		double low = 0.0;
		double high = 1.0;
		for (int i = 0; i < 200; i++)
		{
			double mid = (low + high) / 2.0;
			if (pmt * (1.0 - Math.Pow(1.0 + mid, -count)) / mid > pv)
			{
				low = mid;
			}
			else
			{
				high = mid;
			}
		}
		return (low + high) / 2.0;
	}

	public static List<PriceRow> Build(decimal financed, decimal installment, int count)
	{
		double rate = ImpliedRate(financed, installment, count);
		List<PriceRow> rows = new List<PriceRow>();
		decimal balance = financed;
		for (int k = 1; k <= count; k++)
		{
			decimal interest = Money.R2(balance * (decimal)rate);
			decimal amortization = ((k == count) ? balance : (installment - interest));
			balance = Money.R2(balance - amortization);
			rows.Add(new PriceRow(k, (k == count) ? (interest + amortization) : installment, interest, amortization, Math.Max(0m, balance)));
		}
		return rows;
	}
}
