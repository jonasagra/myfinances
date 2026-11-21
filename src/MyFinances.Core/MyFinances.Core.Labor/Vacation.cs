using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Vacation
{
	public static VacationResult Simulate(VacationInput e, LegalParameters p, Calendar cal)
	{
		int days = e.Days;
		if ((days < 1 || days > 30) ? true : false)
		{
			throw new ArgumentException("Dias de férias: número de 1 a 30.");
		}
		if (e.Salary <= 0m)
		{
			throw new ArgumentException("Informe o salário base.");
		}
		DateOnly start = e.Start;
		int d1 = Math.Min(DateTime.DaysInMonth(start.Year, start.Month) - start.Day + 1, e.Days);
		int d2 = e.Days - d1;
		DateOnly end = start.AddDays(e.Days - 1);
		DateOnly pay = start.AddDays(-2);
		YearMonth payMonth = YearMonth.From(pay);
		YearMonth startMonth = YearMonth.From(start);
		YearMonth sheet1 = startMonth.Next;
		YearMonth sheet2 = startMonth.AddMonths(2);
		decimal vacation = Money.R2(e.Salary * (decimal)e.Days / 30m);
		decimal avg = Money.R2(e.OvertimeAverage * (decimal)e.Days / 30m);
		decimal third = Money.R2((vacation + avg) / 3m);
		decimal gross = vacation + avg + third;
		decimal base1 = gross * (decimal)d1 / (decimal)e.Days;
		decimal base2 = gross - base1;
		decimal inss1 = Inss.Calculate(base1, p.Inss);
		decimal inss2 = ((d2 > 0) ? Inss.Calculate(base2, p.Inss) : 0m);
		decimal c1 = Money.R2(e.MonthlyPayrollLoan * (decimal)d1 / 30m);
		decimal c2 = Money.R2(e.MonthlyPayrollLoan * (decimal)d2 / 30m);
		decimal net = Money.R2(gross - inss1 - inss2 - c1 - c2);
		decimal tv1 = Money.R2(e.TransportVoucherMonth1 * (decimal)d1 / 30m);
		decimal adj1 = InssAdjustment(e.Salary, d1, base1, inss1, p.Inss);
		decimal tv2 = 0m;
		decimal adj2 = 0m;
		if (d2 > 0)
		{
			tv2 = Money.R2(e.TransportVoucherMonth2 * (decimal)d2 / 30m);
			adj2 = InssAdjustment(e.Salary, d2, base2, inss2, p.Inss);
		}
		decimal g1 = 0m;
		decimal g2 = 0m;
		if (e.ReserveSalary && gross > 0m)
		{
			g1 = Money.R2(net * (Money.R2(e.Salary * (decimal)d1 / 30m) + Money.R2(e.OvertimeAverage * (decimal)d1 / 30m)) / gross);
			if (d2 > 0)
			{
				g2 = Money.R2(net * (Money.R2(e.Salary * (decimal)d2 / 30m) + Money.R2(e.OvertimeAverage * (decimal)d2 / 30m)) / gross);
			}
		}
		string notice = null;
		for (int i = 1; i <= 2; i++)
		{
			if (cal.IsHoliday(start.AddDays(i)))
			{
				notice = $"Atenção: pela CLT (art. 134, §3º) as férias não podem começar nos 2 dias antes de feriado ({start.AddDays(i):dd/MM}) ou do repouso semanal. A empresa teria que marcar outra data.";
				break;
			}
		}
		CalcBreakdown bd = new CalcBreakdown().Text($"Período: {start:dd/MM/yyyy} a {end:dd/MM/yyyy} ({d1} dias em {startMonth.SheetName.Substring(0, 3)}" + ((d2 > 0) ? $", {d2} no mês seguinte)" : ")")).Text($"Pagamento: {pay:dd/MM/yyyy} (mês {payMonth})").Add("Férias", vacation, $"ARRED({Money.Num(e.Salary)} × {e.Days}/30)")
			.Add("Média de extras", avg, $"ARRED({Money.Num(e.OvertimeAverage)} × {e.Days}/30)")
			.Add("1/3 de férias", third, "ARRED((férias + média)/3)")
			.Add("= Proventos", gross)
			.Add($"- INSS ({d1} dias)", inss1, $"tabela sobre {Money.Num(Money.R2(base1))} (proventos × {d1}/{e.Days})");
		if (d2 > 0)
		{
			bd.Add($"- INSS ({d2} dias)", inss2, "tabela sobre " + Money.Num(Money.R2(base2)));
		}
		bd.Add($"- Consignado ({d1} dias)", c1, $"ARRED({Money.Num(e.MonthlyPayrollLoan)} × {d1}/30)");
		if (d2 > 0)
		{
			bd.Add($"- Consignado ({d2} dias)", c2);
		}
		bd.Add("= Líquido das férias", net);
		bd.Text("Nas folhas seguintes: salário sem os dias de férias, consignado e VT desses dias abatidos e o ajuste do INSS do mês.");
		if (g1 + g2 > 0m)
		{
			bd.Add($"Fica em {payMonth} (o 1/3)", net - g1 - g2);
			bd.Add($"Guardado para {sheet1}", g1);
			if (g2 > 0m)
			{
				bd.Add($"Guardado para {sheet2}", g2);
			}
		}
		bd.Text("Premissas: sem abono pecuniário (venda de 10 dias) e sem IRRF.");
		string key = start.ToString("dd/MM/yyyy");
		string pre = "Férias " + key;
		List<PlanItem> plan = new List<PlanItem>
		{
			new PlanItem(payMonth, EntryKind.Inflow, $"{pre} a {end:dd/MM/yyyy} (líquido)", net)
		};
		AddPayroll(plan, sheet1, pre, d1, e.Salary, c1, tv1, adj1);
		if (d2 > 0)
		{
			AddPayroll(plan, sheet2, pre, d2, e.Salary, c2, tv2, adj2);
		}
		if (g1 + g2 > 0m)
		{
			plan.Add(new PlanItem(payMonth, EntryKind.Inflow, pre + ": guardado para os meses seguintes", -(g1 + g2)));
			plan.Add(new PlanItem(sheet1, EntryKind.Inflow, pre + ": salário guardado das férias", g1));
			if (g2 > 0m)
			{
				plan.Add(new PlanItem(sheet2, EntryKind.Inflow, pre + ": salário guardado das férias", g2));
			}
		}
		return new VacationResult(start, end, pay, d1, d2, vacation, avg, third, gross, inss1, inss2, c1, c2, net, g1, g2, notice, plan, bd, key);
	}

	private static void AddPayroll(List<PlanItem> p, YearMonth sheet, string pre, int d, decimal salary, decimal payrollLoan, decimal tv, decimal adj)
	{
		p.Add(new PlanItem(sheet, EntryKind.Earning, $"{pre}: {d} dias já pagos", -Money.R2(salary * (decimal)d / 30m)));
		if (payrollLoan > 0m)
		{
			p.Add(new PlanItem(sheet, EntryKind.Deduction, pre + ": consignado já descontado", -payrollLoan));
		}
		if (tv > 0m)
		{
			p.Add(new PlanItem(sheet, EntryKind.Deduction, pre + ": VT dos dias de férias", -tv));
		}
		if (adj != 0m)
		{
			p.Add(new PlanItem(sheet, EntryKind.Deduction, pre + ": ajuste do INSS", adj));
		}
	}

	public static decimal InssAdjustment(decimal salary, int d, decimal vacationBase, decimal vacationInss, InssTable t)
	{
		decimal rest = salary - Money.R2(salary * (decimal)d / 30m);
		return Money.R2(Inss.Calculate(rest + vacationBase, t) - vacationInss - Inss.Calculate(rest, t));
	}

	public static string? KeyOf(string description)
	{
		string n = description.Trim();
		if (n.Length >= 17 && n.StartsWith("Férias ", StringComparison.OrdinalIgnoreCase) && DateOnly.TryParseExact(n.Substring(7, 10), "dd/MM/yyyy", out var _))
		{
			return n.Substring(7, 10);
		}
		return null;
	}
}
