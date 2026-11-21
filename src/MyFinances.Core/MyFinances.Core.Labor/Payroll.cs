using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Payroll
{
	public static decimal AbsenceDeduction(decimal salary, MonthAbsences f, int workload)
	{
		return Money.R2(f.AbsenceDays * salary / 30m) + Money.R2(f.LostDsr * salary / 30m) + Money.R2(f.LateHours * salary / (decimal)workload);
	}

	public static PayrollResult Calculate(decimal salary, decimal earnings, decimal manualDeductions, MonthAbsences absences, PayrollConfig cfg, LegalParameters p)
	{
		CalcBreakdown bd = new CalcBreakdown();
		decimal gross = salary + earnings;
		bd.Add("Salário base", salary);
		bd.Add("(+) Proventos", earnings);
		bd.Add("= Bruto", gross, "salário + proventos");
		decimal vAbsences = ((salary > 0m) ? AbsenceDeduction(salary, absences, cfg.MonthlyHours) : 0m);
		if (vAbsences != 0m)
		{
			bd.Add("Faltas e atrasos", vAbsences, $"ARRED({absences.AbsenceDays}×{Money.Num(salary)}/30) + ARRED({absences.LostDsr}×{Money.Num(salary)}/30) + ARRED({absences.LateHours:0.####}×{Money.Num(salary)}/{cfg.MonthlyHours})");
		}
		decimal inssBase = gross - vAbsences;
		var (inss, inssBd) = Inss.CalculateWithBreakdown(inssBase, p.Inss);
		bd.Add("Base do INSS", inssBase, "bruto − faltas");
		bd.Join(inssBd, "  ");
		decimal tv = ((cfg.TransportVoucherActive && salary > 0m) ? Money.R2(salary * cfg.TransportVoucherPct / 100m) : 0m);
		if (tv != 0m)
		{
			bd.Add("Vale-transporte", tv, Money.Num(salary) + " × " + Money.Pct(cfg.TransportVoucherPct, 0));
		}
		decimal irrf = 0m;
		if (cfg.IrrfActive && inssBase > 0m)
		{
			(decimal Amount, CalcBreakdown Breakdown) tuple2 = Irrf.Calculate(inssBase, inss, cfg.Dependents, 0m, p.Irrf);
			decimal v = tuple2.Amount;
			CalcBreakdown m = tuple2.Breakdown;
			irrf = v;
			bd.Text("IRRF (tabela mensal + redução 2026):");
			bd.Join(m, "  ");
		}
		bd.Add("(-) Descontos lançados", manualDeductions);
		decimal totalDeductions = inss + tv + vAbsences + irrf + manualDeductions;
		decimal net = gross - totalDeductions;
		bd.Add("= Total de descontos", totalDeductions, "INSS + VT + faltas + IRRF + lançados");
		bd.Add("= Líquido a receber", net, "bruto − descontos");
		return new PayrollResult(salary, earnings, gross, vAbsences, inss, tv, irrf, manualDeductions, totalDeductions, net, bd);
	}

	public static decimal OvertimeAverage12Months(IEnumerable<(YearMonth Month, string Description, decimal Amount)> earnings, YearMonth until)
	{
		List<YearMonth> months = (from m in (from x in earnings
				select x.Month into m
				where m <= until
				select m).Distinct()
			orderby m
			select m).ToList();
		HashSet<YearMonth> lastOnes = months.Skip(Math.Max(0, months.Count - 12)).ToHashSet();
		return Money.R2(earnings.Where(((YearMonth Month, string Description, decimal Amount) x) => lastOnes.Contains(x.Month) && CountsInAverage(x.Description)).Sum(((YearMonth Month, string Description, decimal Amount) x) => x.Amount) / 12m);
	}

	public static bool CountsInAverage(string description)
	{
		string d = description.Trim();
		if (!d.StartsWith("Férias", StringComparison.OrdinalIgnoreCase))
		{
			return !d.StartsWith("Retroativo", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
