using System;
using System.Collections.Generic;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public static class Overtime
{
	public static OvertimeResult Simulate(OvertimeInput e, LegalParameters p, Calendar cal)
	{
		if (e.Salary <= 0m)
		{
			throw new ArgumentException("Informe o salário base.");
		}
		int monthlyHours = e.MonthlyHours;
		if ((monthlyHours <= 0 || monthlyHours > 300) ? true : false)
		{
			throw new ArgumentException("Jornada mensal: número de horas (a sua é 180).");
		}
		List<string> warnings = new List<string>();
		if (e.RegularOvertimeMinutesPerDay > e.DailyMinutesLimit || e.HolidayOvertimeMinutesPerDay > e.DailyMinutesLimit)
		{
			warnings.Add("Hora extra acima do limite configurado de " + Hhmm(e.DailyMinutesLimit) + " por dia.");
		}
		if (e.HolidayMinutes > 720)
		{
			warnings.Add("Horas no feriado: no máximo 12:00.");
		}
		decimal time = e.Salary / (decimal)e.MonthlyHours;
		decimal ot50 = time * 1.5m;
		decimal ot100 = time * 2m;
		int minRegular = e.RegularDays * e.RegularOvertimeMinutesPerDay;
		int minH = e.HolidaysWorked * e.HolidayMinutes;
		int minHolidayOt = e.HolidaysWorked * e.HolidayOvertimeMinutesPerDay;
		decimal vReg = Money.R2((decimal)minRegular / 60m * ot50);
		decimal vVac = Money.R2((decimal)minH / 60m * ot100);
		decimal vHolOt = Money.R2((decimal)minHolidayOt / 60m * ot100);
		decimal tot = vReg + vVac + vHolOt;
		YearMonth due = e.BankDueDate;
		int desc = cal.RestDaysInMonth(due);
		int businessDays = due.DaysInMonth - desc;
		decimal dsr = ((businessDays > 0) ? Money.R2(tot * (decimal)desc / (decimal)businessDays) : 0m);
		decimal earn = tot + dsr;
		decimal inssExtra = Money.R2(Inss.Calculate(e.CurrentMonthInssBase + earn, p.Inss) - Inss.Calculate(e.CurrentMonthInssBase, p.Inss));
		YearMonth payMonth = due.Next;
		string key = $"{due.Month:00}/{due.Year % 100:00}";
		CalcBreakdown bd = new CalcBreakdown().Add($"Hora ({Money.Num(e.Salary)} / {e.MonthlyHours})", Money.R(time, 4), "sem arredondar nas contas").Add("Hora extra 50%", Money.R(ot50, 4), "hora × 1,5").Add("Hora 100% (feriado)", Money.R(ot100, 4), "hora × 2")
			.Add("HE 50% (" + Hhmm(minRegular) + ")", vReg, $"{Hhmm(minRegular)} × {Money.R(ot50, 4)}")
			.Add("Feriado 100% (" + Hhmm(minH) + ")", vVac)
			.Add("HE feriado 100% (" + Hhmm(minHolidayOt) + ")", vHolOt)
			.Add($"DSR s/ HE ({desc} / {businessDays})", dsr, "total HE × descansos / dias úteis")
			.Add("= Proventos", earn)
			.Add("- INSS a mais", inssExtra, "INSS(base atual + proventos) − INSS(base atual)")
			.Add("= Líquido a mais", earn - inssExtra)
			.Text($"Folha de {key}, paga no mês {payMonth}. DSR: {desc} descansos (domingos e feriados) e {businessDays} dias úteis.");
		List<PlanItem> plan = new List<PlanItem>();
		if (vReg > 0m)
		{
			plan.Add(new PlanItem(payMonth, EntryKind.Earning, $"HE {key}: hora extra 50% ({Hhmm(minRegular)})", vReg));
		}
		if (vVac > 0m)
		{
			plan.Add(new PlanItem(payMonth, EntryKind.Earning, $"HE {key}: feriado 100% ({Hhmm(minH)})", vVac));
		}
		if (vHolOt > 0m)
		{
			plan.Add(new PlanItem(payMonth, EntryKind.Earning, $"HE {key}: hora extra feriado 100% ({Hhmm(minHolidayOt)})", vHolOt));
		}
		if (dsr > 0m)
		{
			plan.Add(new PlanItem(payMonth, EntryKind.Earning, "HE " + key + ": DSR sobre hora extra", dsr));
		}
		if (tot == 0m)
		{
			warnings.Add("Nada para calcular: coloque dias e horas.");
		}
		return new OvertimeResult(time, ot50, ot100, vReg, vVac, vHolOt, tot, desc, businessDays, dsr, earn, inssExtra, earn - inssExtra, payMonth, key, plan, warnings, bd);
	}

	public static string Hhmm(int minutes)
	{
		return $"{minutes / 60}:{minutes % 60:00}";
	}

	public static bool TryParseHours(string? txt, out int minutes)
	{
		minutes = 0;
		string t = (txt ?? "").Trim();
		if (t.Length == 0)
		{
			return true;
		}
		string[] p = t.Split(':');
		if (p.Length == 1 && int.TryParse(p[0], out var h))
		{
			minutes = h * 60;
			return h >= 0;
		}
		if (p.Length == 2 && int.TryParse(p[0], out h) && int.TryParse(p[1], out var m) && m >= 0 && m < 60 && h >= 0)
		{
			minutes = h * 60 + m;
			return true;
		}
		return false;
	}

	public static string? KeyOf(string description)
	{
		string n = description.Trim();
		if (n.Length < 9 || !n.StartsWith("HE ") || n[5] != '/' || n[8] != ':')
		{
			return null;
		}
		return n.Substring(3, 5);
	}
}
