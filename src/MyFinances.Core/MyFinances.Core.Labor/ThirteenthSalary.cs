using System;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class ThirteenthSalary
{
	public static ThirteenthResult Calculate(int year, decimal decSalary, decimal novSalary, decimal yearEarningsSum, int twelfths, InssTable inss)
	{
		CalcBreakdown bd = new CalcBreakdown();
		twelfths = Math.Clamp(twelfths, 0, 12);
		decimal salary = ((decSalary > 0m) ? decSalary : novSalary);
		decimal average = Money.R2(yearEarningsSum / 12m);
		decimal fullGross = salary + average;
		decimal gross = ((twelfths == 12) ? fullGross : Money.R2(fullGross * (decimal)twelfths / 12m));
		decimal p1 = Money.R2(((novSalary > 0m) ? novSalary : salary) / 2m * ((twelfths == 12) ? 1m : ((decimal)twelfths / 12m)));
		decimal vInss = Inss.Calculate(gross, inss);
		decimal p2 = gross - p1 - vInss;
		bd.Add($"Salário de referência ({year})", salary, (decSalary > 0m) ? "salário do mês DEZ" : "salário do mês NOV");
		bd.Add("Média de extras", average, Money.Num(yearEarningsSum) + " ÷ 12");
		if (twelfths != 12)
		{
			bd.Add($"13º bruto ({twelfths}/12 avos)", gross, $"({Money.Num(salary)} + {Money.Num(average)}) × {twelfths}/12");
		}
		else
		{
			bd.Add("13º bruto", gross, "salário + média");
		}
		bd.Add("1ª parcela (NOV)", p1, "metade do salário de NOV");
		bd.Add("INSS do 13º", vInss, "tabela progressiva sobre o 13º bruto");
		bd.Add("2ª parcela (DEZ)", p2, "bruto − 1ª parcela − INSS");
		bd.Add("= Total líquido", p1 + p2);
		bd.Text("IRRF do 13º não calculado (tributação exclusiva; a planilha também não calculava).");
		return new ThirteenthResult(year, salary, average, twelfths, gross, p1, vInss, p2, p1 + p2, bd);
	}

	public static int Twelfths(DateOnly hireDate, DateOnly end, int year)
	{
		int n = 0;
		for (int m = 1; m <= 12; m++)
		{
			DateOnly a = new DateOnly(year, m, 1);
			DateOnly b = a.AddMonths(1).AddDays(-1);
			if (hireDate > a)
			{
				a = hireDate;
			}
			if (end < b)
			{
				b = end;
			}
			if (b >= a && b.DayNumber - a.DayNumber + 1 >= 15)
			{
				n++;
			}
		}
		return n;
	}
}
