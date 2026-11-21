using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;
using MyFinances.Core.Labor;
using MyFinances.Core.Loans;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class SettingsViewModel : ScreenViewModel
{
	private string _openingBalance = "";

	private string _firstMonth = "";

	private string _workload = "";

	private string _tvPct = "";

	private string _dependents = "";

	private string _hireDate = "";

	private string _holidays = "";

	private string _penalty = "";

	private string _lateInterest = "";

	private string _tolerance = "";

	private string _risk = "";

	private string _provision = "";

	private string _limit = "";

	private string _attachmentMb = "";

	private string _backupCount = "";

	private bool _tv;

	private bool _irrf;

	private bool _with13th;

	private bool _autoBackups;

	private bool _pinAttachments;

	private bool _hasPin;

	private string _year = "";

	private string _minWage = "";

	private string _inssSource = "";

	private string _benefitBand1 = "";

	private string _benefitBand2 = "";

	private string _benefitFixed = "";

	private string _benefitCeiling = "";

	private string _irDependent = "";

	private string _irSimplified = "";

	private string _irRedLimit = "";

	private string _irRedMax = "";

	private string _irRedEnd = "";

	private string _irRedConstant = "";

	private string _irRedFactor = "";

	private bool _inssConfirmed;

	private bool _benefitConfirmed;

	private bool _irConfirmed;

	private bool _inssTruncate = true;

	private bool _creatorMode;

	public override string Title => "Configurações";

	public string OpeningBalance
	{
		get
		{
			return _openingBalance;
		}
		set
		{
			Set(ref _openingBalance, value, "OpeningBalance");
		}
	}

	public string FirstMonth
	{
		get
		{
			return _firstMonth;
		}
		set
		{
			Set(ref _firstMonth, value, "FirstMonth");
		}
	}

	public string Workload
	{
		get
		{
			return _workload;
		}
		set
		{
			Set(ref _workload, value, "Workload");
		}
	}

	public bool TransportVoucher
	{
		get
		{
			return _tv;
		}
		set
		{
			Set(ref _tv, value, "TransportVoucher");
		}
	}

	public string TvPct
	{
		get
		{
			return _tvPct;
		}
		set
		{
			Set(ref _tvPct, value, "TvPct");
		}
	}

	public bool Irrf
	{
		get
		{
			return _irrf;
		}
		set
		{
			Set(ref _irrf, value, "Irrf");
		}
	}

	public string Dependents
	{
		get
		{
			return _dependents;
		}
		set
		{
			Set(ref _dependents, value, "Dependents");
		}
	}

	public string HireDate
	{
		get
		{
			return _hireDate;
		}
		set
		{
			Set(ref _hireDate, value, "HireDate");
		}
	}

	public string LocalHolidays
	{
		get
		{
			return _holidays;
		}
		set
		{
			Set(ref _holidays, value, "LocalHolidays");
		}
	}

	public bool ScenarioWith13th
	{
		get
		{
			return _with13th;
		}
		set
		{
			Set(ref _with13th, value, "ScenarioWith13th");
		}
	}

	public string Penalty
	{
		get
		{
			return _penalty;
		}
		set
		{
			Set(ref _penalty, value, "Penalty");
		}
	}

	public string LateInterest
	{
		get
		{
			return _lateInterest;
		}
		set
		{
			Set(ref _lateInterest, value, "LateInterest");
		}
	}

	public string Tolerance
	{
		get
		{
			return _tolerance;
		}
		set
		{
			Set(ref _tolerance, value, "Tolerance");
		}
	}

	public string Risk
	{
		get
		{
			return _risk;
		}
		set
		{
			Set(ref _risk, value, "Risk");
		}
	}

	public string Provision
	{
		get
		{
			return _provision;
		}
		set
		{
			Set(ref _provision, value, "Provision");
		}
	}

	public string CapitalLimit
	{
		get
		{
			return _limit;
		}
		set
		{
			Set(ref _limit, value, "CapitalLimit");
		}
	}

	public string AttachmentMb
	{
		get
		{
			return _attachmentMb;
		}
		set
		{
			Set(ref _attachmentMb, value, "AttachmentMb");
		}
	}

	public bool AutoBackup
	{
		get
		{
			return _autoBackups;
		}
		set
		{
			Set(ref _autoBackups, value, "AutoBackup");
		}
	}

	public string BackupCount
	{
		get
		{
			return _backupCount;
		}
		set
		{
			Set(ref _backupCount, value, "BackupCount");
		}
	}

	public bool PinForAttachments
	{
		get
		{
			return _pinAttachments;
		}
		set
		{
			Set(ref _pinAttachments, value, "PinForAttachments");
		}
	}

	public bool HasPin
	{
		get
		{
			return _hasPin;
		}
		private set
		{
			Set(ref _hasPin, value, "HasPin");
		}
	}

	public ObservableCollection<int> RegisteredYears { get; } = new ObservableCollection<int>();

	public string Year
	{
		get
		{
			return _year;
		}
		set
		{
			Set(ref _year, value, "Year");
		}
	}

	public string MinimumWage
	{
		get
		{
			return _minWage;
		}
		set
		{
			Set(ref _minWage, value, "MinimumWage");
		}
	}

	public ObservableCollection<BandVM> InssBands { get; } = new ObservableCollection<BandVM>();

	public ObservableCollection<BandVM> IrrfBands { get; } = new ObservableCollection<BandVM>();

	public string InssSource
	{
		get
		{
			return _inssSource;
		}
		set
		{
			Set(ref _inssSource, value, "InssSource");
		}
	}

	public bool InssConfirmed
	{
		get
		{
			return _inssConfirmed;
		}
		set
		{
			Set(ref _inssConfirmed, value, "InssConfirmed");
		}
	}

	public bool InssTruncate
	{
		get
		{
			return _inssTruncate;
		}
		set
		{
			Set(ref _inssTruncate, value, "InssTruncate");
		}
	}

	public string BenefitBand1
	{
		get
		{
			return _benefitBand1;
		}
		set
		{
			Set(ref _benefitBand1, value, "BenefitBand1");
		}
	}

	public string BenefitBand2
	{
		get
		{
			return _benefitBand2;
		}
		set
		{
			Set(ref _benefitBand2, value, "BenefitBand2");
		}
	}

	public string BenefitFixed2
	{
		get
		{
			return _benefitFixed;
		}
		set
		{
			Set(ref _benefitFixed, value, "BenefitFixed2");
		}
	}

	public string BenefitCeiling
	{
		get
		{
			return _benefitCeiling;
		}
		set
		{
			Set(ref _benefitCeiling, value, "BenefitCeiling");
		}
	}

	public bool BenefitConfirmed
	{
		get
		{
			return _benefitConfirmed;
		}
		set
		{
			Set(ref _benefitConfirmed, value, "BenefitConfirmed");
		}
	}

	public string IrDependent
	{
		get
		{
			return _irDependent;
		}
		set
		{
			Set(ref _irDependent, value, "IrDependent");
		}
	}

	public string IrSimplified
	{
		get
		{
			return _irSimplified;
		}
		set
		{
			Set(ref _irSimplified, value, "IrSimplified");
		}
	}

	public string IrRedLimit
	{
		get
		{
			return _irRedLimit;
		}
		set
		{
			Set(ref _irRedLimit, value, "IrRedLimit");
		}
	}

	public string IrRedMax
	{
		get
		{
			return _irRedMax;
		}
		set
		{
			Set(ref _irRedMax, value, "IrRedMax");
		}
	}

	public string IrRedEnd
	{
		get
		{
			return _irRedEnd;
		}
		set
		{
			Set(ref _irRedEnd, value, "IrRedEnd");
		}
	}

	public string IrRedConstant
	{
		get
		{
			return _irRedConstant;
		}
		set
		{
			Set(ref _irRedConstant, value, "IrRedConstant");
		}
	}

	public string IrRedFactor
	{
		get
		{
			return _irRedFactor;
		}
		set
		{
			Set(ref _irRedFactor, value, "IrRedFactor");
		}
	}

	public bool IrConfirmed
	{
		get
		{
			return _irConfirmed;
		}
		set
		{
			Set(ref _irConfirmed, value, "IrConfirmed");
		}
	}

	public string Version => BackupService.AppVersion;

	public string DataFolder => AppPaths.Root;

	public bool CreatorMode
	{
		get
		{
			return _creatorMode;
		}
		private set
		{
			if (Set(ref _creatorMode, value, "CreatorMode"))
			{
				OnPropertyChanged("CreatorModeText");
			}
		}
	}

	public string CreatorModeText
	{
		get
		{
			if (!CreatorMode)
			{
				return "não";
			}
			return "sim (tela de doação desligada)";
		}
	}

	public RelayCommand SaveCommand { get; }

	public RelayCommand SetPinCommand { get; }

	public RelayCommand RemovePinCommand { get; }

	public RelayCommand LoadYearCommand { get; }

	public RelayCommand SaveParametersCommand { get; }

	public RelayCommand RestoreParametersCommand { get; }

	public RelayCommand OpenFolderCommand { get; }

	public RelayCommand OpenLogsCommand { get; }

	public RelayCommand CreatorModeCommand { get; }

	public RelayCommand DeleteDataCommand { get; }

	public SettingsViewModel()
	{
		SaveCommand = new RelayCommand(Save);
		SetPinCommand = new RelayCommand(SetPin);
		RemovePinCommand = new RelayCommand(() =>
		{
			Preferences preferences = Svc.Config.Preferences();
			string text = Dialogs.AskText("PIN", "Digite o PIN atual para remover:", password: true);
			if (text != null)
			{
				if (!Pin.Check(text, preferences.PinHash))
				{
					Dialogs.Warn("PIN incorreto.");
				}
				else
				{
					Svc.Config.Save(preferences with
					{
						PinHash = null
					});
					Load();
				}
			}
		}, () => HasPin);
		LoadYearCommand = new RelayCommand(() =>
		{
			List<string> errors = new List<string>();
			int year = Input.Int(Year, "Ano", errors, 2000, 2100);
			Input.Check(errors);
			LoadParameters(year);
		});
		SaveParametersCommand = new RelayCommand(SaveParameters);
		RestoreParametersCommand = new RelayCommand(() =>
		{
			List<string> errors = new List<string>();
			int num = Input.Int(Year, "Ano", errors, 2000, 2100);
			Input.Check(errors);
			if (Dialogs.Confirm($"Apagar a tabela editada de {num}? Para 2026 volta a padrão de fábrica; outros anos passam a usar a do ano anterior."))
			{
				Svc.Config.RemoveParameters(num);
				Load();
			}
		});
		OpenFolderCommand = new RelayCommand(() =>
		{
			Process.Start(new ProcessStartInfo(AppPaths.Root)
			{
				UseShellExecute = true
			});
		});
		OpenLogsCommand = new RelayCommand(() =>
		{
			Process.Start(new ProcessStartInfo(AppPaths.Logs)
			{
				UseShellExecute = true
			});
		});
		CreatorModeCommand = new RelayCommand(ToggleCreatorMode);
		DeleteDataCommand = new RelayCommand(DeleteData);
	}

	public override void Load()
	{
		Preferences p = Svc.Config.Preferences();
		OpeningBalance = Money.Num(p.OpeningBalance);
		FirstMonth = (YearMonth.TryParseKey(p.FirstMonth, out var pm) ? Input.Month(pm) : "");
		Workload = p.Payroll.MonthlyHours.ToString();
		TransportVoucher = p.Payroll.TransportVoucherActive;
		TvPct = p.Payroll.TransportVoucherPct.ToString("0.##", Money.PtBr);
		Irrf = p.Payroll.IrrfActive;
		Dependents = p.Payroll.Dependents.ToString();
		DateOnly? hireDate = p.Payroll.HireDate;
		string hireDate2;
		if (hireDate.HasValue)
		{
			DateOnly d = hireDate.GetValueOrDefault();
			hireDate2 = Input.Date(d);
		}
		else
		{
			hireDate2 = "";
		}
		HireDate = hireDate2;
		LocalHolidays = string.Join(", ", p.LocalHolidays);
		ScenarioWith13th = p.DefaultScenario == ThirteenthScenario.With13th;
		Penalty = p.DefaultLateRules.PenaltyPct.ToString("0.##", Money.PtBr);
		LateInterest = p.DefaultLateRules.MonthlyLateInterestPct.ToString("0.##", Money.PtBr);
		Tolerance = p.DefaultLateRules.ToleranceDays.ToString();
		Risk = p.DefaultLateRules.DaysAtRisk.ToString();
		Provision = p.DefaultLateRules.LossProvisionPct.ToString("0.##", Money.PtBr);
		decimal? capitalLimit = p.CapitalLimit;
		string capitalLimit2;
		if (capitalLimit.HasValue)
		{
			decimal l = capitalLimit.GetValueOrDefault();
			capitalLimit2 = Money.Num(l);
		}
		else
		{
			capitalLimit2 = "";
		}
		CapitalLimit = capitalLimit2;
		AttachmentMb = p.MaxAttachmentMb.ToString();
		AutoBackup = p.AutoBackup;
		BackupCount = p.AutoBackupsKept.ToString();
		PinForAttachments = p.PinForAttachments;
		HasPin = !string.IsNullOrEmpty(p.PinHash);
		CreatorMode = Bootstrap.CreatorMode;
		RegisteredYears.Clear();
		foreach (int a in Svc.Config.AllParameters().Keys.OrderBy((int k) => k))
		{
			RegisteredYears.Add(a);
		}
		LoadParameters(DateTime.Today.Year);
	}

	private void Save()
	{
		List<string> e = new List<string>();
		Preferences p = Svc.Config.Preferences();
		string[] holidays = (from x in LocalHolidays.Split(new char[3] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
			select x.Trim()).ToArray();
		string[] array = holidays;
		foreach (string f in array)
		{
			if (!DateOnly.TryParseExact(f + "/2024", "dd/MM/yyyy", out var _))
			{
				e.Add("Feriado \"" + f + "\" inválido: use dd/mm separados por vírgula.");
			}
		}
		Preferences newValue = p with
		{
			OpeningBalance = Input.Dec(OpeningBalance, "Saldo inicial", e, required: false, allowNegative: true),
			FirstMonth = Input.Month(FirstMonth, "Primeiro mês", e).Key,
			DefaultScenario = ((!ScenarioWith13th) ? ThirteenthScenario.Without13th : ThirteenthScenario.With13th),
			LocalHolidays = holidays,
			Payroll = new PayrollConfig
			{
				MonthlyHours = Input.Int(Workload, "Jornada mensal", e, 1, 300),
				TransportVoucherActive = TransportVoucher,
				TransportVoucherPct = Input.Dec(TvPct, "VT (%)", e, required: false),
				IrrfActive = Irrf,
				Dependents = Input.Int(Dependents, "Dependentes", e, 0, 20),
				HireDate = Input.OptionalDate(HireDate, "Admissão", e)
			},
			DefaultLateRules = new LateRules
			{
				PenaltyPct = Input.Dec(Penalty, "Multa", e, required: false),
				MonthlyLateInterestPct = Input.Dec(LateInterest, "Mora", e, required: false),
				ToleranceDays = Input.Int(Tolerance, "Tolerância", e, 0, 60),
				DaysAtRisk = Input.Int(Risk, "Dias para inadimplência", e, 1, 3650),
				LossProvisionPct = Input.Dec(Provision, "Provisão", e, required: false)
			},
			CapitalLimit = (string.IsNullOrWhiteSpace(CapitalLimit) ? ((decimal?)null) : new decimal?(Input.Dec(CapitalLimit, "Limite de capital", e))),
			MaxAttachmentMb = Input.Int(AttachmentMb, "Tamanho máximo do anexo", e, 1, 50),
			AutoBackup = AutoBackup,
			AutoBackupsKept = Input.Int(BackupCount, "Backups guardados", e, 1, 60),
			PinForAttachments = PinForAttachments
		};
		if (newValue.Payroll.TransportVoucherPct > 6m)
		{
			e.Add("VT: o desconto máximo legal é 6% do salário básico.");
		}
		Input.Check(e);
		Svc.Config.Save(newValue);
		Dialogs.Info("Configurações salvas. Os contratos já existentes mantêm as regras de atraso com que foram criados.");
	}

	private void SetPin()
	{
		Preferences p = Svc.Config.Preferences();
		if (!string.IsNullOrEmpty(p.PinHash))
		{
			string current = Dialogs.AskText("PIN", "Digite o PIN atual:", password: true);
			if (current == null)
			{
				return;
			}
			if (!Pin.Check(current, p.PinHash))
			{
				Dialogs.Warn("PIN incorreto.");
				return;
			}
		}
		string n1 = Dialogs.AskText("Novo PIN", "Novo PIN (4 a 12 dígitos):", password: true);
		if (n1 != null)
		{
			int length = n1.Length;
			bool flag = ((length < 4 || length > 12) ? true : false);
			if (flag || !n1.All(char.IsDigit))
			{
				Dialogs.Warn("O PIN precisa ter de 4 a 12 dígitos.");
				return;
			}
			if (Dialogs.AskText("Novo PIN", "Repita o PIN:", password: true) != n1)
			{
				Dialogs.Warn("Os PINs não conferem.");
				return;
			}
			Svc.Config.Save(p with
			{
				PinHash = Pin.GenerateHash(n1)
			});
			Dialogs.Info("PIN definido. Ele será pedido ao abrir o MyFinances" + (p.PinForAttachments ? " e ao abrir documentos de clientes." : "."));
			Load();
		}
	}

	private void LoadParameters(int year)
	{
		(LegalParameters Parameters, bool Estimated) tuple = Svc.Config.ParametersForYear(year);
		LegalParameters pars = tuple.Parameters;
		bool est = tuple.Estimated;
		Year = year.ToString();
		MinimumWage = Money.Num(pars.MinimumWage);
		InssBands.Clear();
		foreach (InssBand f in pars.Inss.Bands)
		{
			InssBands.Add(new BandVM
			{
				Until = Money.Num(f.Until),
				Pct = f.RatePct.ToString("0.##", Money.PtBr)
			});
		}
		InssSource = (est ? $"(não há tabela de {year}: mostrando a de {pars.Year})" : pars.Inss.Source);
		InssConfirmed = pars.Inss.Confirmed && !est;
		InssTruncate = pars.Inss.Rounding == InssRounding.TruncatePerBand;
		BenefitBand1 = Money.Num(pars.UnemploymentBenefit.Band1);
		BenefitBand2 = Money.Num(pars.UnemploymentBenefit.Band2);
		BenefitFixed2 = Money.Num(pars.UnemploymentBenefit.Fixed2);
		BenefitCeiling = Money.Num(pars.UnemploymentBenefit.Ceiling);
		BenefitConfirmed = pars.UnemploymentBenefit.Confirmed && !est;
		IrrfBands.Clear();
		foreach (IrrfBand f2 in pars.Irrf.Bands)
		{
			IrrfBands.Add(new BandVM
			{
				Until = ((f2.Until == decimal.MaxValue) ? "acima" : Money.Num(f2.Until)),
				Pct = f2.RatePct.ToString("0.##", Money.PtBr),
				Deduct = Money.Num(f2.DeductibleAmount)
			});
		}
		IrDependent = Money.Num(pars.Irrf.DependentDeduction);
		IrSimplified = Money.Num(pars.Irrf.SimplifiedDeduction);
		IrRedLimit = Money.Num(pars.Irrf.RedExemptLimit);
		IrRedMax = Money.Num(pars.Irrf.RedMax);
		IrRedEnd = Money.Num(pars.Irrf.RedUpperLimit);
		IrRedConstant = Money.Num(pars.Irrf.RedConstant);
		IrRedFactor = pars.Irrf.RedFactor.ToString("0.######", Money.PtBr);
		IrConfirmed = pars.Irrf.Confirmed && !est;
	}

	private void SaveParameters()
	{
		List<string> e = new List<string>();
		int year = Input.Int(Year, "Ano", e, 2000, 2100);
		List<InssBand> inssBands = InssBands.Select((BandVM f, int num) => new InssBand(Input.Dec(f.Until, $"INSS faixa {num + 1}: até", e), Input.Dec(f.Pct, $"INSS faixa {num + 1}: %", e, required: false))).ToList();
		List<IrrfBand> irBands = IrrfBands.Select((BandVM f, int num) => new IrrfBand(f.Until.Trim().Equals("acima", StringComparison.OrdinalIgnoreCase) ? decimal.MaxValue : Input.Dec(f.Until, $"IRRF faixa {num + 1}: até", e), Input.Dec(f.Pct, $"IRRF faixa {num + 1}: %", e, required: false), Input.Dec(f.Deduct, $"IRRF faixa {num + 1}: parcela a deduzir", e, required: false))).ToList();
		for (int i = 1; i < inssBands.Count; i++)
		{
			if (inssBands[i].Until <= inssBands[i - 1].Until)
			{
				e.Add("INSS: os limites das faixas precisam ser crescentes.");
			}
		}
		if (irBands.Count != 0)
		{
			if (!(irBands[irBands.Count - 1].Until != decimal.MaxValue))
			{
				goto IL_00f0;
			}
		}
		e.Add("IRRF: a última faixa precisa ser \"acima\".");
		goto IL_00f0;
		IL_00f0:
		LegalParameters baseP = Svc.Config.ParametersForYear(year).Parameters;
		LegalParameters p = baseP with
		{
			Year = year,
			MinimumWage = Input.Dec(MinimumWage, "Salário mínimo", e),
			MinimumWageSource = "Editado em Configurações",
			Inss = new InssTable(year, inssBands, string.IsNullOrWhiteSpace(InssSource) ? "Editado em Configurações" : InssSource, InssConfirmed, (!InssTruncate) ? InssRounding.RoundTotal : InssRounding.TruncatePerBand),
			UnemploymentBenefit = new UnemploymentBenefitTable(year, Input.Dec(BenefitBand1, "Seguro faixa 1", e), Input.Dec(BenefitBand2, "Seguro faixa 2", e), Input.Dec(BenefitFixed2, "Seguro valor fixo", e), Input.Dec(BenefitCeiling, "Seguro teto", e), "Editado em Configurações", BenefitConfirmed),
			Irrf = baseP.Irrf with
			{
				Year = year,
				Bands = irBands,
				DependentDeduction = Input.Dec(IrDependent, "IRRF dependente", e, required: false),
				SimplifiedDeduction = Input.Dec(IrSimplified, "IRRF simplificado", e, required: false),
				RedExemptLimit = Input.Dec(IrRedLimit, "Redução: limite de isenção", e, required: false),
				RedMax = Input.Dec(IrRedMax, "Redução máxima", e, required: false),
				RedUpperLimit = Input.Dec(IrRedEnd, "Redução: limite final", e, required: false),
				RedConstant = Input.Dec(IrRedConstant, "Redução: constante", e, required: false),
				RedFactor = Input.Dec(IrRedFactor, "Redução: fator", e, required: false),
				Confirmed = IrConfirmed
			}
		};
		Input.Check(e);
		if (Dialogs.Confirm($"Salvar as tabelas de {year}? Os cálculos de {year} passam a usar estes valores."))
		{
			Svc.Config.SaveParameters(p);
			Load();
			LoadParameters(year);
		}
	}

	private void ToggleCreatorMode()
	{
		if (CreatorMode)
		{
			if (!Dialogs.Confirm("Desativar o modo criador? A tela de doação volta a aparecer de vez em quando."))
			{
				return;
			}
			Bootstrap.SetCreatorMode(active: false);
		}
		else
		{
			string r = Dialogs.AskText("Modo criador", "O modo criador só esconde a tela de doação NESTE usuário do Windows (cria o arquivo creator-mode.txt na pasta de dados). Não muda nenhuma função. Para confirmar, digite: SOU O CRIADOR");
			if (r == null)
			{
				return;
			}
			if (!r.Trim().Equals("SOU O CRIADOR", StringComparison.OrdinalIgnoreCase))
			{
				Dialogs.Warn("Texto não confere. Nada foi alterado.");
				return;
			}
			Bootstrap.SetCreatorMode(active: true);
		}
		CreatorMode = Bootstrap.CreatorMode;
	}

	private void DeleteData()
	{
		if (Dialogs.Confirm("Isso APAGA TODOS os dados do MyFinances deste usuário: banco, anexos de clientes, chave de proteção, backups automáticos e logs.\n\nRecomendado: faça antes um backup portátil (tela Backup).\n\nQuer continuar?", "Apagar dados locais"))
		{
			if (Dialogs.AskText("Apagar dados locais", "Para confirmar, digite APAGAR:")?.Trim() != "APAGAR")
			{
				Dialogs.Info("Nada foi apagado.");
				return;
			}
			Bootstrap.DeleteLocalData();
			Dialogs.Info("Dados apagados. O MyFinances será fechado. Se abrir de novo, começa do zero.");
			Application.Current.Shutdown();
		}
	}
}
