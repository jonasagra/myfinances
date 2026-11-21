using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Salary
{
	public static RetroactiveResult Retroactive(YearMonth paymentMonth, decimal oldSalary, decimal newSalary, int monthsLate, decimal otherEarningsOfMonth, decimal monthAbsences, InssTable t)
	{
		if ((monthsLate < 1 || monthsLate > 24) ? true : false)
		{
			throw new ArgumentException("Meses em atraso: de 1 a 24.");
		}
		if (newSalary <= oldSalary)
		{
			throw new ArgumentException("Retroativo só quando o salário novo é maior que o atual (" + Money.Fmt(oldSalary) + ").");
		}
		decimal diff = Money.R2(newSalary - oldSalary);
		bool hasDec = Enumerable.Range(1, monthsLate).Any((int k) => paymentMonth.AddMonths(-k).Month == 12);
		decimal vRetro = Money.R2(diff * (decimal)monthsLate);
		decimal v13 = (hasDec ? diff : 0m);
		decimal corr = (decimal)monthsLate * (Inss.Calculate(newSalary, t) - Inss.Calculate(oldSalary, t));
		if (hasDec)
		{
			corr += Inss.Calculate(newSalary, t) - Inss.Calculate(oldSalary, t);
		}
		decimal monthBase = newSalary + otherEarningsOfMonth - monthAbsences;
		decimal extra = Inss.Calculate(monthBase + vRetro + v13, t) - Inss.Calculate(monthBase, t);
		decimal adj = Money.R2(corr - extra);
		List<PlanItem> plan = new List<PlanItem>
		{
			new PlanItem(paymentMonth, EntryKind.Earning, $"Retroativo: diferença salarial ({monthsLate} {((monthsLate == 1) ? "mês" : "meses")})", vRetro)
		};
		if (v13 > 0m)
		{
			plan.Add(new PlanItem(paymentMonth, EntryKind.Earning, "Retroativo: diferença de 13º", v13));
		}
		if (adj != 0m)
		{
			plan.Add(new PlanItem(paymentMonth, EntryKind.Deduction, "Retroativo: ajuste do INSS (mês a mês)", adj));
		}
		CalcBreakdown bd = new CalcBreakdown().Add("Diferença mensal", diff, Money.Num(newSalary) + " − " + Money.Num(oldSalary)).Add("Retroativo", vRetro, $"{Money.Num(diff)} × {monthsLate}").Add("Diferença de 13º", v13, hasDec ? "o atraso passou por dezembro" : "sem dezembro no atraso")
			.Add("INSS correto (mês a mês)", corr)
			.Add("INSS que o mês já calcularia", extra)
			.Add("= Ajuste do INSS", adj, "correto − já calculado")
			.Text("Se teve férias no período, a diferença delas vem à parte (lance manualmente).");
		return new RetroactiveResult(diff, vRetro, v13, adj, plan, bd);
	}
}
