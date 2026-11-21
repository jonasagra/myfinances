using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Core.Finance;

public static class FinanceSummary
{
	public static decimal RealBalanceDifference(decimal remaining, decimal realBalance, decimal pendingInflow, decimal pendingOutflow)
	{
		return Money.R2(remaining - pendingInflow + pendingOutflow - realBalance);
	}

	public static FinanceSummaryResult Calculate(IReadOnlyList<MonthCalc> months, decimal openingBalance, PayrollConfig cfg, Func<int, LegalParameters> parametersForYear, bool include13th = true)
	{
		List<MonthCalc> order = months.OrderBy((MonthCalc monthCalc2) => monthCalc2.Month).ToList();
		List<int> years = order.Select((MonthCalc monthCalc2) => monthCalc2.Month.Year).Distinct().ToList();
		List<ThirteenthResult> thirteenthCount = new List<ThirteenthResult>();
		if (include13th)
		{
			foreach (int year in years)
			{
				List<MonthCalc> ofYear = order.Where((MonthCalc monthCalc2) => monthCalc2.Month.Year == year).ToList();
				MonthCalc dec = ofYear.FirstOrDefault((MonthCalc monthCalc2) => monthCalc2.Month.Month == 12);
				MonthCalc? monthCalc = ofYear.FirstOrDefault((MonthCalc monthCalc2) => monthCalc2.Month.Month == 11);
				decimal decSalary = dec?.BaseSalary ?? 0m;
				decimal novSalary = monthCalc?.BaseSalary ?? decSalary;
				if (decSalary == 0m && novSalary == 0m)
				{
					continue;
				}
				decimal earnSum = (from l in ofYear.SelectMany((MonthCalc monthCalc2) => monthCalc2.Entries)
					where l.Kind == EntryKind.Earning && Payroll.CountsInAverage(l.Description)
					select l).Sum((EntryCalc l) => l.Amount);
				int twelfths = 12;
				DateOnly? hireDate = cfg.HireDate;
				if (hireDate.HasValue)
				{
					DateOnly hire = hireDate.GetValueOrDefault();
					if (hire.Year == year)
					{
						twelfths = ThirteenthSalary.Twelfths(hire, new DateOnly(year, 12, 31), year);
					}
				}
				hireDate = cfg.HireDate;
				if (!hireDate.HasValue || hireDate.GetValueOrDefault().Year <= year)
				{
					thirteenthCount.Add(ThirteenthSalary.Calculate(year, decSalary, novSalary, earnSum, twelfths, parametersForYear(year).Inss));
				}
			}
		}
		List<MonthSummary> list = new List<MonthSummary>();
		decimal remWith = openingBalance;
		decimal remWithout = openingBalance;
		foreach (MonthCalc m in order)
		{
			LegalParameters p = parametersForYear(m.Month.Year);
			decimal earn = m.Entries.Where((EntryCalc l) => l.Kind == EntryKind.Earning).Sum((EntryCalc l) => l.Amount);
			decimal desc = m.Entries.Where((EntryCalc l) => l.Kind == EntryKind.Deduction).Sum((EntryCalc l) => l.Amount);
			decimal inflows = m.Entries.Where((EntryCalc l) => l.Kind == EntryKind.Inflow).Sum((EntryCalc l) => l.Amount);
			List<EntryCalc> source = m.Entries.Where((EntryCalc l) => l.Kind == EntryKind.Expense).ToList();
			decimal exp = source.Sum((EntryCalc l) => l.Amount);
			PayrollResult payroll = Payroll.Calculate(m.BaseSalary, earn, desc, m.Absences, cfg, p);
			decimal dec2 = 0m;
			ThirteenthResult d = thirteenthCount.FirstOrDefault((ThirteenthResult x) => x.Year == m.Month.Year);
			if ((object)d != null)
			{
				if (m.Month.Month == 11)
				{
					dec2 = d.FirstInstallment;
				}
				else if (m.Month.Month == 12)
				{
					dec2 = d.SecondInstallment;
				}
			}
			decimal balanceWithout = payroll.Net + inflows - exp;
			decimal balanceWith = balanceWithout + dec2;
			remWith += balanceWith;
			remWithout += balanceWithout;
			Dictionary<string, decimal> byCat = (from l in source
				group l by (!string.IsNullOrWhiteSpace(l.Category)) ? l.Category : "Sem categoria").ToDictionary((IGrouping<string, EntryCalc> g) => g.Key, (IGrouping<string, EntryCalc> g) => g.Sum((EntryCalc x) => x.Amount));
			list.Add(new MonthSummary(m.Month, payroll, inflows, dec2, exp, balanceWith, balanceWithout, remWith, remWithout, byCat));
		}
		return new FinanceSummaryResult(list, thirteenthCount, openingBalance);
	}

	public static decimal CurrentSalary(IReadOnlyDictionary<YearMonth, decimal> defined, YearMonth month)
	{
		decimal v = 0m;
		YearMonth? best = null;
		foreach (KeyValuePair<YearMonth, decimal> item in defined)
		{
			item.Deconstruct(out var key, out var value);
			YearMonth c = key;
			decimal s = value;
			if (!(c <= month))
			{
				continue;
			}
			if (best.HasValue)
			{
				key = c;
				YearMonth? yearMonth = best;
				if (!(key > yearMonth))
				{
					continue;
				}
			}
			best = c;
			v = s;
		}
		return v;
	}
}
