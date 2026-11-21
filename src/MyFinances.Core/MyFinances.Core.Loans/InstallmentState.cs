using System;
using System.Collections.Generic;
using System.Text;
using MyFinances.Core.Common;

namespace MyFinances.Core.Loans;

public sealed class InstallmentState
{
	public int Version { get; init; }

	public int Number { get; init; }

	public DateOnly DueDate { get; init; }

	public decimal Principal { get; init; }

	public decimal Interest { get; init; }

	public decimal Fee { get; init; }

	public bool Grace { get; init; }

	public string ExpectedBreakdown { get; init; } = "";

	public decimal PaidPrincipal { get; internal set; }

	public decimal PaidInterest { get; internal set; }

	public decimal PaidFee { get; internal set; }

	public decimal PenaltyCharged { get; internal set; }

	public decimal LateInterestCharged { get; internal set; }

	public decimal PaidPenalty { get; internal set; }

	public decimal PaidLateInterest { get; internal set; }

	public bool PenaltyApplied { get; internal set; }

	public DateOnly? LastAccrual { get; internal set; }

	public DateOnly? PaidOffDate { get; internal set; }

	public bool Superseded { get; internal set; }

	public int DaysLate { get; internal set; }

	internal List<string> ChargesBreakdown { get; } = new List<string>();

	public decimal Total => Principal + Interest + Fee;

	public decimal OutstandingPrincipal => Principal - PaidPrincipal;

	public decimal OutstandingInterest => Interest - PaidInterest;

	public decimal OutstandingFee => Fee - PaidFee;

	public decimal OutstandingBase => OutstandingPrincipal + OutstandingInterest + OutstandingFee;

	public decimal OutstandingPenalty => PenaltyCharged - PaidPenalty;

	public decimal OutstandingLateInterest => LateInterestCharged - PaidLateInterest;

	public decimal OutstandingCharges => OutstandingPenalty + OutstandingLateInterest;

	public decimal OutstandingTotal => OutstandingBase + OutstandingCharges;

	public decimal Paid => PaidPrincipal + PaidInterest + PaidFee + PaidPenalty + PaidLateInterest;

	public string Breakdown
	{
		get
		{
			StringBuilder sb = new StringBuilder(ExpectedBreakdown);
			foreach (string l in ChargesBreakdown)
			{
				sb.Append('\n').Append(l);
			}
			if (Paid > 0m)
			{
				StringBuilder stringBuilder = sb.Append('\n');
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(48, 5, stringBuilder);
				handler.AppendLiteral("Pago: principal ");
				handler.AppendFormatted(Money.Num(PaidPrincipal));
				handler.AppendLiteral(", juros ");
				handler.AppendFormatted(Money.Num(PaidInterest));
				handler.AppendLiteral(", tarifa ");
				handler.AppendFormatted(Money.Num(PaidFee));
				handler.AppendLiteral(", multa ");
				handler.AppendFormatted(Money.Num(PaidPenalty));
				handler.AppendLiteral(", mora ");
				handler.AppendFormatted(Money.Num(PaidLateInterest));
				stringBuilder2.Append(ref handler);
			}
			if (OutstandingTotal > 0m)
			{
				StringBuilder stringBuilder = sb.Append('\n');
				StringBuilder stringBuilder3 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 3, stringBuilder);
				handler.AppendLiteral("Em aberto: ");
				handler.AppendFormatted(Money.Num(OutstandingTotal));
				handler.AppendLiteral(" (base ");
				handler.AppendFormatted(Money.Num(OutstandingBase));
				handler.AppendLiteral(" + encargos ");
				handler.AppendFormatted(Money.Num(OutstandingCharges));
				handler.AppendLiteral(")");
				stringBuilder3.Append(ref handler);
			}
			return sb.ToString();
		}
	}

	public InstallmentStatus Status(DateOnly reference)
	{
		if (Superseded)
		{
			return InstallmentStatus.Superseded;
		}
		if (OutstandingTotal <= 0m)
		{
			return InstallmentStatus.Paid;
		}
		if (DueDate < reference)
		{
			return InstallmentStatus.Overdue;
		}
		if (!(Paid > 0m))
		{
			return InstallmentStatus.Upcoming;
		}
		return InstallmentStatus.Partial;
	}
}
