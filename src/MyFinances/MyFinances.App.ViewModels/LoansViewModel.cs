using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Loans;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class LoansViewModel : ScreenViewModel
{
	private List<LoanSummary> _all = new List<LoanSummary>();

	private LoanSummary? _selected;

	private LoanDetail? _detail;

	private string _filter = "Todos";

	private string _search = "";

	private bool _draftMode;

	private bool _showHistory;

	private InstallmentRow? _installmentSel;

	private PaymentRow? _paymentSel;

	private ClientData? _draftClient;

	private string _draftNotes = "";

	private string _offerText = "";

	private string _draftError = "";

	private string _limitWarning = "";

	private string _previewTotals = "";

	private string _pmtDate = "";

	private string _pmtTime = "";

	private string _pmtAmount = "";

	private string _pmtMethod = "Pix";

	private string _pmtNote = "";

	private bool _capInterest;

	private bool _capCharges;

	private bool _chargeExtensionInterest;

	private bool _capChargesOnExtension = true;

	private bool _shortenTerm;

	private string _reason = "";

	private string _extensionPeriods = "1";

	private string _amortAmount = "";

	private string _outstanding = "";

	public override string Title => "Empréstimos";

	public ObservableCollection<LoanSummary> ItemList { get; } = new ObservableCollection<LoanSummary>();

	public ObservableCollection<InstallmentRow> Installments { get; } = new ObservableCollection<InstallmentRow>();

	public ObservableCollection<PaymentRow> Payments { get; } = new ObservableCollection<PaymentRow>();

	public ObservableCollection<VersionRow> Versions { get; } = new ObservableCollection<VersionRow>();

	public ObservableCollection<PreviewRow> Preview { get; } = new ObservableCollection<PreviewRow>();

	public IReadOnlyList<string> Filters { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[6] { "Todos", "Ativos", "Em atraso", "Inadimplentes", "Quitados", "Cancelados/baixados" });

	public string Filter
	{
		get
		{
			return _filter;
		}
		set
		{
			if (Set(ref _filter, value, "Filter"))
			{
				ApplyFilter();
			}
		}
	}

	public string Search
	{
		get
		{
			return _search;
		}
		set
		{
			if (Set(ref _search, value, "Search"))
			{
				ApplyFilter();
			}
		}
	}

	public bool DraftMode
	{
		get
		{
			return _draftMode;
		}
		set
		{
			Set(ref _draftMode, value, "DraftMode");
			OnPropertyChanged("DetailMode");
		}
	}

	public bool DetailMode
	{
		get
		{
			if (!_draftMode)
			{
				return (object)_detail != null;
			}
			return false;
		}
	}

	public bool ShowHistory
	{
		get
		{
			return _showHistory;
		}
		set
		{
			if (Set(ref _showHistory, value, "ShowHistory"))
			{
				ShowDetail();
			}
		}
	}

	public LoanSummary? Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (Set(ref _selected, value, "Selected") && (object)value != null)
			{
				DraftMode = false;
				OpenDetail(value.Id);
			}
		}
	}

	public LoanDetail? Detail
	{
		get
		{
			return _detail;
		}
		private set
		{
			Set(ref _detail, value, "Detail");
			OnPropertyChanged("DetailMode");
			OnPropertyChanged("Header");
			OnPropertyChanged("TermsText");
			OnPropertyChanged("Position");
			OnPropertyChanged("StatusText");
		}
	}

	public LoanPosition? Position => _detail?.Position;

	public string StatusText
	{
		get
		{
			if ((object)_detail != null)
			{
				return LoanEngine.Name(_detail.Position.Status);
			}
			return "";
		}
	}

	public string Header
	{
		get
		{
			if ((object)_detail != null)
			{
				return $"Empréstimo nº {_detail.Entity.Code} — {_detail.Client}";
			}
			return "";
		}
	}

	public string TermsText
	{
		get
		{
			if ((object)_detail == null)
			{
				return "";
			}
			IReadOnlyList<VersionCalc> versions = _detail.Versions;
			VersionCalc v = versions[versions.Count - 1];
			LoanTerms c = v.Terms;
			string s = $"Versão {v.Number} ({VersionName(v.Kind)}) desde {v.Start:dd/MM/yyyy} · {Money.Fmt(c.Principal)} · {Money.Pct(c.InterestRatePct)} {Schedule.PeriodName(c.Frequency)} · {Schedule.MethodName(c.Method)} · multa {Money.Pct(c.LateRules.PenaltyPct)} + mora {Money.Pct(c.LateRules.MonthlyLateInterestPct)} a.m. · tolerância {c.LateRules.ToleranceDays} dia(s)";
			if (_detail.Entity.State != 0)
			{
				s += $"\n{((_detail.Entity.State == 1) ? "CANCELADO" : "BAIXADO COMO PERDA")} em {_detail.Entity.StateDate:dd/MM/yyyy}: {_detail.Entity.StateReason}";
			}
			return s;
		}
	}

	public InstallmentRow? SelectedInstallment
	{
		get
		{
			return _installmentSel;
		}
		set
		{
			Set(ref _installmentSel, value, "SelectedInstallment");
			OnPropertyChanged("SelectedBreakdown");
		}
	}

	public PaymentRow? SelectedPayment
	{
		get
		{
			return _paymentSel;
		}
		set
		{
			Set(ref _paymentSel, value, "SelectedPayment");
			OnPropertyChanged("SelectedBreakdown");
		}
	}

	public string SelectedBreakdown => _paymentSel?.Breakdown ?? _installmentSel?.Breakdown ?? "Selecione uma parcela ou um pagamento para ver a memória de cálculo.";

	public LoanTermsForm Draft { get; } = new LoanTermsForm();

	public ObservableCollection<ClientData> Clients { get; } = new ObservableCollection<ClientData>();

	public ClientData? NewLoanClient
	{
		get
		{
			return _draftClient;
		}
		set
		{
			Set(ref _draftClient, value, "NewLoanClient");
		}
	}

	public string DraftNotes
	{
		get
		{
			return _draftNotes;
		}
		set
		{
			Set(ref _draftNotes, value, "DraftNotes");
		}
	}

	public string OfferText
	{
		get
		{
			return _offerText;
		}
		set
		{
			Set(ref _offerText, value, "OfferText");
		}
	}

	public string DraftError
	{
		get
		{
			return _draftError;
		}
		set
		{
			Set(ref _draftError, value, "DraftError");
		}
	}

	public string LimitWarning
	{
		get
		{
			return _limitWarning;
		}
		set
		{
			Set(ref _limitWarning, value, "LimitWarning");
		}
	}

	public string PreviewTotals
	{
		get
		{
			return _previewTotals;
		}
		set
		{
			Set(ref _previewTotals, value, "PreviewTotals");
		}
	}

	public string PmtDate
	{
		get
		{
			return _pmtDate;
		}
		set
		{
			Set(ref _pmtDate, value, "PmtDate");
		}
	}

	public string PmtTime
	{
		get
		{
			return _pmtTime;
		}
		set
		{
			Set(ref _pmtTime, value, "PmtTime");
		}
	}

	public string PmtAmount
	{
		get
		{
			return _pmtAmount;
		}
		set
		{
			Set(ref _pmtAmount, value, "PmtAmount");
		}
	}

	public string PmtMethod
	{
		get
		{
			return _pmtMethod;
		}
		set
		{
			Set(ref _pmtMethod, value, "PmtMethod");
		}
	}

	public string PmtNote
	{
		get
		{
			return _pmtNote;
		}
		set
		{
			Set(ref _pmtNote, value, "PmtNote");
		}
	}

	public IReadOnlyList<string> PaymentMethods { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[6] { "Pix", "Dinheiro", "Transferência", "Boleto", "Cartão", "Outro" });

	public LoanTermsForm Reneg { get; } = new LoanTermsForm();

	public bool CapitalizeInterest
	{
		get
		{
			return _capInterest;
		}
		set
		{
			Set(ref _capInterest, value, "CapitalizeInterest");
		}
	}

	public bool CapitalizeCharges
	{
		get
		{
			return _capCharges;
		}
		set
		{
			Set(ref _capCharges, value, "CapitalizeCharges");
		}
	}

	public bool ChargeExtensionInterest
	{
		get
		{
			return _chargeExtensionInterest;
		}
		set
		{
			Set(ref _chargeExtensionInterest, value, "ChargeExtensionInterest");
		}
	}

	public bool CapitalizeChargesOnExtension
	{
		get
		{
			return _capChargesOnExtension;
		}
		set
		{
			Set(ref _capChargesOnExtension, value, "CapitalizeChargesOnExtension");
		}
	}

	public bool ShortenTerm
	{
		get
		{
			return _shortenTerm;
		}
		set
		{
			Set(ref _shortenTerm, value, "ShortenTerm");
		}
	}

	public string Reason
	{
		get
		{
			return _reason;
		}
		set
		{
			Set(ref _reason, value, "Reason");
		}
	}

	public string ExtensionPeriods
	{
		get
		{
			return _extensionPeriods;
		}
		set
		{
			Set(ref _extensionPeriods, value, "ExtensionPeriods");
		}
	}

	public string AmortizationAmount
	{
		get
		{
			return _amortAmount;
		}
		set
		{
			Set(ref _amortAmount, value, "AmortizationAmount");
		}
	}

	public string OutstandingText
	{
		get
		{
			return _outstanding;
		}
		set
		{
			Set(ref _outstanding, value, "OutstandingText");
		}
	}

	public RelayCommand NewCommand { get; }

	public RelayCommand SimulateCommand { get; }

	public RelayCommand CreateLoanCommand { get; }

	public RelayCommand CopyOfferCommand { get; }

	public RelayCommand CancelDraftCommand { get; }

	public RelayCommand PayCommand { get; }

	public RelayCommand PayInstallmentCommand { get; }

	public RelayCommand ReverseCommand { get; }

	public RelayCommand RenegotiateCommand { get; }

	public RelayCommand ExtendCommand { get; }

	public RelayCommand AmortizeCommand { get; }

	public RelayCommand UndoVersionCommand { get; }

	public RelayCommand CancelLoanCommand { get; }

	public RelayCommand WriteOffCommand { get; }

	public RelayCommand ReopenCommand { get; }

	public RelayCommand CopyCollectionCommand { get; }

	public LoansViewModel()
	{
		NewCommand = new RelayCommand(OpenNew);
		SimulateCommand = new RelayCommand(() =>
		{
			Simulate();
		});
		CreateLoanCommand = new RelayCommand(CreateLoan);
		CopyOfferCommand = new RelayCommand(() =>
		{
			if (Simulate())
			{
				Clipboard.SetText(OfferText);
				Dialogs.Info("Proposta copiada. Cole no WhatsApp.");
			}
		});
		CancelDraftCommand = new RelayCommand(() =>
		{
			DraftMode = false;
		});
		PayCommand = new RelayCommand(Pay, () => DetailMode);
		PayInstallmentCommand = new RelayCommand(() =>
		{
			InstallmentRow selectedInstallment = SelectedInstallment;
			if ((object)selectedInstallment != null)
			{
				PmtAmount = Money.Num(selectedInstallment.Outstanding);
			}
		}, () =>
		{
			InstallmentRow selectedInstallment = SelectedInstallment;
			return (object)selectedInstallment != null && selectedInstallment.Outstanding > 0m;
		});
		ReverseCommand = new RelayCommand(Reverse, () =>
		{
			PaymentRow selectedPayment = SelectedPayment;
			return (object)selectedPayment != null && !selectedPayment.Reversed;
		});
		RenegotiateCommand = new RelayCommand(Renegotiate, () => DetailMode);
		ExtendCommand = new RelayCommand(Extend, () => DetailMode);
		AmortizeCommand = new RelayCommand(Amortize, () => DetailMode);
		UndoVersionCommand = new RelayCommand(() =>
		{
			if ((object)_detail != null && Dialogs.Confirm("Desfazer a última versão (renegociação/prorrogação/amortização)? A versão anterior volta a valer."))
			{
				Svc.Loans.UndoLastVersion(_detail.Entity.Id);
				Reload();
			}
		}, () =>
		{
			LoanDetail detail = _detail;
			if ((object)detail != null)
			{
				IReadOnlyList<VersionCalc> versions = detail.Versions;
				if (versions != null)
				{
					return versions.Count > 1;
				}
			}
			return false;
		});
		CancelLoanCommand = new RelayCommand(() =>
		{
			ChangeState(SpecialState.Canceled, "Cancelar o contrato? Use para contrato desfeito ou registrado por engano. O histórico fica guardado e dá para reabrir.");
		}, () =>
		{
			LoanDetail detail = _detail;
			if ((object)detail != null)
			{
				Loan entity = detail.Entity;
				if (entity != null)
				{
					return entity.State == 0;
				}
			}
			return false;
		});
		WriteOffCommand = new RelayCommand(() =>
		{
			ChangeState(SpecialState.WrittenOff, "Baixar como perda? O principal em aberto vira prejuízo no painel. Pagamentos depois disso contam como recuperação. Dá para reabrir.");
		}, () =>
		{
			LoanDetail detail = _detail;
			if ((object)detail != null)
			{
				Loan entity = detail.Entity;
				if (entity != null)
				{
					return entity.State == 0;
				}
			}
			return false;
		});
		ReopenCommand = new RelayCommand(() =>
		{
			ChangeState(SpecialState.None, "Reabrir o contrato (desfazer cancelamento/baixa)?");
		}, () =>
		{
			LoanDetail detail = _detail;
			if ((object)detail != null)
			{
				Loan entity = detail.Entity;
				if (entity != null)
				{
					return entity.State != 0;
				}
			}
			return false;
		});
		CopyCollectionCommand = new RelayCommand(CopyCollection, () => DetailMode);
	}

	public override void Load()
	{
		_all = Svc.Loans.ListAll();
		ApplyFilter();
		Clients.Clear();
		foreach (ClientData c in Svc.Clients.ListAll())
		{
			Clients.Add(c);
		}
		if ((object)_detail != null)
		{
			OpenDetail(_detail.Entity.Id);
		}
	}

	private void ApplyFilter()
	{
		string b = Search.Trim();
		ItemList.Clear();
		foreach (LoanSummary e in from loanSummary in _all.Where((LoanSummary loanSummary) =>
			{
				switch (Filter)
				{
				case "Ativos":
				{
					LoanStatus status = loanSummary.Status;
					return (uint)status <= 2u;
				}
				case "Em atraso":
					return loanSummary.Status == LoanStatus.Late;
				case "Inadimplentes":
					return loanSummary.Status == LoanStatus.AtRisk;
				case "Quitados":
					return loanSummary.Status == LoanStatus.PaidOff;
				case "Cancelados/baixados":
				{
					LoanStatus status = loanSummary.Status;
					return (uint)(status - 4) <= 1u;
				}
				default:
					return true;
				}
			})
			where b.Length == 0 || loanSummary.Client.Contains(b, StringComparison.CurrentCultureIgnoreCase) || loanSummary.Code.ToString() == b
			select loanSummary)
		{
			ItemList.Add(e);
		}
	}

	private void Reload()
	{
		Guid? id = _detail?.Entity.Id;
		_all = Svc.Loans.ListAll();
		ApplyFilter();
		if (id.HasValue)
		{
			Guid i = id.GetValueOrDefault();
			OpenDetail(i);
		}
	}

	private void OpenDetail(Guid id)
	{
		Detail = Svc.Loans.Get(id);
		PmtDate = Input.Date(DateOnly.FromDateTime(DateTime.Today));
		PmtTime = DateTime.Now.ToString("HH:mm");
		PmtAmount = "";
		PmtNote = "";
		Reason = "";
		IReadOnlyList<VersionCalc> versions = _detail.Versions;
		LoanTerms c = versions[versions.Count - 1].Terms;
		Reneg.Rate = c.InterestRatePct.ToString("0.####", Money.PtBr);
		Reneg.Frequency = Reneg.Frequencies.First((Option<Frequency> p) => p.Amount == c.Frequency);
		Reneg.Method = Reneg.Methods.First((Option<AmortizationMethod> m) => m.Amount == c.Method);
		Reneg.ContractDate = Input.Date(DateOnly.FromDateTime(DateTime.Today));
		Reneg.FirstDueDate = Input.Date(DateOnly.FromDateTime(DateTime.Today).AddMonths(1));
		(decimal Principal, decimal OverdueInterest, decimal Charges, IReadOnlyList<InstallmentState> Open) tuple = LoanEngine.OutstandingOnDate(_detail.Calc, DateOnly.FromDateTime(DateTime.Today));
		decimal pr = tuple.Principal;
		decimal ju = tuple.OverdueInterest;
		decimal en = tuple.Charges;
		OutstandingText = $"Hoje em aberto: principal {Money.Fmt(pr)} · juros/tarifas vencidos {Money.Fmt(ju)} · multa e mora {Money.Fmt(en)}";
		ShowDetail();
	}

	private void ShowDetail()
	{
		Installments.Clear();
		Payments.Clear();
		Versions.Clear();
		if ((object)_detail == null)
		{
			return;
		}
		LoanPosition pos = _detail.Position;
		DateOnly today = pos.Reference;
		foreach (InstallmentState p in pos.Installments.Where((InstallmentState installmentState) => ShowHistory || installmentState.Version == pos.CurrentVersion))
		{
			Installments.Add(new InstallmentRow(p.Version, p.Number, p.DueDate, p.Principal, p.Interest, p.Fee, p.Total, p.Paid, p.OutstandingCharges, p.OutstandingTotal, p.DaysLate, p.Status(today), p.Breakdown));
		}
		Dictionary<Guid, PaymentAllocation> alloc = pos.Allocations.ToDictionary((PaymentAllocation paymentAllocation) => paymentAllocation.PaymentId);
		foreach (Payment p2 in _detail.Payments.OrderByDescending((Payment payment) => payment.Timestamp))
		{
			ObservableCollection<PaymentRow> payments = Payments;
			Guid id = p2.Id;
			DateTime timestamp = p2.Timestamp;
			decimal amount = p2.Amount;
			string paymentMethod = p2.PaymentMethod;
			bool reversed = p2.Reversed;
			string text;
			if (p2.Reversed)
			{
				text = $"Estornado em {p2.ReversedAt:dd/MM/yyyy}";
			}
			else
			{
				text = (p2.ExtraAmortization ? "Amortização extra" : "Válido");
			}
			ObservableCollection<PaymentRow> observableCollection = payments;
			Guid id2 = id;
			DateTime timestamp2 = timestamp;
			decimal amount2 = amount;
			string paymentMethod2 = paymentMethod;
			bool reversed2 = reversed;
			string status = text;
			string breakdown;
			if (p2.Reversed)
			{
				breakdown = "Estornado: " + p2.ReversalReason;
			}
			else
			{
				breakdown = (alloc.TryGetValue(p2.Id, out var a) ? a.Breakdown : "");
			}
			observableCollection.Add(new PaymentRow(id2, timestamp2, amount2, paymentMethod2, reversed2, status, breakdown, p2.Note));
		}
		foreach (VersionCalc v in _detail.Versions)
		{
			Versions.Add(new VersionRow(v.Number, v.Start, VersionName(v.Kind), $"{Money.Fmt(v.Terms.Principal)} em {v.Installments.Count}x · {Money.Pct(v.Terms.InterestRatePct)} {Schedule.PeriodName(v.Terms.Frequency)} · {Schedule.MethodName(v.Terms.Method)}", v.Reason, v.DiscountGranted));
		}
		OnPropertyChanged("SelectedBreakdown");
	}

	public static string VersionName(VersionKind t)
	{
		return t switch
		{
			VersionKind.Original => "original", 
			VersionKind.Renegotiation => "renegociação", 
			VersionKind.Extension => "prorrogação", 
			VersionKind.ExtraAmortization => "amortização extraordinária", 
			_ => t.ToString(), 
		};
	}

	private void OpenNew()
	{
		Clients.Clear();
		foreach (ClientData c in Svc.Clients.ListAll())
		{
			Clients.Add(c);
		}
		if (Clients.Count == 0)
		{
			Dialogs.Warn("Cadastre um cliente antes (tela Clientes).");
			return;
		}
		if (NewLoanClient == null)
		{
			ClientData clientData = (NewLoanClient = Clients[0]);
		}
		Detail = null;
		Selected = null;
		Preview.Clear();
		OfferText = "";
		DraftError = "";
		LimitWarning = "";
		PreviewTotals = "";
		DraftMode = true;
	}

	private bool Simulate()
	{
		Preview.Clear();
		OfferText = "";
		DraftError = "";
		LimitWarning = "";
		PreviewTotals = "";
		List<string> errors = new List<string>();
		LoanTerms c = Draft.Read(errors);
		errors.AddRange(from e in Schedule.Validate(c)
			where !errors.Contains(e)
			select e);
		if (errors.Count > 0)
		{
			DraftError = string.Join("\n", errors);
			return false;
		}
		IReadOnlyList<ScheduledInstallment> p = Schedule.Generate(c);
		foreach (ScheduledInstallment x in p)
		{
			Preview.Add(new PreviewRow(x.Number, x.DueDate, x.Principal, x.Interest, x.Fee, x.Total, x.BalanceAfter, x.Breakdown));
		}
		PreviewTotals = $"Total a receber {Money.Fmt(p.Sum((ScheduledInstallment scheduledInstallment) => scheduledInstallment.Total))} · juros {Money.Fmt(p.Sum((ScheduledInstallment scheduledInstallment) => scheduledInstallment.Interest))} · tarifa {Money.Fmt(p.Sum((ScheduledInstallment scheduledInstallment) => scheduledInstallment.Fee))}";
		ClientData? newLoanClient = NewLoanClient;
		OfferText = Offer.Text((newLoanClient != null) ? newLoanClient.FullName.Split(' ')[0] : null, c, p);
		decimal? lim = Svc.Config.Preferences().CapitalLimit;
		if (lim.HasValue)
		{
			decimal l = lim.GetValueOrDefault();
			decimal outstanding = _all.Where((LoanSummary e) =>
			{
				LoanStatus status = e.Status;
				return (uint)status <= 2u;
			}).Sum((LoanSummary e) => e.OutstandingPrincipal);
			if (outstanding + c.Principal > l)
			{
				LimitWarning = $"Atenção: com este contrato o principal em aberto ({Money.Fmt(outstanding + c.Principal)}) passa do seu limite de capital ({Money.Fmt(l)}).";
			}
		}
		return true;
	}

	private void CreateLoan()
	{
		if (NewLoanClient == null)
		{
			DraftError = "Escolha o cliente.";
		}
		else
		{
			if (!Simulate())
			{
				return;
			}
			LoanTerms c = Draft.Read(new List<string>());
			string msg = $"Registrar empréstimo de {Money.Fmt(c.Principal)} para {NewLoanClient.FullName} em {Preview.Count} parcela(s)?\n\nAs condições ficam congeladas no contrato.";
			if (!string.IsNullOrEmpty(LimitWarning))
			{
				msg = msg + "\n\n" + LimitWarning;
			}
			if (Dialogs.Confirm(msg))
			{
				Loan e = Svc.Loans.Create(NewLoanClient.Id, c, DraftNotes);
				DraftMode = false;
				_all = Svc.Loans.ListAll();
				ApplyFilter();
				Selected = ItemList.FirstOrDefault((LoanSummary x) => x.Id == e.Id);
			}
		}
	}

	private void Pay()
	{
		if ((object)_detail != null)
		{
			List<string> errors = new List<string>();
			DateTime dt = Input.Timestamp(PmtDate, PmtTime, "Data do pagamento", errors);
			decimal v = Input.Dec(PmtAmount, "Valor", errors);
			Input.Check(errors);
			LoanPosition before = LoanEngine.Calculate(_detail.Calc, DateOnly.FromDateTime(dt));
			if ((!(v > before.TotalOwed) || !(before.TotalOwed > 0m) || Dialogs.Confirm("O valor passa do saldo devedor total na data (" + Money.Fmt(before.TotalOwed) + "). A diferença fica registrada como excedente a devolver. Continuar?")) && Dialogs.Confirm($"Registrar pagamento de {Money.Fmt(v)} em {dt:dd/MM/yyyy HH:mm}?\nAbatimento: multa → mora → juros → tarifa → principal, da parcela mais antiga."))
			{
				Svc.Loans.RecordPayment(_detail.Entity.Id, dt, v, PmtMethod, PmtNote);
				Reload();
			}
		}
	}

	private void Reverse()
	{
		if ((object)SelectedPayment != null)
		{
			string reason = Dialogs.AskText("Estornar pagamento", $"Motivo do estorno do pagamento de {Money.Fmt(SelectedPayment.Amount)} ({SelectedPayment.Timestamp:dd/MM/yyyy}):");
			if (reason != null)
			{
				Svc.Loans.Reverse(SelectedPayment.Id, reason);
				Reload();
			}
		}
	}

	private void Renegotiate()
	{
		if ((object)_detail != null)
		{
			List<string> errors = new List<string>();
			LoanTerms c = Reneg.Read(errors, requireCapital: false);
			Input.Check(errors);
			DateTime date = DateTime.Now;
			if (Dialogs.Confirm($"Renegociar a partir de agora ({date:dd/MM/yyyy HH:mm})?\n{OutstandingText}\n\nNovo principal = principal em aberto{(CapitalizeInterest ? " + juros vencidos" : "")}{(CapitalizeCharges ? " + multa/mora" : "")}. " + "O que não for incorporado fica registrado como desconto concedido. A versão atual fica guardada no histórico."))
			{
				Svc.Loans.Renegotiate(_detail.Entity.Id, date, c, CapitalizeInterest, CapitalizeCharges, Reason);
				Reload();
			}
		}
	}

	private void Extend()
	{
		if ((object)_detail != null)
		{
			List<string> errors = new List<string>();
			int n = Input.Int(ExtensionPeriods, "Períodos", errors, 1, 60);
			Input.Check(errors);
			if (Dialogs.Confirm($"Empurrar as parcelas em aberto {n} período(s)?" + (ChargeExtensionInterest ? " Serão cobrados juros do período prorrogado." : " Sem juros extras.")))
			{
				Svc.Loans.Extend(_detail.Entity.Id, DateTime.Now, n, ChargeExtensionInterest, CapitalizeChargesOnExtension, Reason);
				Reload();
			}
		}
	}

	private void Amortize()
	{
		if ((object)_detail != null)
		{
			List<string> errors = new List<string>();
			decimal v = Input.Dec(AmortizationAmount, "Valor da amortização", errors);
			Input.Check(errors);
			if (Dialogs.Confirm($"Registrar {Money.Fmt(v)} como amortização extraordinária agora? Primeiro abate o que estiver vencido; o resto reduz o principal e o cronograma é recalculado ({(ShortenTerm ? "menos parcelas" : "parcelas menores")}) numa nova versão."))
			{
				Svc.Loans.ExtraAmortization(_detail.Entity.Id, DateTime.Now, v, ShortenTerm, PmtMethod, "Amortização extraordinária");
				Reload();
			}
		}
	}

	private void ChangeState(SpecialState state, string question)
	{
		if ((object)_detail == null || !Dialogs.Confirm(question))
		{
			return;
		}
		string reason = "";
		if (state != SpecialState.None)
		{
			string m = Dialogs.AskText("Motivo", "Descreva o motivo (fica no histórico):");
			if (m == null)
			{
				return;
			}
			reason = m;
		}
		Svc.Loans.ChangeState(_detail.Entity.Id, state, DateTime.Now, reason);
		Reload();
	}

	private void CopyCollection()
	{
		if ((object)_detail != null)
		{
			LoanPosition pos = _detail.Position;
			List<InstallmentState> openOnes = (from p in pos.Installments
				where p.Version == pos.CurrentVersion && p.OutstandingTotal > 0m
				orderby p.DueDate
				select p).ToList();
			List<InstallmentState> overdue = openOnes.Where((InstallmentState p) => p.DueDate < pos.Reference).ToList();
			string name = _detail.Client.Split(' ')[0];
			string t;
			if (overdue.Count > 0)
			{
				t = $"Oi, {name}! Passando para lembrar que há {overdue.Count} parcela(s) em aberto, total de {Money.Fmt(overdue.Sum((InstallmentState p) => p.OutstandingTotal))} até hoje ({pos.Reference:dd/MM/yyyy}), já com multa e juros de atraso. Me avisa quando puder acertar.";
			}
			else
			{
				t = ((openOnes.Count <= 0) ? ("Oi, " + name + "! Seu empréstimo está quitado. Obrigado!") : $"Oi, {name}! Lembrete: a próxima parcela vence em {openOnes[0].DueDate:dd/MM/yyyy}, valor {Money.Fmt(openOnes[0].OutstandingTotal)}.");
			}
			Clipboard.SetText(t);
			Dialogs.Info("Mensagem copiada:\n\n" + t);
		}
	}
}
