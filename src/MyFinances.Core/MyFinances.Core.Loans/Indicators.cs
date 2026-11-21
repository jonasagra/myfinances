using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyFinances.Core.Common;

namespace MyFinances.Core.Loans;

public static class Indicators
{
	public static LoanIndicators Calculate(IEnumerable<(LoanCalc Loan, DateOnly ContractDate)> loans, DateOnly start, DateOnly end, DateOnly today, decimal? capitalLimit = null)
	{
		DateOnly reference = ((end < today) ? end : today);
		int activeCount = 0;
		int late = 0;
		int risk = 0;
		int overdue = 0;
		decimal capital = 0m;
		decimal amort = 0m;
		decimal principalBal = 0m;
		decimal totalBal = 0m;
		decimal overdueAmt = 0m;
		decimal intExp = 0m;
		decimal intRcv = 0m;
		decimal chgRcv = 0m;
		decimal feeRcv = 0m;
		decimal recovered = 0m;
		decimal losses = 0m;
		decimal desc = 0m;
		decimal receivable = 0m;
		decimal estLoss = 0m;
		Dictionary<YearMonth, (decimal, decimal, decimal)> series = new Dictionary<YearMonth, (decimal, decimal, decimal)>();
		foreach (YearMonth c in YearMonth.Range(YearMonth.From(start), YearMonth.From(end)))
		{
			series[c] = (0m, 0m, 0m);
		}
		foreach (var loan2 in loans)
		{
			LoanCalc loan = loan2.Loan;
			DateOnly contractDate = loan2.ContractDate;
			LoanPosition pos = LoanEngine.Calculate(loan, reference);
			if (pos.Status == LoanStatus.Canceled)
			{
				continue;
			}
			if (Within(contractDate))
			{
				capital += pos.ContractedPrincipal;
			}
			LoanStatus status = pos.Status;
			bool flag = (uint)status <= 2u;
			bool active = flag;
			if (active)
			{
				activeCount++;
				if (pos.Status == LoanStatus.Late)
				{
					late++;
				}
				if (pos.Status == LoanStatus.AtRisk)
				{
					risk++;
				}
				principalBal += pos.OutstandingPrincipal;
				totalBal += pos.TotalOwed;
				overdue += pos.OverdueInstallments;
				overdueAmt += pos.OverdueOutstanding;
			}
			foreach (InstallmentState p in pos.Installments.Where((InstallmentState installmentState) => Within(installmentState.DueDate)))
			{
				decimal exp = (p.Superseded ? p.PaidInterest : p.Interest);
				intExp += exp;
				YearMonth k = YearMonth.From(p.DueDate);
				if (series.TryGetValue(k, out var s))
				{
					series[k] = (s.Item1 + exp, s.Item2, s.Item3);
				}
			}
			foreach (PaymentAllocation a in pos.Allocations)
			{
				DateOnly d = DateOnly.FromDateTime(a.Timestamp);
				if (Within(d))
				{
					decimal j = a.Total(Component.Interest);
					decimal chg = a.Total(Component.Penalty) + a.Total(Component.LateInterest);
					decimal t = a.Total(Component.Fee);
					decimal pr = a.Total(Component.Principal);
					intRcv += j;
					chgRcv += chg;
					feeRcv += t;
					amort += pr;
					if (a.AfterWriteOff)
					{
						recovered += pr;
					}
					YearMonth k2 = YearMonth.From(d);
					if (series.TryGetValue(k2, out var s2))
					{
						series[k2] = (s2.Item1, s2.Item2 + j + chg + t, s2.Item3 + pr);
					}
				}
			}
			DateOnly? writeOffDate = pos.WriteOffDate;
			if (writeOffDate.HasValue)
			{
				DateOnly db = writeOffDate.GetValueOrDefault();
				if (Within(db))
				{
					losses += pos.WrittenOffLoss;
				}
			}
			desc += pos.DiscountsGranted;
			if (pos.Status == LoanStatus.AtRisk)
			{
				LateRules rule = loan.Versions.OrderBy((VersionCalc v) => v.Start).Last().Terms.LateRules;
				estLoss += Money.R2(pos.OutstandingPrincipal * rule.LossProvisionPct / 100m);
			}
			else if (active)
			{
				receivable += pos.Installments.Where((InstallmentState installmentState) => !installmentState.Superseded && installmentState.DueDate <= end).Sum((InstallmentState installmentState) => installmentState.OutstandingInterest + installmentState.OutstandingFee);
			}
		}
		decimal realized = intRcv + chgRcv + feeRcv + recovered - losses;
		decimal projected = realized + receivable - estLoss;
		string formulas = new StringBuilder().AppendLine("Lucro realizado = juros + multa + mora + tarifas recebidos no período + principal recuperado após baixa − principal baixado como perda no período.").AppendLine("Amortização de principal não é lucro: é o seu dinheiro voltando.").AppendLine("Juros a receber = juros e tarifas em aberto (vencidos ou a vencer até o fim do período) dos contratos ativos que não estão em risco.")
			.AppendLine("Perda estimada = principal em aberto dos contratos em risco (atraso acima do limite do contrato) × % de provisão do contrato.")
			.AppendLine("Lucro projetado = lucro realizado + juros a receber − perda estimada.")
			.Append("Previsto = o que o cronograma manda pagar; Recebido = pagamentos registrados; Vencido = previsto não pago após o vencimento; Baixado = perda decidida por você.")
			.ToString();
		return new LoanIndicators(start, end, reference, activeCount, late, risk, capital, amort, principalBal, totalBal, overdue, overdueAmt, intExp, intRcv, chgRcv, feeRcv, recovered, losses, desc, realized, receivable, estLoss, projected, capitalLimit, (!capitalLimit.HasValue) ? ((decimal?)null) : (capitalLimit - (decimal?)principalBal), series.OrderBy((KeyValuePair<YearMonth, (decimal Exp, decimal Rec, decimal Princ)> keyValuePair) => keyValuePair.Key).Select((KeyValuePair<YearMonth, (decimal Exp, decimal Rec, decimal Princ)> keyValuePair) => new LoanMonthlySeries(keyValuePair.Key, keyValuePair.Value.Exp, keyValuePair.Value.Rec, keyValuePair.Value.Princ)).ToList(), formulas);
		bool Within(DateOnly dateOnly)
		{
			if (dateOnly >= start)
			{
				return dateOnly <= end;
			}
			return false;
		}
	}
}
