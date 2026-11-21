using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using MyFinances.Core.Common;
using MyFinances.Core.Loans;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public sealed class LoansService
{
	private static readonly JsonSerializerOptions Json = SettingsService.Json;

	public static LoanCalc ToCalc(Loan e)
	{
		return new LoanCalc(e.Id, (from v in e.Versions
			orderby v.Number
			select new VersionCalc(v.Number, v.Start, (VersionKind)v.Kind, JsonSerializer.Deserialize<LoanTerms>(v.TermsJson, Json), JsonSerializer.Deserialize<List<ScheduledInstallment>>(v.InstallmentsJson, Json), v.DiscountGranted, v.Reason)).ToList(), e.Payments.Select((Payment p) => new PaymentCalc(p.Id, p.Timestamp, p.Amount, p.Reversed, p.ExtraAmortization)).ToList(), (SpecialState)e.State, e.StateDate);
	}

	private static IQueryable<Loan> Complete(AppDbContext db)
	{
		return db.Loans.AsNoTracking().Include((Loan e) => e.Client).Include((Loan e) => e.Versions)
			.Include((Loan e) => e.Payments);
	}

	public List<LoanSummary> ListAll(DateOnly? reference = null)
	{
		DateOnly today = reference ?? DateOnly.FromDateTime(DateTime.Today);
		using AppDbContext db = AppDbContext.Create();
		List<LoanSummary> list = new List<LoanSummary>();
		foreach (Loan e in Complete(db).AsSplitQuery().ToList())
		{
			LoanPosition pos = LoanEngine.Calculate(ToCalc(e), today);
			InstallmentState next = (from p in pos.Installments
				where p.Version == pos.CurrentVersion && p.OutstandingTotal > 0m
				orderby p.DueDate
				select p).FirstOrDefault();
			list.Add(new LoanSummary(e.Id, e.Code, e.ClientId, e.Client?.FullName ?? "", e.ContractDate, pos.ContractedPrincipal, pos.OutstandingPrincipal, pos.TotalOwed, pos.Status, pos.OverdueInstallments, pos.LongestDelayDays, next?.DueDate, next?.OutstandingTotal ?? 0m, pos.CurrentVersion));
		}
		return list.OrderByDescending((LoanSummary x) => x.Code).ToList();
	}

	public LoanDetail Get(Guid id, DateOnly? reference = null)
	{
		using AppDbContext db = AppDbContext.Create();
		Loan e = Complete(db).AsSplitQuery().FirstOrDefault((Loan x) => x.Id == id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado."));
		LoanCalc calc = ToCalc(e);
		LoanPosition pos = LoanEngine.Calculate(calc, reference ?? DateOnly.FromDateTime(DateTime.Today));
		return new LoanDetail(e, e.Client?.FullName ?? "", calc, pos, calc.Versions, e.Payments.OrderBy((Payment p) => p.Timestamp).ToList());
	}

	public List<(LoanCalc Loan, DateOnly ContractDate)> AllForIndicators()
	{
		using AppDbContext db = AppDbContext.Create();
		return (from e in db.Loans.AsNoTracking().Include((Loan e) => e.Versions).Include((Loan e) => e.Payments)
				.AsSplitQuery()
				.ToList()
			select (ToCalc(e), ContractDate: e.ContractDate)).ToList();
	}

	public Loan Create(Guid clientId, LoanTerms c, string? notes)
	{
		List<string> errors = Schedule.Validate(c);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		IReadOnlyList<ScheduledInstallment> installments = Schedule.Generate(c);
		using AppDbContext db = AppDbContext.Create();
		if (!(db.Clients.Find(clientId) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Escolha o cliente."))).Active)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Cliente inativo. Reative o cadastro antes de emprestar."));
		}
		int code = ((IQueryable<Loan>)db.Loans).Max((Expression<Func<Loan, int?>>)((Loan loan) => loan.Code)).GetValueOrDefault() + 1;
		Loan e = new Loan
		{
			ClientId = clientId,
			Code = code,
			ContractDate = c.ContractDate,
			Notes = (string.IsNullOrWhiteSpace(notes) ? null : notes.Trim())
		};
		e.Versions.Add(NewVersion(1, c.ContractDate.ToDateTime(TimeOnly.MinValue), VersionKind.Original, c, installments, 0m, null));
		db.Loans.Add(e);
		Audit.Record(db, "Loan", e.Id, "Create", $"#{code}, {installments.Count} installments, {c.Method}");
		db.SaveChanges();
		return e;
	}

	private static LoanVersion NewVersion(int number, DateTime start, VersionKind kind, LoanTerms c, IReadOnlyList<ScheduledInstallment> installments, decimal discount, string? reason)
	{
		return new LoanVersion
		{
			Number = number,
			Start = start,
			Kind = (int)kind,
			TermsJson = JsonSerializer.Serialize(c, Json),
			InstallmentsJson = JsonSerializer.Serialize(installments, Json),
			DiscountGranted = discount,
			Reason = reason
		};
	}

	public static List<string> ValidatePayment(decimal amount, DateTime timestamp, DateOnly contractDate)
	{
		List<string> e = new List<string>();
		if (amount <= 0m)
		{
			e.Add("O valor do pagamento precisa ser maior que zero.");
		}
		if (DateOnly.FromDateTime(timestamp) < contractDate)
		{
			e.Add("O pagamento não pode ser antes da contratação.");
		}
		if (timestamp > DateTime.Now.AddDays(1.0))
		{
			e.Add("O pagamento não pode ser no futuro.");
		}
		return e;
	}

	public Payment RecordPayment(Guid loanId, DateTime timestamp, decimal amount, string method, string? note, bool extraAmortization = false)
	{
		using AppDbContext db = AppDbContext.Create();
		Loan e = db.Loans.Include((Loan x) => x.Versions).FirstOrDefault((Loan x) => x.Id == loanId) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado."));
		List<string> errors = ValidatePayment(amount, timestamp, e.ContractDate);
		if (e.State == 1)
		{
			errors.Add("Contrato cancelado: reabra antes de registrar pagamento.");
		}
		if (e.Versions.Count > 0 && timestamp < e.Versions.Max((LoanVersion v) => v.Start))
		{
			errors.Add("Há uma renegociação/prorrogação depois dessa data. Pagamentos anteriores a ela mudariam a versão já fechada; registre com data posterior ou estorne a versão.");
		}
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		Payment p = new Payment
		{
			LoanId = loanId,
			Timestamp = timestamp,
			Amount = Money.R2(amount),
			PaymentMethod = (string.IsNullOrWhiteSpace(method) ? "Pix" : method.Trim()),
			Note = (string.IsNullOrWhiteSpace(note) ? null : note.Trim()),
			ExtraAmortization = extraAmortization
		};
		db.Payments.Add(p);
		Audit.Record(db, "Payment", p.Id, "Record", $"loan #{e.Code}");
		db.SaveChanges();
		return p;
	}

	public void Reverse(Guid paymentId, string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Informe o motivo do estorno."));
		}
		using AppDbContext db = AppDbContext.Create();
		Payment p = db.Payments.Find(paymentId) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Pagamento não encontrado."));
		if (p.Reversed)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Este pagamento já foi estornado."));
		}
		DateTime lastVersion = db.LoanVersions.Where((LoanVersion v) => v.LoanId == p.LoanId).Max((LoanVersion v) => v.Start);
		if (p.Timestamp < lastVersion && db.LoanVersions.Count((LoanVersion v) => v.LoanId == p.LoanId) > 1)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Este pagamento é anterior a uma renegociação/prorrogação. Desfaça a última versão antes de estornar."));
		}
		p.Reversed = true;
		p.ReversedAt = DateTime.Now;
		p.ReversalReason = reason.Trim();
		Audit.Record(db, "Payment", p.Id, "Reverse");
		db.SaveChanges();
	}

	public void Renegotiate(Guid id, DateTime date, LoanTerms newOnes, bool capitalizeInterest, bool capitalizeCharges, string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Informe o motivo da renegociação."));
		}
		using AppDbContext db = AppDbContext.Create();
		Loan e = db.Loans.Include((Loan x) => x.Versions).Include((Loan x) => x.Payments).FirstOrDefault((Loan x) => x.Id == id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado."));
		CheckActive(e, date);
		var (princ, interest, chg, _) = LoanEngine.OutstandingOnDate(ToCalc(e), DateOnly.FromDateTime(date));
		if (princ + interest + chg <= 0m)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Não há saldo em aberto para renegociar."));
		}
		decimal newPrincipal = princ + (capitalizeInterest ? interest : 0m) + (capitalizeCharges ? chg : 0m);
		decimal discount = (capitalizeInterest ? 0m : interest) + (capitalizeCharges ? 0m : chg);
		LoanTerms c = newOnes with
		{
			Principal = newPrincipal,
			ContractDate = DateOnly.FromDateTime(date)
		};
		List<string> errors = Schedule.Validate(c);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		int num = e.Versions.Max((LoanVersion loanVersion) => loanVersion.Number) + 1;
		LoanVersion v = NewVersion(num, date, VersionKind.Renegotiation, c, Schedule.Generate(c), discount, $"{reason.Trim()} | em aberto: principal {Money.Num(princ)}, juros vencidos {Money.Num(interest)}, encargos {Money.Num(chg)}" + ((capitalizeInterest | capitalizeCharges) ? " | incorporado ao novo principal (gera juros sobre esse valor)" : ""));
		v.LoanId = e.Id;
		db.LoanVersions.Add(v);
		Audit.Record(db, "Loan", e.Id, "Renegotiate", $"#{e.Code}, version {num}");
		db.SaveChanges();
	}

	public void Extend(Guid id, DateTime date, int periods, bool chargeExtensionInterest, bool capitalizeCharges, string reason)
	{
		if ((periods < 1 || periods > 60) ? true : false)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Prorrogar: de 1 a 60 períodos."));
		}
		if (string.IsNullOrWhiteSpace(reason))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Informe o motivo da prorrogação."));
		}
		using AppDbContext db = AppDbContext.Create();
		Loan e = db.Loans.Include((Loan x) => x.Versions).Include((Loan x) => x.Payments).FirstOrDefault((Loan x) => x.Id == id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado."));
		CheckActive(e, date);
		LoanCalc loanCalc = ToCalc(e);
		IReadOnlyList<VersionCalc> versions = loanCalc.Versions;
		LoanTerms current = versions[versions.Count - 1].Terms;
		(decimal Principal, decimal OverdueInterest, decimal Charges, IReadOnlyList<InstallmentState> Open) tuple = LoanEngine.OutstandingOnDate(loanCalc, DateOnly.FromDateTime(date));
		decimal chg = tuple.Charges;
		IReadOnlyList<InstallmentState> item = tuple.Open;
		if (item.Count == 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Não há parcelas em aberto para prorrogar."));
		}
		List<ScheduledInstallment> installments = Schedule.Extend(item.Select((InstallmentState p) => (DueDate: p.DueDate, OutstandingPrincipal: p.OutstandingPrincipal, OutstandingInterest: p.OutstandingInterest, OutstandingFee: p.OutstandingFee)), current.Frequency, periods, current.InterestRatePct, chargeExtensionInterest).ToList();
		if (capitalizeCharges && chg > 0m)
		{
			installments[0] = installments[0] with
			{
				Fee = installments[0].Fee + chg,
				Breakdown = installments[0].Breakdown + "; + encargos de atraso incorporados " + Money.Num(chg)
			};
		}
		LoanTerms c = current with
		{
			Principal = installments.Sum((ScheduledInstallment p) => p.Principal),
			ContractDate = DateOnly.FromDateTime(date),
			FirstDueDate = installments[0].DueDate,
			InstallmentCount = installments.Count,
			GracePeriods = 0,
			GraceKind = GraceKind.None,
			SetupFee = 0m
		};
		int num = e.Versions.Max((LoanVersion loanVersion) => loanVersion.Number) + 1;
		LoanVersion v = NewVersion(num, date, VersionKind.Extension, c, installments, capitalizeCharges ? 0m : chg, $"{reason.Trim()} | {periods} período(s)");
		v.LoanId = e.Id;
		db.LoanVersions.Add(v);
		Audit.Record(db, "Loan", e.Id, "Extend", $"#{e.Code}, version {num}, {periods} period(s)");
		db.SaveChanges();
	}

	public void ExtraAmortization(Guid id, DateTime date, decimal amount, bool shortenTerm, string method, string? note)
	{
		using (AppDbContext db0 = AppDbContext.Create())
		{
			CheckActive(db0.Loans.Include((Loan x) => x.Versions).FirstOrDefault((Loan x) => x.Id == id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado.")), date);
		}
		RecordPayment(id, date, amount, method, note, extraAmortization: true);
		using AppDbContext db1 = AppDbContext.Create();
		Loan e = db1.Loans.Include((Loan x) => x.Versions).Include((Loan x) => x.Payments).First((Loan x) => x.Id == id);
		LoanCalc calc = ToCalc(e);
		DateOnly day = DateOnly.FromDateTime(date);
		(decimal Principal, decimal OverdueInterest, decimal Charges, IReadOnlyList<InstallmentState> Open) tuple = LoanEngine.OutstandingOnDate(calc, day);
		decimal princ = tuple.Principal;
		List<InstallmentState> upcoming = (from p in tuple.Open
			where p.DueDate > day
			orderby p.DueDate
			select p).ToList();
		if (princ <= 0m || upcoming.Count == 0)
		{
			return;
		}
		IReadOnlyList<VersionCalc> versions = calc.Versions;
		LoanTerms terms = versions[versions.Count - 1].Terms;
		int n = upcoming.Count;
		if (shortenTerm)
		{
			if (upcoming[upcoming.Count - 1].Total > 0m)
			{
				n = Math.Max(1, (int)Math.Ceiling(princ / Math.Max(0.01m, upcoming.Average((InstallmentState p) => p.Principal))));
			}
			n = Math.Min(n, upcoming.Count);
		}
		LoanTerms c = terms with
		{
			Principal = princ,
			ContractDate = day,
			FirstDueDate = upcoming[0].DueDate,
			InstallmentCount = n,
			GracePeriods = 0,
			GraceKind = GraceKind.None,
			SetupFee = 0m
		};
		int num = e.Versions.Max((LoanVersion loanVersion) => loanVersion.Number) + 1;
		LoanVersion v = NewVersion(num, date.AddSeconds(1.0), VersionKind.ExtraAmortization, c, Schedule.Generate(c), 0m, $"Amortização extraordinária de {Money.Num(amount)}; principal restante {Money.Num(princ)}; {(shortenTerm ? "prazo reduzido" : "parcela reduzida")}");
		v.LoanId = e.Id;
		db1.LoanVersions.Add(v);
		Audit.Record(db1, "Loan", e.Id, "ExtraAmortization", $"#{e.Code}, version {num}");
		db1.SaveChanges();
	}

	public void UndoLastVersion(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		Loan e = db.Loans.Include((Loan x) => x.Versions).Include((Loan x) => x.Payments).First((Loan x) => x.Id == id);
		if (e.Versions.Count <= 1)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Só existe a versão original."));
		}
		LoanVersion last = e.Versions.OrderBy((LoanVersion v) => v.Number).Last();
		if (e.Payments.Any((Payment p) => !p.Reversed && p.Timestamp >= last.Start))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Há pagamentos na versão atual. Estorne-os antes de desfazer a versão."));
		}
		db.LoanVersions.Remove(last);
		Audit.Record(db, "Loan", e.Id, "UndoVersion", $"#{e.Code}, version {last.Number}");
		db.SaveChanges();
	}

	public void ChangeState(Guid id, SpecialState state, DateTime date, string reason)
	{
		if (state != SpecialState.None && string.IsNullOrWhiteSpace(reason))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Informe o motivo."));
		}
		using AppDbContext db = AppDbContext.Create();
		Loan e = db.Loans.Find(id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Empréstimo não encontrado."));
		if (DateOnly.FromDateTime(date) < e.ContractDate)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("A data não pode ser antes da contratação."));
		}
		e.State = (int)state;
		e.StateDate = ((state == SpecialState.None) ? ((DateTime?)null) : new DateTime?(date));
		e.StateReason = ((state == SpecialState.None) ? null : reason.Trim());
		AppDbContext db2 = db;
		Guid? id2 = e.Id;
		Audit.Record(db2, "Loan", id2, state switch
		{
			SpecialState.Canceled => "Cancel", 
			SpecialState.WrittenOff => "WriteOff", 
			_ => "Reopen", 
		}, $"#{e.Code}");
		db.SaveChanges();
	}

	private static void CheckActive(Loan e, DateTime date)
	{
		if (e.State != 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Contrato cancelado ou baixado: reabra antes."));
		}
		if (DateOnly.FromDateTime(date) < e.ContractDate)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("A data não pode ser antes da contratação."));
		}
		if (e.Versions.Count > 0 && date <= e.Versions.Max((LoanVersion v) => v.Start))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("A data precisa ser depois do início da versão atual."));
		}
	}
}
