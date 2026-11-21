using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class PayslipImportViewModel : ObservableObject
{
	private readonly Payslip _payslip;

	private string _month;

	private YearMonth? _target;

	private MonthData? _data;

	private bool _fillAbsences;

	private bool _updateSalary;

	private string _error = "";

	private decimal _appNet;

	public string FileName { get; }

	public string Title
	{
		get
		{
			YearMonth? competence = _payslip.Competence;
			if (competence.HasValue)
			{
				YearMonth c = competence.GetValueOrDefault();
				return $"Contracheque de {c.MonthNameLong.ToLowerInvariant()}/{c.Year}";
			}
			return "Contracheque";
		}
	}

	public string PaidText
	{
		get
		{
			DateOnly? paidOn = _payslip.PaidOn;
			if (paidOn.HasValue)
			{
				DateOnly d = paidOn.GetValueOrDefault();
				return $"pago em {d:dd/MM/yyyy}";
			}
			return "";
		}
	}

	public ObservableCollection<PayslipImportRow> Rows { get; } = new ObservableCollection<PayslipImportRow>();

	public ObservableCollection<PayslipCheckRow> Checks { get; } = new ObservableCollection<PayslipCheckRow>();

	public string Month
	{
		get
		{
			return _month;
		}
		set
		{
			if (Set(ref _month, value, "Month"))
			{
				Reload();
			}
		}
	}

	public YearMonth? Target => _target;

	public string Error
	{
		get
		{
			return _error;
		}
		private set
		{
			Set(ref _error, value, "Error");
		}
	}

	public decimal? PayslipSalary => _payslip.FixedSalary ?? _payslip.Lines.FirstOrDefault((PayslipLine l) => l.Kind == PayslipLineKind.BaseSalary)?.Earning;

	public bool SalaryDiffers
	{
		get
		{
			decimal? payslipSalary = PayslipSalary;
			if (payslipSalary.HasValue)
			{
				decimal s = payslipSalary.GetValueOrDefault();
				if ((object)_data != null)
				{
					return s != _data.Salary;
				}
			}
			return false;
		}
	}

	public string SalaryText => $"Salário base de {Money.Fmt(PayslipSalary.GetValueOrDefault())} (no mês está {Money.Fmt(_data?.Salary ?? 0m)})";

	public bool UpdateSalary
	{
		get
		{
			return _updateSalary;
		}
		set
		{
			if (Set(ref _updateSalary, value, "UpdateSalary"))
			{
				Recalculate();
			}
		}
	}

	public MonthAbsences PayslipAbsences => _payslip.Absences(SalaryUsed, Svc.Config.Preferences().Payroll.MonthlyHours);

	public bool AbsencesDiffer
	{
		get
		{
			if ((object)_data == null)
			{
				return false;
			}
			int workload = Svc.Config.Preferences().Payroll.MonthlyHours;
			return Payroll.AbsenceDeduction(SalaryUsed, PayslipAbsences, workload) != Payroll.AbsenceDeduction(SalaryUsed, _data.Absences, workload);
		}
	}

	public string AbsencesText
	{
		get
		{
			MonthAbsences a = PayslipAbsences;
			List<string> parts = new List<string>();
			if (a.AbsenceDays > 0m)
			{
				parts.Add(a.AbsenceDays.ToString("0.##", Money.PtBr) + " falta(s)");
			}
			if (a.LostDsr > 0m)
			{
				parts.Add(a.LostDsr.ToString("0.##", Money.PtBr) + " DSR perdido(s)");
			}
			if (a.LateHours > 0m)
			{
				parts.Add(a.LateHours.ToString("0.##", Money.PtBr) + " h de atraso");
			}
			return "Faltas e atrasos: " + ((parts.Count == 0) ? "nenhum" : string.Join(", ", parts));
		}
	}

	public bool FillAbsences
	{
		get
		{
			return _fillAbsences;
		}
		set
		{
			if (Set(ref _fillAbsences, value, "FillAbsences"))
			{
				Recalculate();
			}
		}
	}

	public decimal? PayslipNet => _payslip.Net;

	public decimal AppNet
	{
		get
		{
			return _appNet;
		}
		private set
		{
			Set(ref _appNet, value, "AppNet");
			OnPropertyChanged("NetMatches");
			OnPropertyChanged("NetDifference");
		}
	}

	public bool NetMatches => PayslipNet == (decimal?)AppNet;

	public string NetDifference
	{
		get
		{
			decimal? payslipNet = PayslipNet;
			if (payslipNet.HasValue)
			{
				decimal n = payslipNet.GetValueOrDefault();
				if (n != AppNet)
				{
					return "diferença de " + Money.Fmt(AppNet - n);
				}
			}
			return "";
		}
	}

	private decimal SalaryUsed
	{
		get
		{
			if (UpdateSalary)
			{
				decimal? payslipSalary = PayslipSalary;
				if (payslipSalary.HasValue)
				{
					return payslipSalary.GetValueOrDefault();
				}
			}
			return _data?.Salary ?? PayslipSalary.GetValueOrDefault();
		}
	}

	public List<PayslipImportRow> Selected => Rows.Where((PayslipImportRow r) => r.Include).ToList();

	public PayslipImportViewModel(Payslip payslip, string fileName)
	{
		_payslip = payslip;
		FileName = fileName;
		YearMonth? targetMonth = payslip.TargetMonth;
		string month;
		if (targetMonth.HasValue)
		{
			YearMonth m = targetMonth.GetValueOrDefault();
			month = Input.Month(m);
		}
		else
		{
			month = Input.Month(YearMonth.Today);
		}
		_month = month;
		foreach (PayslipLine l in payslip.Lines.Where((PayslipLine payslipLine) =>
		{
			PayslipLineKind kind = payslipLine.Kind;
			return (uint)(kind - 1) <= 1u && payslipLine.Amount != 0m;
		}))
		{
			Rows.Add(new PayslipImportRow(l, Recalculate));
		}
		Reload();
	}

	private void Reload()
	{
		List<string> errors = new List<string>();
		YearMonth m = Input.Month(_month, "Mês", errors);
		Error = string.Join(" ", errors);
		if (errors.Count > 0)
		{
			_target = null;
			return;
		}
		_target = m;
		_data = Svc.Finance.GetMonth(m);
		EntryKind[] array = new EntryKind[2]
		{
			EntryKind.Earning,
			EntryKind.Deduction
		};
		foreach (EntryKind kind in array)
		{
			List<PayslipImportRow> list = Rows.Where((PayslipImportRow payslipImportRow) => payslipImportRow.Kind == kind).ToList();
			HashSet<PayslipLine> posted = Payslip.AlreadyPosted(existing: (from e in _data.Entries
				where e.Kind == (int)kind
				select e.Amount).ToList(), lines: list.Select((PayslipImportRow payslipImportRow) => payslipImportRow.Line).ToList());
			foreach (PayslipImportRow r in list)
			{
				r.Posted = posted.Contains(r.Line);
				r.Include = !r.Posted;
			}
		}
		_updateSalary = SalaryDiffers;
		_fillAbsences = AbsencesDiffer;
		string[] array2 = new string[6] { "SalaryDiffers", "SalaryText", "UpdateSalary", "AbsencesDiffer", "AbsencesText", "FillAbsences" };
		foreach (string p in array2)
		{
			OnPropertyChanged(p);
		}
		Recalculate();
	}

	private void Recalculate()
	{
		if ((object)_data == null)
		{
			return;
		}
		YearMonth? target = _target;
		if (target.HasValue)
		{
			YearMonth m = target.GetValueOrDefault();
			List<PayslipImportRow> include = Rows.Where((PayslipImportRow payslipImportRow) => payslipImportRow.Include).ToList();
			decimal earnings = _data.Entries.Where((Entry e) => e.Kind == 1).Sum((Entry e) => e.Amount) + include.Where((PayslipImportRow payslipImportRow) => payslipImportRow.Kind == EntryKind.Earning).Sum((PayslipImportRow payslipImportRow) => payslipImportRow.Amount);
			decimal deductions = _data.Entries.Where((Entry e) => e.Kind == 2).Sum((Entry e) => e.Amount) + include.Where((PayslipImportRow payslipImportRow) => payslipImportRow.Kind == EntryKind.Deduction).Sum((PayslipImportRow payslipImportRow) => payslipImportRow.Amount);
			MonthAbsences absences = (FillAbsences ? PayslipAbsences : _data.Absences);
			PayrollResult r = Payroll.Calculate(SalaryUsed, earnings, deductions, absences, Svc.Config.Preferences().Payroll, Svc.Config.ParametersForYear(m.Year).Parameters);
			AppNet = r.Net;
			Checks.Clear();
			Checks.Add(new PayslipCheckRow("INSS", Sum(PayslipLineKind.Inss), r.Inss));
			Checks.Add(new PayslipCheckRow("Vale transporte", Sum(PayslipLineKind.TransportVoucher), r.TransportVoucher));
			Checks.Add(new PayslipCheckRow("Faltas e atrasos", Sum(PayslipLineKind.AbsenceDays) + Sum(PayslipLineKind.LostDsr) + Sum(PayslipLineKind.LateHours), r.Absences));
			if (Sum(PayslipLineKind.Irrf) != 0m || r.Irrf != 0m)
			{
				Checks.Add(new PayslipCheckRow("IRRF", Sum(PayslipLineKind.Irrf), r.Irrf));
			}
		}
		decimal Sum(PayslipLineKind k)
		{
			return _payslip.Lines.Where((PayslipLine l) => l.Kind == k).Sum((PayslipLine l) => l.Deduction);
		}
	}
}
