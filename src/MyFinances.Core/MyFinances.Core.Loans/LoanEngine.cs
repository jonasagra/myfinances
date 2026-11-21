using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyFinances.Core.Common;

namespace MyFinances.Core.Loans;

public static class LoanEngine
{
	public static LoanPosition Calculate(LoanCalc loan, DateOnly reference)
	{
		if (loan.Versions.Count == 0)
		{
			throw new ArgumentException("Contrato sem versões.");
		}
		List<VersionCalc> versions = (from versionCalc in loan.Versions
			orderby versionCalc.Start, versionCalc.Number
			select versionCalc).ToList();
		List<PaymentCalc> payments = (from paymentCalc in loan.Payments
			where !paymentCalc.Reversed && paymentCalc.Amount > 0m
			orderby paymentCalc.Timestamp
			select paymentCalc).ToList();
		List<InstallmentState> all = new List<InstallmentState>();
		List<PaymentAllocation> allocations = new List<PaymentAllocation>();
		DateTime? stateDate = loan.StateDate;
		DateOnly? dateOnly;
		if (stateDate.HasValue)
		{
			DateTime from = stateDate.GetValueOrDefault();
			dateOnly = DateOnly.FromDateTime(from);
		}
		else
		{
			dateOnly = null;
		}
		DateOnly? stateDate2 = dateOnly;
		DateOnly dateOnly2;
		if (loan.State != SpecialState.None && stateDate2.HasValue)
		{
			DateOnly lim = stateDate2.GetValueOrDefault();
			if (lim < reference)
			{
				dateOnly2 = lim;
				goto IL_0143;
			}
		}
		dateOnly2 = reference;
		goto IL_0143;
		IL_0143:
		DateOnly accrualLimit = dateOnly2;
		decimal loss = 0m;
		bool writeOffComputed = false;
		for (int vi = 0; vi < versions.Count; vi++)
		{
			VersionCalc v = versions[vi];
			LateRules rules = v.Terms.LateRules;
			DateTime versionEnd = ((vi + 1 < versions.Count) ? versions[vi + 1].Start : DateTime.MaxValue);
			List<InstallmentState> installments = (from scheduledInstallment in v.Installments
				select new InstallmentState
				{
					Version = v.Number,
					Number = scheduledInstallment.Number,
					DueDate = scheduledInstallment.DueDate,
					Principal = scheduledInstallment.Principal,
					Interest = scheduledInstallment.Interest,
					Fee = scheduledInstallment.Fee,
					Grace = scheduledInstallment.Grace,
					ExpectedBreakdown = scheduledInstallment.Breakdown
				} into installmentState
				orderby installmentState.DueDate, installmentState.Number
				select installmentState).ToList();
			all.AddRange(installments);
			foreach (PaymentCalc pmt in payments.Where((PaymentCalc paymentCalc) => paymentCalc.Timestamp >= v.Start && paymentCalc.Timestamp < versionEnd))
			{
				DateOnly day = DateOnly.FromDateTime(pmt.Timestamp);
				int num;
				if (loan.State == SpecialState.WrittenOff)
				{
					stateDate = loan.StateDate;
					if (stateDate.HasValue)
					{
						DateTime db = stateDate.GetValueOrDefault();
						num = ((pmt.Timestamp > db) ? 1 : 0);
						goto IL_02a0;
					}
				}
				num = 0;
				goto IL_02a0;
				IL_02a0:
				bool afterWriteOff = (byte)num != 0;
				if (afterWriteOff && !writeOffComputed && vi == versions.Count - 1)
				{
					loss = installments.Sum((InstallmentState installmentState) => installmentState.OutstandingPrincipal);
					writeOffComputed = true;
				}
				Accrue(installments, rules, Min(day, accrualLimit));
				allocations.Add(Allocate(installments, pmt, day, v.Number, afterWriteOff));
			}
			if (vi + 1 < versions.Count)
			{
				foreach (InstallmentState item in installments.Where((InstallmentState installmentState) => installmentState.OutstandingTotal > 0m))
				{
					item.Superseded = true;
				}
				continue;
			}
			Accrue(installments, rules, accrualLimit);
			if (loan.State == SpecialState.WrittenOff && !writeOffComputed)
			{
				loss = installments.Sum((InstallmentState installmentState) => installmentState.OutstandingPrincipal);
			}
		}
		VersionCalc current = versions[versions.Count - 1];
		List<InstallmentState> live = all.Where((InstallmentState installmentState) => installmentState.Version == current.Number).ToList();
		foreach (InstallmentState p in live)
		{
			p.DaysLate = ((p.OutstandingTotal > 0m && p.DueDate < reference) ? (reference.DayNumber - p.DueDate.DayNumber) : 0);
		}
		int tol = current.Terms.LateRules.ToleranceDays;
		List<InstallmentState> overdue = live.Where((InstallmentState installmentState) => installmentState.OutstandingTotal > 0m && installmentState.DueDate.AddDays(tol) < reference).ToList();
		int longestDelay = ((overdue.Count != 0) ? overdue.Max((InstallmentState installmentState) => installmentState.DaysLate) : 0);
		decimal outstandingPrincipal = live.Sum((InstallmentState installmentState) => installmentState.OutstandingPrincipal);
		decimal outstandingTotal = live.Sum((InstallmentState installmentState) => installmentState.OutstandingTotal);
		LoanStatus status = loan.State switch
		{
			SpecialState.Canceled => LoanStatus.Canceled, 
			SpecialState.WrittenOff => LoanStatus.WrittenOff, 
			_ => (!(outstandingTotal <= 0m)) ? ((longestDelay <= current.Terms.LateRules.DaysAtRisk) ? ((overdue.Count > 0) ? LoanStatus.Late : LoanStatus.OnTime) : LoanStatus.AtRisk) : LoanStatus.PaidOff, 
		};
		decimal expectedInterest = all.Where((InstallmentState installmentState) => !installmentState.Superseded || installmentState.PaidInterest > 0m).Sum((InstallmentState installmentState) => (!installmentState.Superseded) ? installmentState.Interest : installmentState.PaidInterest);
		return new LoanPosition(loan.Id, reference, status, current.Number, all, allocations, versions[0].Terms.Principal, (status == LoanStatus.Canceled) ? 0m : outstandingPrincipal, expectedInterest, overdue.Sum((InstallmentState installmentState) => installmentState.OutstandingInterest), live.Sum((InstallmentState installmentState) => installmentState.OutstandingCharges), overdue.Sum((InstallmentState installmentState) => installmentState.OutstandingTotal), overdue.Count, longestDelay, loss, (loan.State == SpecialState.WrittenOff) ? stateDate2 : ((DateOnly?)null), versions.Sum((VersionCalc versionCalc) => versionCalc.DiscountGranted), allocations.Sum((PaymentAllocation a) => a.Surplus));
	}

	private static DateOnly Min(DateOnly a, DateOnly b)
	{
		if (!(a < b))
		{
			return b;
		}
		return a;
	}

	internal static void Accrue(List<InstallmentState> installments, LateRules r, DateOnly until)
	{
		foreach (InstallmentState p in installments)
		{
			if (p.OutstandingBase <= 0m || until <= p.DueDate.AddDays(r.ToleranceDays))
			{
				continue;
			}
			if (!p.PenaltyApplied)
			{
				p.PenaltyApplied = true;
				decimal penalty = Money.R2(p.OutstandingBase * r.PenaltyPct / 100m);
				p.PenaltyCharged += penalty;
				if (penalty > 0m)
				{
					p.ChargesBreakdown.Add($"{until:dd/MM/yyyy}: multa = ARRED({Money.Num(p.OutstandingBase)} × {Money.Pct(r.PenaltyPct)}) = {Money.Num(penalty)}");
				}
			}
			DateOnly? lastAccrual = p.LastAccrual;
			DateOnly dateOnly;
			if (lastAccrual.HasValue)
			{
				DateOnly u = lastAccrual.GetValueOrDefault();
				if (u > p.DueDate)
				{
					dateOnly = u;
					goto IL_0153;
				}
			}
			dateOnly = p.DueDate;
			goto IL_0153;
			IL_0153:
			DateOnly since = dateOnly;
			int days = until.DayNumber - since.DayNumber;
			if (days > 0)
			{
				decimal lateInterest = Money.R2(p.OutstandingBase * r.MonthlyLateInterestPct / 100m / 30m * (decimal)days);
				p.LateInterestCharged += lateInterest;
				if (lateInterest > 0m)
				{
					p.ChargesBreakdown.Add($"{since:dd/MM/yyyy}→{until:dd/MM/yyyy}: mora = ARRED({Money.Num(p.OutstandingBase)} × {Money.Pct(r.MonthlyLateInterestPct)} ÷ 30 × {days} dias) = {Money.Num(lateInterest)}");
				}
			}
			p.LastAccrual = until;
		}
	}

	private static PaymentAllocation Allocate(List<InstallmentState> installments, PaymentCalc pmt, DateOnly day, int version, bool afterWriteOff)
	{
		List<AllocationItem> items = new List<AllocationItem>();
		StringBuilder sb = new StringBuilder($"Pagamento de {Money.Fmt(pmt.Amount)} em {pmt.Timestamp:dd/MM/yyyy HH:mm}");
		if (afterWriteOff)
		{
			sb.Append(" (após a baixa: recuperação de crédito)");
		}
		decimal rest = pmt.Amount;
		foreach (InstallmentState p in installments.Where((InstallmentState installmentState) => installmentState.DueDate <= day && installmentState.OutstandingTotal > 0m))
		{
			Take(p, Component.Penalty, p.OutstandingPenalty, (decimal v) =>
			{
				p.PaidPenalty += v;
			});
			Take(p, Component.LateInterest, p.OutstandingLateInterest, (decimal v) =>
			{
				p.PaidLateInterest += v;
			});
			Take(p, Component.Interest, p.OutstandingInterest, (decimal v) =>
			{
				p.PaidInterest += v;
			});
			Take(p, Component.Fee, p.OutstandingFee, (decimal v) =>
			{
				p.PaidFee += v;
			});
			Take(p, Component.Principal, p.OutstandingPrincipal, (decimal v) =>
			{
				p.PaidPrincipal += v;
			});
			if (p.OutstandingTotal <= 0m && !p.PaidOffDate.HasValue)
			{
				p.PaidOffDate = day;
			}
			if (rest <= 0m)
			{
				break;
			}
		}
		if (rest > 0m)
		{
			sb.Append("\n  sobra antecipa as próximas parcelas:");
			foreach (InstallmentState p2 in installments.Where((InstallmentState installmentState) => installmentState.DueDate > day && installmentState.OutstandingTotal > 0m))
			{
				Take(p2, Component.Interest, p2.OutstandingInterest, (decimal v) =>
				{
					p2.PaidInterest += v;
				});
				Take(p2, Component.Fee, p2.OutstandingFee, (decimal v) =>
				{
					p2.PaidFee += v;
				});
				Take(p2, Component.Principal, p2.OutstandingPrincipal, (decimal v) =>
				{
					p2.PaidPrincipal += v;
				});
				if (p2.OutstandingTotal <= 0m && !p2.PaidOffDate.HasValue)
				{
					p2.PaidOffDate = day;
				}
				if (rest <= 0m)
				{
					break;
				}
			}
		}
		if (rest > 0m)
		{
			StringBuilder stringBuilder = sb;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(44, 1, stringBuilder);
			handler.AppendLiteral("\n  excedente (valor a devolver ao cliente): ");
			handler.AppendFormatted(Money.Num(rest));
			stringBuilder.Append(ref handler);
		}
		return new PaymentAllocation(pmt.Id, pmt.Timestamp, pmt.Amount, version, items, rest, afterWriteOff, sb.ToString());
		decimal Take(InstallmentState installmentState, Component c, decimal outstanding, Action<decimal> toPay)
		{
			if (rest <= 0m || outstanding <= 0m)
			{
				return 0m;
			}
			decimal v = Math.Min(rest, outstanding);
			toPay(v);
			rest -= v;
			items.Add(new AllocationItem(version, installmentState.Number, c, v));
			StringBuilder stringBuilder2 = sb;
			StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(17, 4, stringBuilder2);
			handler2.AppendLiteral("\n  parcela ");
			handler2.AppendFormatted(installmentState.Number);
			handler2.AppendLiteral(" (");
			handler2.AppendFormatted(installmentState.DueDate, "dd/MM/yyyy");
			handler2.AppendLiteral(") ");
			handler2.AppendFormatted(Name(c));
			handler2.AppendLiteral(": ");
			handler2.AppendFormatted(Money.Num(v));
			stringBuilder2.Append(ref handler2);
			return v;
		}
	}

	public static string Name(Component c)
	{
		return c switch
		{
			Component.Penalty => "multa", 
			Component.LateInterest => "mora", 
			Component.Interest => "juros", 
			Component.Fee => "tarifa", 
			Component.Principal => "principal", 
			_ => c.ToString(), 
		};
	}

	public static string Name(LoanStatus s)
	{
		return s switch
		{
			LoanStatus.OnTime => "Em dia", 
			LoanStatus.Late => "Em atraso", 
			LoanStatus.AtRisk => "Inadimplente (em risco)", 
			LoanStatus.PaidOff => "Quitado", 
			LoanStatus.Canceled => "Cancelado", 
			LoanStatus.WrittenOff => "Baixado como perda", 
			_ => s.ToString(), 
		};
	}

	public static string Name(InstallmentStatus s)
	{
		return s switch
		{
			InstallmentStatus.Upcoming => "A vencer", 
			InstallmentStatus.Partial => "Parcial", 
			InstallmentStatus.Overdue => "Vencida", 
			InstallmentStatus.Paid => "Paga", 
			InstallmentStatus.Superseded => "Substituída", 
			_ => s.ToString(), 
		};
	}

	public static (decimal Principal, decimal OverdueInterest, decimal Charges, IReadOnlyList<InstallmentState> Open) OutstandingOnDate(LoanCalc c, DateOnly date)
	{
		LoanPosition pos = Calculate(c, date);
		List<InstallmentState> live = pos.Installments.Where((InstallmentState p) => p.Version == pos.CurrentVersion && p.OutstandingTotal > 0m).ToList();
		return (Principal: live.Sum((InstallmentState p) => p.OutstandingPrincipal), OverdueInterest: live.Where((InstallmentState p) => p.DueDate <= date).Sum((InstallmentState p) => p.OutstandingInterest + p.OutstandingFee), Charges: live.Sum((InstallmentState p) => p.OutstandingCharges), Open: live);
	}
}
