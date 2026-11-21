using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Termination
{
	public static TerminationResult Simulate(TerminationInput e, LegalParameters p)
	{
		if (e.LastDay < e.HireDate)
		{
			throw new ArgumentException("O último dia não pode ser antes da admissão.");
		}
		if (e.Salary <= 0m)
		{
			throw new ArgumentException("Informe o salário base.");
		}
		if (e.DueVacationDays < 0)
		{
			throw new ArgumentException("Férias vencidas: número de dias (30 por período).");
		}
		List<string> warnings = new List<string>();
		DateOnly outgoing = e.LastDay;
		DateOnly hire = e.HireDate;
		bool withoutCause = e.Kind == TerminationKind.WithoutCause;
		bool agreement = e.Kind == TerminationKind.Agreement;
		bool forCause = e.Kind == TerminationKind.ForCause;
		bool resignation = e.Kind == TerminationKind.Resignation;
		bool hasEmployerNotice = withoutCause | agreement;
		int years = FullYears(hire, outgoing);
		int noticeDays = 0;
		int paidNotice = 0;
		if (hasEmployerNotice)
		{
			noticeDays = Math.Min(30 + 3 * years, 90);
			paidNotice = ((e.Notice == NoticeKind.Indemnified) ? noticeDays : (noticeDays - 30));
		}
		DateOnly end = outgoing.AddDays(paidNotice);
		int y0 = outgoing.Year;
		decimal payBase = e.Salary + e.OvertimeAverage;
		int balanceDays = ((outgoing.AddDays(1).Day == 1) ? 30 : Math.Min(outgoing.Day, 30));
		decimal vBalance = Money.R2(e.Salary / 30m * (decimal)balanceDays);
		decimal inssBalance = Inss.Calculate(vBalance, p.Inss);
		CalcBreakdown a = new CalcBreakdown().Text("VERBAS (" + Name(e.Kind).ToUpperInvariant() + ")");
		a.Add($"Saldo de salário ({balanceDays} dias)", vBalance, $"{Money.Num(e.Salary)} / 30 × {balanceDays}");
		decimal vNotice = 0m;
		decimal v13 = 0m;
		decimal inss13 = 0m;
		decimal vPv = 0m;
		decimal tPv = 0m;
		if (hasEmployerNotice && paidNotice > 0)
		{
			vNotice = Money.R2(payBase / 30m * (decimal)paidNotice);
			if (agreement)
			{
				vNotice = Money.R2(vNotice / 2m);
			}
			a.Add($"Aviso indenizado ({paidNotice} dias{(agreement ? ", metade no acordo" : "")})", vNotice, $"({Money.Num(e.Salary)} + {Money.Num(e.OvertimeAverage)}) / 30 × {paidNotice}" + (agreement ? " ÷ 2" : ""));
		}
		if (!forCause)
		{
			for (int i = y0; i <= end.Year; i++)
			{
				int twelfths = ThirteenthSalary.Twelfths(hire, end, i);
				decimal v13y = Money.R2(payBase * (decimal)twelfths / 12m);
				decimal paid = 0m;
				if (i == y0)
				{
					paid = e.Thirteenth switch
					{
						ThirteenthAlreadyReceived.FirstInstallment => e.FirstInstallmentPaid, 
						ThirteenthAlreadyReceived.All => v13y, 
						_ => 0m, 
					};
					if (paid > v13y)
					{
						paid = v13y;
					}
					inss13 += ((e.Thirteenth == ThirteenthAlreadyReceived.All) ? 0m : Inss.Calculate(v13y, p.Inss));
				}
				else
				{
					inss13 += Inss.Calculate(v13y, p.Inss);
				}
				if (i == y0 && e.Thirteenth == ThirteenthAlreadyReceived.All)
				{
					a.Text($"13º {i}: já pago");
					continue;
				}
				a.Add($"13º {i} ({twelfths}/12)", v13y, $"({Money.Num(payBase)}) × {twelfths}/12");
				v13 += v13y;
				if (i == y0 && paid > 0m)
				{
					a.Add("  (-) 1ª parcela já paga", -paid);
					v13 -= paid;
				}
			}
			int vacTwelfths = VacationTwelfths(hire, end);
			vPv = Money.R2(payBase * (decimal)vacTwelfths / 12m);
			tPv = Money.R2(vPv / 3m);
			a.Add($"Férias proporc. ({vacTwelfths}/12)", vPv, $"{Money.Num(payBase)} × {vacTwelfths}/12");
			a.Add("1/3 das férias proporc.", tPv);
		}
		int complete = FullYears(hire, end.AddDays(1));
		int pend = (e.DueVacationDays + 29) / 30;
		decimal vDv = 0m;
		bool anyDouble = false;
		for (int j = 1; j <= pend; j++)
		{
			int chunk = ((j == pend && e.DueVacationDays % 30 != 0) ? (e.DueVacationDays % 30) : 30);
			int freq = complete - pend + j;
			bool doubled = freq >= 1 && outgoing > Birthday(hire, freq + 1).AddDays(-1);
			decimal v14 = Money.R2(payBase / 30m * (decimal)chunk) * (decimal)((!doubled) ? 1 : 2);
			anyDouble |= doubled;
			a.Add(doubled ? $"Férias vencidas EM DOBRO ({chunk} dias)" : $"Férias vencidas ({chunk} dias)", v14, $"{Money.Num(payBase)} / 30 × {chunk}" + (doubled ? " × 2 (CLT art. 137)" : ""));
			vDv += v14;
		}
		decimal tDv = Money.R2(vDv / 3m);
		if (pend > 0)
		{
			a.Add("1/3 das férias vencidas", tDv);
		}
		if (e.DueVacationDays > 30 && e.DueVacationDays % 30 != 0)
		{
			warnings.Add($"Férias vencidas: cada período vale até 30 dias; {e.DueVacationDays} dias não fecha com períodos de 30.");
		}
		else if (pend > 0 && pend > complete)
		{
			warnings.Add($"Férias vencidas: só {complete} período(s) aquisitivo(s) completo(s) até o fim do contrato. Confira os dias.");
		}
		else if (anyDouble)
		{
			warnings.Add("Férias em dobro: um período passou do prazo da empresa. Se você já tirou essas férias, diminua os dias.");
		}
		decimal gross = vBalance + vNotice + v13 + vPv + tPv + vDv + tDv;
		decimal net = gross - inssBalance - inss13;
		decimal noticeDeduction = 0m;
		if (resignation && !e.WorksNoticeOnResignation)
		{
			noticeDeduction = Math.Min(Money.R2(payBase), Math.Max(0m, net));
			net -= noticeDeduction;
		}
		decimal ret = Math.Min(Money.R2(Math.Max(0m, net) * p.PayrollLoanMaxSeverancePct / 100m), e.PayrollLoanBalance);
		decimal rest = e.PayrollLoanBalance - ret;
		a.Add("= Total bruto", gross);
		a.Add("- INSS (saldo de salário)", inssBalance);
		if (inss13 > 0m)
		{
			a.Add("- INSS (13º)", inss13);
		}
		if (noticeDeduction > 0m)
		{
			a.Add("- Aviso não cumprido (CLT art. 487, §2º)", noticeDeduction);
		}
		if (ret > 0m)
		{
			a.Add("- Consignado (até " + Money.Pct(p.PayrollLoanMaxSeverancePct, 0) + ")", ret);
		}
		a.Add("= LÍQUIDO DA RESCISÃO", net - ret);
		a.Text($"Pago até {outgoing.AddDays(10):dd/MM/yyyy} (10 dias após o término — CLT art. 477, §6º).");
		a.Text("IRRF não calculado (premissa da planilha: verbas abaixo da isenção mensal).");
		int months = FullMonths(hire, outgoing);
		CalcBreakdown b = new CalcBreakdown().Text("TEMPO E AVISO").Text($"Tempo de casa: {years} ano(s) e {months - years * 12} mês(es)");
		if (hasEmployerNotice)
		{
			b.Text($"Aviso prévio: {noticeDays} dias (30 + 3 × {years} anos, até 90)").Text($"Contrato vai até (projeção): {end:dd/MM/yyyy}");
		}
		decimal dep = Money.R2((vBalance + vNotice + v13) * p.FgtsPct / 100m);
		b.Text("FGTS").Add("Saldo informado", e.FgtsBalance).Add("+ Depósito da rescisão (" + Money.Pct(p.FgtsPct, 0) + ")", dep, "sobre saldo + aviso + 13º");
		decimal penalty = 0m;
		decimal g10 = 0m;
		decimal gM = 0m;
		decimal withdrawal = 0m;
		if (withoutCause | agreement)
		{
			decimal penaltyPct = (agreement ? p.FgtsPenaltyAgreementPct : p.FgtsPenaltyPct);
			penalty = Money.R2((e.FgtsBalance + Money.R2((vBalance + v13) * p.FgtsPct / 100m)) * penaltyPct / 100m);
			b.Add("+ Multa de " + Money.Pct(penaltyPct, 0), penalty, "sobre saldo + depósitos, sem o FGTS do aviso indenizado");
			if (e.PayrollLoanFgtsCollateral && rest > 0m)
			{
				g10 = Math.Min(Money.R2((e.FgtsBalance + dep) * p.PayrollLoanFgtsBalancePct / 100m), rest);
				rest -= g10;
				gM = Math.Min(Money.R2(penalty * p.PayrollLoanFgtsPenaltyPct / 100m), rest);
				rest -= gM;
				if (g10 + gM > 0m)
				{
					b.Add("- Vai para o consignado (garantia)", g10 + gM, $"até {Money.Pct(p.PayrollLoanFgtsBalancePct, 0)} do saldo + até {Money.Pct(p.PayrollLoanFgtsPenaltyPct, 0)} da multa");
				}
			}
			if (e.BirthdayWithdrawal)
			{
				withdrawal = penalty - gM;
				b.Add("= Saca (só a multa)", withdrawal).Text("Saque-aniversário: o saldo fica bloqueado.");
			}
			else
			{
				decimal withdrawableBalance = (agreement ? Money.R2((e.FgtsBalance + dep) * 0.8m) : (e.FgtsBalance + dep));
				withdrawal = Math.Max(0m, withdrawableBalance - g10) + penalty - gM;
				b.Add(agreement ? "= Saca (até 80% do saldo + multa)" : "= Saca", withdrawal);
			}
			if (e.FgtsBalance == 0m)
			{
				warnings.Add("Coloque o saldo do FGTS (app FGTS, \"saldo para fins rescisórios\") para a multa e o saque saírem certos.");
			}
		}
		else
		{
			b.Text(forCause ? "Justa causa: sem saque e sem multa." : "Pedido de demissão: sem saque e sem multa (o saldo continua no FGTS).");
		}
		int installmentCount = 0;
		decimal vBenefit = 0m;
		b.Text("SEGURO-DESEMPREGO");
		if (withoutCause)
		{
			int res = Math.Clamp(e.BenefitPriorClaims, 0, 2);
			if ((res == 0 && months >= 12) || (res == 1 && months >= 9) || (res == 2 && months >= 6))
			{
				if (months >= 24)
				{
					installmentCount = 5;
				}
				else
				{
					installmentCount = ((months >= 12) ? 4 : 3);
				}
				vBenefit = BenefitAmount(payBase, p);
				b.Add($"{installmentCount} parcelas de", vBenefit, "média = salário + média de extras (premissa: últimos 3 meses iguais)").Add("= Total do seguro", vBenefit * (decimal)installmentCount).Text($"({months} meses de casa; tabela {p.UnemploymentBenefit.Year} — {(p.UnemploymentBenefit.Confirmed ? "confirmada" : "estimativa")})");
			}
			else
			{
				b.Text($"Sem direito: precisa de {res switch
				{
					1 => 9, 
					0 => 12, 
					_ => 6, 
				}} meses de trabalho.");
			}
		}
		else
		{
			b.Text("Sem direito neste tipo de desligamento.");
		}
		if (e.PayrollLoanBalance > 0m)
		{
			b.Text("CONSIGNADO").Add("Saldo devedor", e.PayrollLoanBalance).Add("- Abatido na rescisão", ret);
			if (g10 + gM > 0m)
			{
				b.Add("- Abatido do FGTS", g10 + gM);
			}
			b.Add("= Ainda deve", rest);
		}
		decimal total = net - ret + withdrawal + vBenefit * (decimal)installmentCount;
		b.Add("ENTRA NO TOTAL", total, "líquido + saque FGTS + seguro");
		return new TerminationResult(years, months, noticeDays, paidNotice, end, outgoing.AddDays(10), vBalance, balanceDays, vNotice, v13, vPv, tPv, vDv, tDv, noticeDeduction, gross, inssBalance, inss13, ret, net - ret, dep, penalty, g10 + gM, withdrawal, installmentCount, vBenefit, rest, total, warnings, a, b);
	}

	public static decimal BenefitAmount(decimal average, LegalParameters p)
	{
		UnemploymentBenefitTable t = p.UnemploymentBenefit;
		decimal v;
		if (average <= t.Band1)
		{
			v = average * 0.8m;
		}
		else
		{
			v = ((average <= t.Band2) ? (t.Fixed2 + (average - t.Band1) * 0.5m) : t.Ceiling);
		}
		return Math.Max(Money.R2(v), p.MinimumWage);
	}

	public static string Name(TerminationKind t)
	{
		return t switch
		{
			TerminationKind.WithoutCause => "Sem justa causa", 
			TerminationKind.ForCause => "Com justa causa", 
			TerminationKind.Resignation => "Pedido de demissão", 
			TerminationKind.Agreement => "Acordo (art. 484-A)", 
			_ => t.ToString(), 
		};
	}

	public static DateOnly Birthday(DateOnly hire, int k)
	{
		int y = hire.Year + k;
		int d = Math.Min(hire.Day, DateTime.DaysInMonth(y, hire.Month));
		return new DateOnly(y, hire.Month, d);
	}

	public static int FullYears(DateOnly hire, DateOnly d)
	{
		int n = d.Year - hire.Year;
		if (Birthday(hire, n) > d)
		{
			n--;
		}
		return Math.Max(0, n);
	}

	public static int FullMonths(DateOnly hire, DateOnly d)
	{
		int n = (d.Year - hire.Year) * 12 + d.Month - hire.Month;
		if (d.Day + 1 < hire.Day)
		{
			n--;
		}
		return Math.Max(0, n);
	}

	public static int VacationTwelfths(DateOnly hire, DateOnly end)
	{
		DateOnly start = Birthday(hire, FullYears(hire, end.AddDays(1)));
		int n;
		for (n = 0; !(AddMonths(start, n + 1).AddDays(-1) > end); n++)
		{
		}
		if (end.DayNumber - AddMonths(start, n).DayNumber + 1 >= 15)
		{
			n++;
		}
		return Math.Min(n, 12);
	}

	private static DateOnly AddMonths(DateOnly d, int n)
	{
		return new DateOnly(d.Year, d.Month, 1).AddMonths(n).AddDays(d.Day - 1);
	}

	public static int SuggestDueVacation(DateOnly hire, DateOnly projectedEnd, int periodsTaken)
	{
		return 30 * Math.Max(0, FullYears(hire, projectedEnd.AddDays(1)) - periodsTaken);
	}
}
