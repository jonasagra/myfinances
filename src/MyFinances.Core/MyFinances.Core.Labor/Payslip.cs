using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record Payslip(YearMonth? Competence, DateOnly? PaidOn, decimal? FixedSalary, IReadOnlyList<PayslipLine> Lines, decimal? TotalEarnings, decimal? TotalDeductions, decimal? Net)
{
	public YearMonth? TargetMonth
	{
		get
		{
			DateOnly? paidOn = PaidOn;
			if (paidOn.HasValue)
			{
				DateOnly d = paidOn.GetValueOrDefault();
				return YearMonth.From(d);
			}
			return Competence?.Next;
		}
	}

	private static readonly string[] MonthNames = new string[12]
	{
		"JANEIRO", "FEVEREIRO", "MARCO", "ABRIL", "MAIO", "JUNHO", "JULHO", "AGOSTO", "SETEMBRO", "OUTUBRO",
		"NOVEMBRO", "DEZEMBRO"
	};

	private static readonly Regex LineRx = new Regex("^\\s*\\d+\\s+(\\d{3,5})\\s+(.+?)\\s+(-?[\\d.]*\\d,\\d+)\\s+(-?[\\d.]*\\d,\\d{2})\\s+(-?[\\d.]*\\d,\\d{2})\\s*$");

	private static readonly Regex DateRx = new Regex("\\b(\\d{2}/\\d{2}/\\d{4})\\b");

	private static readonly Regex CompetenceRx = new Regex("\\b([A-ZÇ]+)/(\\d{4})\\b");

	private static readonly Regex NumberRx = new Regex("-?[\\d.]*\\d[.,]\\d{2}\\b");

	public static Payslip Parse(IEnumerable<string> textLines)
	{
		List<string> lines = (from text in textLines
			select text.Trim() into text
			where text.Length > 0
			select text).ToList();
		YearMonth? competence = null;
		DateOnly? paidOn = null;
		decimal? fixedSalary = null;
		decimal? totalEarnings = null;
		decimal? totalDeductions = null;
		decimal? net = null;
		List<PayslipLine> items = new List<PayslipLine>();
		for (int i = 0; i < lines.Count; i++)
		{
			string l = lines[i];
			string u = Plain(l);
			Match m = LineRx.Match(l);
			if (m != null && m.Success)
			{
				items.Add(new PayslipLine(m.Groups[1].Value, m.Groups[2].Value.Trim(), ParseNumber(m.Groups[3].Value).GetValueOrDefault(), ParseNumber(m.Groups[4].Value).GetValueOrDefault(), ParseNumber(m.Groups[5].Value).GetValueOrDefault()));
				continue;
			}
			if (u.Contains("COMPETENCIA"))
			{
				string c = Next(i);
				if (c != null)
				{
					Match cm = CompetenceRx.Match(Plain(c));
					if (cm != null && cm.Success)
					{
						int idx = Array.IndexOf(MonthNames, cm.Groups[1].Value);
						if (idx >= 0)
						{
							competence = new YearMonth(int.Parse(cm.Groups[2].Value), idx + 1);
						}
					}
				}
			}
			if (u.Contains("PAGTO"))
			{
				string p = Next(i);
				if (p != null)
				{
					Match dm = DateRx.Match(p);
					if (dm != null && dm.Success && DateOnly.TryParseExact(dm.Groups[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
					{
						paidOn = d;
					}
					if (u.Contains("FIXO"))
					{
						Match sm = NumberRx.Match(p);
						if (sm != null && sm.Success)
						{
							fixedSalary = ParseNumber(sm.Value);
						}
					}
				}
			}
			if (u.Contains("TOTAL DE PROVENTOS"))
			{
				totalEarnings = LastNumber(Next(i));
			}
			if (u.Contains("TOTAL DE DESCONTOS"))
			{
				totalDeductions = LastNumber(Next(i));
			}
			if (u.Contains("LIQUIDO A RECEBER"))
			{
				net = LastNumber(Next(i));
			}
		}
		if (items.Count == 0)
		{
			throw new FormatException("Não encontrei as linhas de proventos e descontos. Esse arquivo é um contracheque?");
		}
		return new Payslip(competence, paidOn, fixedSalary, items, totalEarnings, totalDeductions, net);
		static decimal? LastNumber(string? text)
		{
			if (text != null)
			{
				return (from match in NumberRx.Matches(text)
					select ParseNumber(match.Value)).LastOrDefault((decimal? v) => v.HasValue);
			}
			return null;
		}
		string? Next(int num)
		{
			if (num + 1 >= lines.Count)
			{
				return null;
			}
			return lines[num + 1];
		}
	}

	private static decimal? ParseNumber(string s)
	{
		s = s.Trim();
		if (s.Contains(','))
		{
			if (!decimal.TryParse(s.Replace(".", ""), NumberStyles.Number, Money.PtBr, out var a))
			{
				return null;
			}
			return a;
		}
		if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var b))
		{
			return null;
		}
		return b;
	}

	internal static string Plain(string s)
	{
		return new string((from c in s.ToUpperInvariant().Normalize(NormalizationForm.FormD)
			where CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
			select c).ToArray());
	}

	public static PayslipLineKind Classify(PayslipLine l)
	{
		string d = Plain(l.Description);
		if (l.Earning != 0m)
		{
			if (!d.StartsWith("SALARIO") || (!d.Contains("CONTRATUAL") && !(d == "SALARIO") && !d.Contains("BASE")))
			{
				return PayslipLineKind.Earning;
			}
			return PayslipLineKind.BaseSalary;
		}
		if (d.Contains("INSS"))
		{
			return PayslipLineKind.Inss;
		}
		if (d.Contains("IRRF") || d.Contains("IMPOSTO DE RENDA"))
		{
			return PayslipLineKind.Irrf;
		}
		if (d.Contains("VALE TRANSPORTE") || d.Contains("VALE-TRANSPORTE"))
		{
			return PayslipLineKind.TransportVoucher;
		}
		if (d.Contains("ATRASO") || d.Contains("SAIDA ANTECIPADA"))
		{
			return PayslipLineKind.LateHours;
		}
		if (d.Contains("DESCANSO SEMANAL") || d.Contains("DSR"))
		{
			return PayslipLineKind.LostDsr;
		}
		if (d.Contains("FALTA") || d.Contains("SUSPENSAO"))
		{
			return PayslipLineKind.AbsenceDays;
		}
		return PayslipLineKind.Deduction;
	}

	public static bool IsCalculatedByApp(PayslipLineKind k)
	{
		if ((uint)(k - 3) <= 5u)
		{
			return true;
		}
		return false;
	}

	internal static string Friendly(string description)
	{
		string t = Regex.Replace(description.Trim().ToLower(Money.PtBr), "\\s+", " ");
		t = Regex.Replace(t, "\\b(\\d)o\\b", "$1º");
		t = t.Replace("inss", "INSS").Replace("irrf", "IRRF").Replace("dsr", "DSR");
		if (t.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpper(t[0], Money.PtBr));
			string text = t;
			return string.Concat(readOnlySpan, text.Substring(1, text.Length - 1));
		}
		return t;
	}

	public MonthAbsences Absences(decimal salary, int workload)
	{
		if (salary <= 0m)
		{
			return MonthAbsences.None;
		}
		return new MonthAbsences(Money.R2(Sum(PayslipLineKind.AbsenceDays) * 30m / salary), Money.R2(Sum(PayslipLineKind.LostDsr) * 30m / salary), Money.R(Sum(PayslipLineKind.LateHours) * (decimal)workload / salary, 4));
		decimal Sum(PayslipLineKind k)
		{
			return Lines.Where((PayslipLine l) => l.Kind == k).Sum((PayslipLine l) => l.Deduction);
		}
	}

	public static HashSet<PayslipLine> AlreadyPosted(IReadOnlyList<PayslipLine> lines, IReadOnlyList<decimal> existing)
	{
		HashSet<PayslipLine> found = new HashSet<PayslipLine>(ReferenceEqualityComparer.Instance);
		if (lines.Count == 0)
		{
			return found;
		}
		if (existing.Count > 0 && Money.R2(lines.Sum((PayslipLine payslipLine) => payslipLine.Amount)) == Money.R2(existing.Sum()))
		{
			foreach (PayslipLine l in lines)
			{
				found.Add(l);
			}
			return found;
		}
		List<decimal> left = existing.ToList();
		foreach (PayslipLine l2 in lines)
		{
			int i = left.FindIndex((decimal v) => v == l2.Amount);
			if (i >= 0)
			{
				left.RemoveAt(i);
				found.Add(l2);
			}
		}
		return found;
	}
}
