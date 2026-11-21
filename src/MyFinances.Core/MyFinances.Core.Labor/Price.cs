using System;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Price
{
	public static decimal Installment(decimal pv, decimal i, int n)
	{
		if (n <= 0)
		{
			throw new ArgumentOutOfRangeException("n");
		}
		if (i == 0m)
		{
			return Money.R2(pv / (decimal)n);
		}
		decimal f = Money.Pow(1m + i, n);
		return Money.R2(pv * i * f / (f - 1m));
	}

	public static decimal ImplicitRate(decimal pv, decimal pmtValue, int n)
	{
		if (pmtValue * (decimal)n <= pv)
		{
			return 0m;
		}
		decimal lo = 0m;
		decimal hi = 1m;
		for (int k = 0; k < 200; k++)
		{
			decimal mid = (lo + hi) / 2m;
			decimal f = Money.Pow(1m + mid, n);
			if (pmtValue * (f - 1m) / (mid * f) > pv)
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		return (lo + hi) / 2m;
	}
}
