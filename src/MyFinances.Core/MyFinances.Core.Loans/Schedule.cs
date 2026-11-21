using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;

namespace MyFinances.Core.Loans;

public static class Schedule
{
	public static DateOnly DueDate(DateOnly first, Frequency p, int index)
	{
		return p switch
		{
			Frequency.Monthly => first.AddMonths(index), 
			Frequency.Biweekly => first.AddDays(15 * index), 
			Frequency.Weekly => first.AddDays(7 * index), 
			Frequency.Daily => first.AddDays(index), 
			_ => throw new ArgumentOutOfRangeException("p"), 
		};
	}

	public static string PeriodName(Frequency p)
	{
		return p switch
		{
			Frequency.Monthly => "ao mês", 
			Frequency.Biweekly => "por quinzena", 
			Frequency.Weekly => "por semana", 
			Frequency.Daily => "ao dia", 
			_ => "", 
		};
	}

	public static string MethodName(AmortizationMethod m)
	{
		return m switch
		{
			AmortizationMethod.Sac => "SAC (amortização constante)", 
			AmortizationMethod.Price => "Price (parcela fixa)", 
			AmortizationMethod.SimpleInterest => "Juros simples (parcela fixa)", 
			AmortizationMethod.BulletPrincipal => "Só juros + principal no final", 
			_ => m.ToString(), 
		};
	}

	public static List<string> Validate(LoanTerms c)
	{
		List<string> e = new List<string>();
		if (c.Principal <= 0m)
		{
			e.Add("O capital emprestado precisa ser maior que zero.");
		}
		int installmentCount = c.InstallmentCount;
		if ((installmentCount < 1 || installmentCount > 600) ? true : false)
		{
			e.Add("Número de parcelas: de 1 a 600.");
		}
		if (c.InterestRatePct < 0m || c.InterestRatePct > 100m)
		{
			e.Add("Taxa de juros: de 0% a 100% por período.");
		}
		if (c.FirstDueDate < c.ContractDate)
		{
			e.Add("O primeiro vencimento não pode ser antes da contratação.");
		}
		if (c.GracePeriods < 0 || c.GracePeriods > 120)
		{
			e.Add("Carência: de 0 a 120 períodos.");
		}
		if (c.GracePeriods > 0 && c.GraceKind == GraceKind.None)
		{
			e.Add("Escolha como funciona a carência (paga juros ou sem pagamento).");
		}
		if (c.SetupFee < 0m)
		{
			e.Add("A tarifa não pode ser negativa.");
		}
		if (c.LateRules.PenaltyPct < 0m || c.LateRules.PenaltyPct > 100m)
		{
			e.Add("Multa: de 0% a 100%.");
		}
		if (c.LateRules.MonthlyLateInterestPct < 0m || c.LateRules.MonthlyLateInterestPct > 100m)
		{
			e.Add("Juros de atraso: de 0% a 100% ao mês.");
		}
		if (c.LateRules.ToleranceDays < 0)
		{
			e.Add("Tolerância não pode ser negativa.");
		}
		return e;
	}

	public static IReadOnlyList<ScheduledInstallment> Generate(LoanTerms c)
	{
		List<string> errors = Validate(c);
		if (errors.Count > 0)
		{
			throw new ArgumentException(string.Join(" ", errors));
		}
		decimal i = c.InterestRatePct / 100m;
		string rateText = Money.Pct(c.InterestRatePct, 4).Replace(",0000%", "%");
		List<ScheduledInstallment> list = new List<ScheduledInstallment>();
		decimal balance = c.Principal;
		int idx = 0;
		int num = 1;
		decimal graceInterest = 0m;
		decimal pendingFee = c.SetupFee;
		int k = 0;
		while (k < ((c.GraceKind != GraceKind.None) ? c.GracePeriods : 0))
		{
			decimal j = Money.R2(c.Principal * i);
			DateOnly due = DueDate(c.FirstDueDate, c.Frequency, idx);
			if (c.GraceKind == GraceKind.PaysInterest)
			{
				decimal fee = pendingFee;
				pendingFee = 0m;
				list.Add(new ScheduledInstallment(num++, due, 0m, j, fee, Grace: true, balance, $"Carência {k + 1}: só juros = ARRED({Money.Num(c.Principal)} × {rateText}) = {Money.Num(j)}" + ((fee > 0m) ? ("; tarifa " + Money.Num(fee)) : "")));
			}
			else
			{
				graceInterest += j;
			}
			k++;
			idx++;
		}
		int n = c.InstallmentCount;
		decimal sacAmort = Money.R2(c.Principal / (decimal)n);
		decimal pmtValue = ((c.Method == AmortizationMethod.Price) ? Price.Installment(c.Principal, i, n) : 0m);
		decimal simpleInterest = Money.R2(c.Principal * i);
		int k2 = 1;
		while (k2 <= n)
		{
			DateOnly due2 = DueDate(c.FirstDueDate, c.Frequency, idx);
			bool last = k2 == n;
			decimal interest;
			decimal amort;
			string bd;
			switch (c.Method)
			{
			case AmortizationMethod.Sac:
				interest = Money.R2(balance * i);
				amort = (last ? balance : sacAmort);
				bd = $"SAC: principal = {(last ? "saldo restante" : $"ARRED({Money.Num(c.Principal)} / {n})")} = {Money.Num(amort)}; juros = ARRED(saldo {Money.Num(balance)} × {rateText}) = {Money.Num(interest)}";
				break;
			case AmortizationMethod.Price:
				interest = Money.R2(balance * i);
				amort = (last ? balance : (pmtValue - interest));
				bd = $"Price: parcela {Money.Num(pmtValue)}; juros = ARRED(saldo {Money.Num(balance)} × {rateText}) = {Money.Num(interest)}; principal = {(last ? "saldo restante" : "parcela − juros")} = {Money.Num(amort)}";
				break;
			case AmortizationMethod.SimpleInterest:
				interest = simpleInterest;
				amort = (last ? balance : sacAmort);
				bd = $"Juros simples: juros = ARRED({Money.Num(c.Principal)} × {rateText}) = {Money.Num(interest)}; principal = {(last ? "saldo restante" : $"ARRED({Money.Num(c.Principal)} / {n})")} = {Money.Num(amort)}";
				break;
			default:
				interest = simpleInterest;
				amort = (last ? balance : 0m);
				bd = $"Só juros: juros = ARRED({Money.Num(c.Principal)} × {rateText}) = {Money.Num(interest)}" + (last ? ("; principal inteiro = " + Money.Num(amort)) : "");
				break;
			}
			if (k2 == 1 && graceInterest > 0m)
			{
				bd = bd + "; + juros da carência sem pagamento = " + Money.Num(graceInterest) + " (não viram principal)";
				interest += graceInterest;
			}
			decimal fee2 = pendingFee;
			pendingFee = 0m;
			if (fee2 > 0m)
			{
				bd = bd + "; tarifa de contratação = " + Money.Num(fee2);
			}
			balance -= amort;
			list.Add(new ScheduledInstallment(num++, due2, amort, interest, fee2, Grace: false, balance, bd + "; saldo após = " + Money.Num(balance)));
			k2++;
			idx++;
		}
		return list;
	}

	public static IReadOnlyList<ScheduledInstallment> Extend(IEnumerable<(DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee)> openOnes, Frequency p, int periods, decimal ratePct, bool chargeInterest)
	{
		List<(DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee)> list = (from tuple in openOnes
			where tuple.Principal + tuple.Interest + tuple.Fee > 0m
			orderby tuple.DueDate
			select tuple).ToList();
		decimal outstandingPrincipal = list.Sum(((DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee) tuple) => tuple.Principal);
		decimal extra = (chargeInterest ? Money.R2(outstandingPrincipal * ratePct / 100m * (decimal)periods) : 0m);
		List<ScheduledInstallment> res = new List<ScheduledInstallment>();
		decimal balance = outstandingPrincipal;
		int n = 1;
		foreach (var a in list)
		{
			DateOnly due = AdvancePeriods(a.DueDate, p, periods);
			decimal j = a.Interest + ((n == 1) ? extra : 0m);
			balance -= a.Principal;
			res.Add(new ScheduledInstallment(n, due, a.Principal, j, a.Fee, Grace: false, balance, $"Prorrogada {periods} período(s): vencia em {a.DueDate:dd/MM/yyyy}; principal {Money.Num(a.Principal)}; juros {Money.Num(a.Interest)}" + ((n == 1 && extra > 0m) ? $" + juros da prorrogação ARRED({Money.Num(outstandingPrincipal)} × {Money.Pct(ratePct)} × {periods}) = {Money.Num(extra)}" : "")));
			n++;
		}
		return res;
	}

	public static DateOnly AdvancePeriods(DateOnly d, Frequency p, int periods)
	{
		return DueDate(d, p, periods);
	}
}
