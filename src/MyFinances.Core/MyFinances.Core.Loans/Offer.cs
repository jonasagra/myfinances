using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyFinances.Core.Common;

namespace MyFinances.Core.Loans;

public static class Offer
{
	public static string Text(string? clientName, LoanTerms c, IReadOnlyList<ScheduledInstallment> installments)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(string.IsNullOrWhiteSpace(clientName) ? "Proposta de empréstimo" : ("Proposta de empréstimo — " + clientName));
		sb.AppendLine();
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder);
		handler.AppendLiteral("Valor: ");
		handler.AppendFormatted(Money.Fmt(c.Principal));
		stringBuilder2.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 3, stringBuilder);
		handler.AppendLiteral("Juros: ");
		handler.AppendFormatted(Money.Pct(c.InterestRatePct));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(Schedule.PeriodName(c.Frequency));
		handler.AppendLiteral(" (");
		handler.AppendFormatted(Schedule.MethodName(c.Method));
		handler.AppendLiteral(")");
		stringBuilder3.AppendLine(ref handler);
		if (c.GracePeriods > 0)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder4 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(24, 2, stringBuilder);
			handler.AppendLiteral("Carência: ");
			handler.AppendFormatted(c.GracePeriods);
			handler.AppendLiteral(" período(s) — ");
			handler.AppendFormatted((c.GraceKind == GraceKind.PaysInterest) ? "paga só os juros" : "sem pagamento");
			stringBuilder4.AppendLine(ref handler);
		}
		if (c.SetupFee > 0m)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder5 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(30, 1, stringBuilder);
			handler.AppendLiteral("Tarifa: ");
			handler.AppendFormatted(Money.Fmt(c.SetupFee));
			handler.AppendLiteral(" (junto da 1ª parcela)");
			stringBuilder5.AppendLine(ref handler);
		}
		stringBuilder = sb;
		StringBuilder stringBuilder6 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder);
		handler.AppendLiteral("Parcelas: ");
		handler.AppendFormatted(installments.Count);
		stringBuilder6.AppendLine(ref handler);
		List<IGrouping<decimal, ScheduledInstallment>> groups = (from scheduledInstallment in installments
			group scheduledInstallment by scheduledInstallment.Total).ToList();
		if (groups.Count == 1)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder7 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder);
			handler.AppendLiteral("Valor de cada parcela: ");
			handler.AppendFormatted(Money.Fmt(groups[0].Key));
			stringBuilder7.AppendLine(ref handler);
		}
		else
		{
			foreach (ScheduledInstallment p in installments)
			{
				stringBuilder = sb;
				StringBuilder stringBuilder8 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(p.Number);
				handler.AppendLiteral("ª — ");
				handler.AppendFormatted(p.DueDate, "dd/MM/yyyy");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(Money.Fmt(p.Total));
				stringBuilder8.AppendLine(ref handler);
			}
		}
		stringBuilder = sb;
		StringBuilder stringBuilder9 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
		handler.AppendLiteral("1º vencimento: ");
		handler.AppendFormatted(installments[0].DueDate, "dd/MM/yyyy");
		stringBuilder9.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder10 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder);
		handler.AppendLiteral("Total a pagar: ");
		handler.AppendFormatted(Money.Fmt(installments.Sum((ScheduledInstallment scheduledInstallment) => scheduledInstallment.Total)));
		stringBuilder10.AppendLine(ref handler);
		sb.AppendLine();
		stringBuilder = sb;
		StringBuilder stringBuilder11 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(71, 2, stringBuilder);
		handler.AppendLiteral("Em caso de atraso: multa de ");
		handler.AppendFormatted(Money.Pct(c.LateRules.PenaltyPct));
		handler.AppendLiteral(" + juros de ");
		handler.AppendFormatted(Money.Pct(c.LateRules.MonthlyLateInterestPct));
		handler.AppendLiteral(" ao mês, proporcional aos dias.");
		stringBuilder11.AppendLine(ref handler);
		return sb.ToString().TrimEnd();
	}
}
