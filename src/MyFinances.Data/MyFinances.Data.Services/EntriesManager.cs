using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public sealed class EntriesManager
{
	private static readonly Regex Suffix = new Regex("\\s*(\\d+)\\s*/\\s*(\\d+)\\s*$");

	public static string BaseName(string description)
	{
		return Suffix.Replace(description.Trim(), "").Trim();
	}

	private static (int K, int N)? InstallmentOf(string description)
	{
		Match m = Suffix.Match(description.Trim());
		if (!m.Success)
		{
			return null;
		}
		return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
	}

	private static bool Matches(Entry e, EntryKind kind, string name)
	{
		if (e.Kind == (int)kind)
		{
			return string.Equals(BaseName(e.Description), name.Trim(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static List<Entry> All(AppDbContext db)
	{
		return db.Entries.AsNoTracking().ToList();
	}

	public List<EntrySeries> Series()
	{
		using AppDbContext db = AppDbContext.Create();
		return (from s in (from e in All(db)
				group e by (Kind: (EntryKind)e.Kind, Name: BaseName(e.Description).ToLowerInvariant())).Select((IGrouping<(EntryKind Kind, string Name), Entry> g) =>
			{
				List<Entry> list = g.OrderBy((Entry e) => e.YearMonth).ToList();
				Entry entry = list[list.Count - 1];
				(int, int)? tuple = InstallmentOf(entry.Description);
				return new EntrySeries(g.Key.Kind, BaseName(entry.Description), list.Count, YearMonth.ParseKey(list[0].YearMonth), YearMonth.ParseKey(entry.YearMonth), entry.Amount, tuple?.Item1, tuple?.Item2);
			})
			orderby s.Kind
			select s).ThenBy((EntrySeries s) => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	public List<EntryOccurrence> Occurrences(EntryKind kind, string name)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from e in All(db)
			where Matches(e, kind, name)
			orderby e.YearMonth
			select new EntryOccurrence(e.Id, YearMonth.ParseKey(e.YearMonth), e.Description, e.Amount)).ToList();
	}

	public decimal? CreditOf(EntryKind kind, string name)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> all = All(db);
		HashSet<string> groups = (from e in all
			where Matches(e, kind, name) && e.Group != null
			select e.Group).ToHashSet();
		decimal credit = all.Where((Entry e) => e.Kind == 3 && e.Group != null && groups.Contains(e.Group)).Sum((Entry e) => e.Amount);
		return (credit > 0m) ? new decimal?(credit) : ((decimal?)null);
	}

	public int ChangeAmount(IReadOnlyCollection<Guid> ids, decimal amount)
	{
		if (amount <= 0m)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("O valor precisa ser maior que zero."));
		}
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = db.Entries.Where((Entry e) => ids.Contains(e.Id)).ToList();
		foreach (Entry item in rows)
		{
			item.Amount = Money.R2(amount);
			item.UpdatedAt = DateTime.Now;
		}
		Audit.Record(db, "Entry", null, "ManageAmount", $"{rows.Count} row(s)");
		db.SaveChanges();
		return rows.Count;
	}

	public int Rename(IReadOnlyCollection<Guid> ids, string newName)
	{
		newName = newName.Trim();
		if (newName.Length == 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Digite o novo nome."));
		}
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = db.Entries.Where((Entry entry) => ids.Contains(entry.Id)).ToList();
		foreach (Entry e in rows)
		{
			(int, int)? inst = InstallmentOf(e.Description);
			string text;
			if (inst.HasValue)
			{
				(int, int) i = inst.GetValueOrDefault();
				text = $"{newName} {i.Item1}/{i.Item2}";
			}
			else
			{
				text = newName;
			}
			string description = text;
			List<string> errors = FinanceService.Validate((EntryKind)e.Kind, description, e.Amount, allowNegative: true);
			if (errors.Count > 0)
			{
				throw new ValidationException(errors);
			}
			e.Description = description;
			e.UpdatedAt = DateTime.Now;
		}
		Audit.Record(db, "Entry", null, "ManageRename", $"{rows.Count} row(s)");
		db.SaveChanges();
		return rows.Count;
	}

	public int Delete(IReadOnlyCollection<Guid> ids)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = db.Entries.Where((Entry e) => ids.Contains(e.Id)).ToList();
		db.Entries.RemoveRange(rows);
		Audit.Record(db, "Entry", null, "ManageDelete", $"{rows.Count} row(s)");
		db.SaveChanges();
		return rows.Count;
	}

	public int CompleteInstallments(EntryKind kind, string name)
	{
		using AppDbContext db = AppDbContext.Create();
		Entry last = (from e in All(db)
			where Matches(e, kind, name)
			orderby e.YearMonth
			select e).LastOrDefault() ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Lançamento não encontrado."));
		(int, int)? tuple = InstallmentOf(last.Description);
		if (tuple.HasValue)
		{
			(int, int) inst = tuple.GetValueOrDefault();
			if (inst.Item1 < inst.Item2)
			{
				YearMonth month = YearMonth.ParseKey(last.YearMonth);
				string group = "Gerenciar " + Guid.NewGuid().ToString("N").Substring(0, 8);
				string baseName = BaseName(last.Description);
				int n = 0;
				for (int k = inst.Item1 + 1; k <= inst.Item2; k++)
				{
					month = month.Next;
					db.Entries.Add(NewLike(last, month, $"{baseName} {k}/{inst.Item2}", group, EntryOrigin.Installments, $"{k}/{inst.Item2}"));
					n++;
				}
				Audit.Record(db, "Entry", null, "ManageComplete", $"{n} row(s)");
				db.SaveChanges();
				return n;
			}
		}
		throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Esse lançamento não tem parcelas faltando."));
	}

	public int RepeatUntil(EntryKind kind, string name, YearMonth until)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> source = (from e in All(db)
			where Matches(e, kind, name)
			select e).ToList();
		Entry last = source.OrderBy((Entry e) => e.YearMonth).LastOrDefault() ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Lançamento não encontrado."));
		if (InstallmentOf(last.Description).HasValue)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Esse lançamento é parcelado: use \"Completar parcelas\"."));
		}
		YearMonth start = YearMonth.ParseKey(last.YearMonth).Next;
		if (until < start)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Escolha um mês depois de " + YearMonth.ParseKey(last.YearMonth).SheetName + "."));
		}
		if (YearMonth.Difference(start, until) > 120)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Máximo de 10 anos de uma vez."));
		}
		HashSet<string> existing = source.Select((Entry e) => e.YearMonth).ToHashSet();
		string group = "Gerenciar " + Guid.NewGuid().ToString("N").Substring(0, 8);
		int n = 0;
		foreach (YearMonth month in YearMonth.Range(start, until))
		{
			if (!existing.Contains(month.Key))
			{
				db.Entries.Add(NewLike(last, month, last.Description.Trim(), group, EntryOrigin.Recurring, null));
				n++;
			}
		}
		Audit.Record(db, "Entry", null, "ManageRepeat", $"{n} row(s)");
		db.SaveChanges();
		return n;
	}

	private static Entry NewLike(Entry model, YearMonth month, string description, string group, EntryOrigin origin, string? installment)
	{
		return new Entry
		{
			YearMonth = month.Key,
			Kind = model.Kind,
			Description = description,
			Amount = model.Amount,
			CategoryId = model.CategoryId,
			Origin = origin,
			Group = group,
			Installment = installment,
			CreatedAt = DateTime.Now,
			UpdatedAt = DateTime.Now
		};
	}
}
