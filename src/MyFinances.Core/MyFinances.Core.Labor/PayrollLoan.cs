using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class PayrollLoan
{
	public static PayrollLoanResult Simulate(PayrollLoanInput e, LegalParameters p)
	{
		if (e.ReleasedAmount <= 0m)
		{
			throw new ArgumentException("Informe o valor liberado.");
		}
		int installments = e.Installments;
		if ((installments < 1 || installments > 120) ? true : false)
		{
			throw new ArgumentException("Parcelas: de 1 a 120.");
		}
		if (e.MonthlyRatePct < 0m)
		{
			throw new ArgumentException("Taxa de juros não pode ser negativa.");
		}
		int days = e.FirstDueDate.DayNumber - e.ContractDate.DayNumber;
		if (days <= 0)
		{
			throw new ArgumentException("A 1ª parcela tem que vencer depois da data da contratação.");
		}
		if (days > 365)
		{
			throw new ArgumentException("A 1ª parcela está a mais de um ano da contratação; confira as datas.");
		}
		decimal financed = e.ReleasedAmount + e.IofAmount + e.FixedFees;
		decimal i = e.MonthlyRatePct / 100m;
		decimal factor = ((i == 0m) ? 1m : ((decimal)Math.Pow((double)(1m + i), (double)(days - 30) / 30.0)));
		decimal openingBalance = Money.R2(financed * factor);
		decimal termInterest = openingBalance - financed;
		decimal pmtValue = Price.Installment(openingBalance, i, e.Installments);
		List<(int, decimal, decimal, decimal)> table = new List<(int, decimal, decimal, decimal)>();
		decimal balance = openingBalance;
		decimal totInterest = termInterest;
		for (int k = 1; k <= e.Installments; k++)
		{
			decimal j = Money.R2(balance * i);
			decimal am = ((k == e.Installments) ? balance : (pmtValue - j));
			balance -= am;
			totInterest += j;
			table.Add((k, j, am, balance));
		}
		decimal last = table[table.Count - 1].Item2 + table[table.Count - 1].Item3;
		decimal total = pmtValue * (decimal)(e.Installments - 1) + last;
		bool useContract = e.ContractInstallment > 0m;
		decimal postedInstallment = (useContract ? e.ContractInstallment : pmtValue);
		decimal cet = Price.ImplicitRate(e.ReleasedAmount, postedInstallment, e.Installments);
		decimal cetYear = (Money.Pow(1m + cet, 12) - 1m) * 100m;
		decimal margin = Money.R2(Math.Max(0m, e.GrossPay - e.MandatoryDeductions) * p.PayrollLoanMarginPct / 100m);
		decimal available = margin - e.CurrentPayrollLoanInstallments;
		CalcBreakdown bd = new CalcBreakdown().Add("Valor liberado", e.ReleasedAmount).Add("IOF (do contrato)", e.IofAmount).Add("Tarifas", e.FixedFees)
			.Add("= Valor financiado", financed)
			.Text($"Dias da contratação ({e.ContractDate:dd/MM/yyyy}) até a 1ª parcela ({e.FirstDueDate:dd/MM/yyyy}): {days}")
			.Add("Juros dos dias além de 30", termInterest, $"financiado × (1 + {Money.Pct(e.MonthlyRatePct)})^(({days} − 30)/30) − financiado")
			.Add("= Saldo para calcular a parcela", openingBalance)
			.Add("Parcela calculada (Price)", pmtValue, $"PV × i / (1 − (1+i)^−n), i = {Money.Pct(e.MonthlyRatePct)}, n = {e.Installments} — estimativa");
		if (useContract)
		{
			bd.Add("Parcela do contrato (usada nos lançamentos)", e.ContractInstallment, "diferença para a calculada: " + Money.Num(e.ContractInstallment - pmtValue));
		}
		bd.Add("Total pago", useContract ? (e.ContractInstallment * (decimal)e.Installments) : total).Add("Total de juros (calculado)", totInterest).Text($"CET aproximado: {Money.Pct(cet * 100m)} a.m. / {Money.Pct(cetYear)} a.a. (sobre o valor liberado, sem contar os dias)")
			.Add("Margem (35% da remuneração disponível)", margin, $"({Money.Num(e.GrossPay)} − {Money.Num(e.MandatoryDeductions)}) × {Money.Pct(p.PayrollLoanMarginPct, 0)} — estimativa")
			.Add("(-) Parcelas consignadas atuais", e.CurrentPayrollLoanInstallments)
			.Add("= Margem disponível", available)
			.Text((postedInstallment <= available) ? "A parcela cabe na margem estimada." : "ATENÇÃO: a parcela passa da margem estimada.");
		YearMonth creditMonth = YearMonth.From(e.ContractDate);
		YearMonth firstMonth = YearMonth.From(e.FirstDueDate);
		List<PlanItem> plan = new List<PlanItem>
		{
			new PlanItem(creditMonth, EntryKind.Inflow, e.Description + " (crédito)", e.ReleasedAmount)
		};
		for (int l = 1; l <= e.Installments; l++)
		{
			List<PlanItem> list = plan;
			YearMonth month = firstMonth.AddMonths(l - 1);
			string description = $"{e.Description} {l}/{e.Installments}";
			decimal amount;
			if (useContract)
			{
				amount = e.ContractInstallment;
			}
			else
			{
				amount = ((l == e.Installments) ? last : pmtValue);
			}
			list.Add(new PlanItem(month, EntryKind.Deduction, description, amount));
		}
		return new PayrollLoanResult(postedInstallment, useContract ? (e.ContractInstallment * (decimal)e.Installments) : total, totInterest, financed, cet * 100m, cetYear, margin, available, postedInstallment <= available, table, plan, bd);
	}
}
