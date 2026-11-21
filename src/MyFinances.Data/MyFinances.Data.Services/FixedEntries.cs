using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public static class FixedEntries
{
	public sealed record Change(List<Entry> Before, List<Guid> Created, string Group, FixedSeries? SeriesBefore);

	private const string Prefix = "FixedSeries:";

	public static bool IsFixed(Entry e)
	{
		if (e.Origin == EntryOrigin.Fixed)
		{
			return e.Group != null;
		}
		return false;
	}

	private static YearMonth Horizon(YearMonth start)
	{
		YearMonth h = new YearMonth(YearMonth.Today.Year + 1, 12);
		if (!(h < start))
		{
			return h;
		}
		return start;
	}

	private static FixedSeries? Read(AppDbContext db, string group)
	{
		Setting s = db.Settings.FirstOrDefault((Setting x) => x.Key == "FixedSeries:" + group);
		if (s != null)
		{
			return JsonSerializer.Deserialize<FixedSeries>(s.Amount, SettingsService.Json);
		}
		return null;
	}

	private static void Write(AppDbContext db, FixedSeries f)
	{
		string json = JsonSerializer.Serialize(f, SettingsService.Json);
		Setting s = db.Settings.FirstOrDefault((Setting x) => x.Key == "FixedSeries:" + f.Group);
		if (s == null)
		{
			db.Settings.Add(new Setting
			{
				Key = "FixedSeries:" + f.Group,
				Amount = json
			});
		}
		else
		{
			s.Amount = json;
			s.UpdatedAt = DateTime.Now;
		}
	}

	private static Entry NewRow(Entry model, YearMonth month)
	{
		return new Entry
		{
			YearMonth = month.Key,
			Kind = model.Kind,
			Description = model.Description,
			Amount = model.Amount,
			CategoryId = model.CategoryId,
			Origin = EntryOrigin.Fixed,
			Group = model.Group,
			CreatedAt = DateTime.Now,
			UpdatedAt = DateTime.Now
		};
	}

	public static string Create(EntryKind kind, string description, decimal amount, YearMonth start, Guid? categoryId = null)
	{
		List<string> errors = FinanceService.Validate(kind, description, amount);
		if (kind == EntryKind.Deduction)
		{
			errors.Add("Lançamento fixo é para despesa, entrada ou provento.");
		}
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		string group = "Fixo " + Guid.NewGuid().ToString("N").Substring(0, 8);
		Entry model = new Entry
		{
			Kind = (int)kind,
			Description = description.Trim(),
			Amount = Money.R2(amount),
			CategoryId = categoryId,
			Group = group
		};
		using AppDbContext db = AppDbContext.Create();
		YearMonth until = Horizon(start);
		foreach (YearMonth m in YearMonth.Range(start, until))
		{
			db.Entries.Add(NewRow(model, m));
		}
		Write(db, new FixedSeries(group, until.Key, null));
		Audit.Record(db, "Entry", null, "FixedCreate", $"{start.Key} kind {(int)kind}");
		db.SaveChanges();
		return group;
	}

	private static bool SameName(Entry a, Entry b)
	{
		if (a.Kind == b.Kind)
		{
			return string.Equals(a.Description.Trim(), b.Description.Trim(), StringComparison.CurrentCultureIgnoreCase);
		}
		return false;
	}

	public static Change MakeFixed(Guid id, YearMonth? startEarlier = null)
	{
		using AppDbContext db = AppDbContext.Create();
		Entry entry = db.Entries.FirstOrDefault((Entry e) => e.Id == id) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Lançamento não encontrado."));
		if (IsFixed(entry))
		{
			return new Change(new List<Entry>(), new List<Guid>(), entry.Group, null);
		}
		if (entry.Kind == 2)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Lançamento fixo é para despesa, entrada ou provento."));
		}
		YearMonth month = YearMonth.ParseKey(entry.YearMonth);
		string group = "Fixo " + Guid.NewGuid().ToString("N").Substring(0, 8);
		YearMonth until = Horizon(month);
		YearMonth yearMonth;
		if (startEarlier.HasValue)
		{
			YearMonth s = startEarlier.GetValueOrDefault();
			if (s < month)
			{
				yearMonth = s;
				goto IL_0154;
			}
		}
		yearMonth = month;
		goto IL_0154;
		IL_0154:
		YearMonth from = yearMonth;
		List<string> keys = (from yearMonth2 in YearMonth.Range(@from, until)
			select yearMonth2.Key).ToList();
		Dictionary<string, Entry> twins = (from e in db.Entries.Where((Entry e) => keys.Contains(e.YearMonth) && e.Kind == entry.Kind && e.Id != entry.Id && (int)e.Origin != 10).ToList()
			where SameName(e, entry)
			group e by e.YearMonth).ToDictionary((IGrouping<string, Entry> g) => g.Key, (IGrouping<string, Entry> g) => g.First());
		List<Entry> before = new List<Entry> { Copy(entry) };
		List<Guid> created = new List<Guid>();
		entry.Origin = EntryOrigin.Fixed;
		entry.Group = group;
		entry.UpdatedAt = DateTime.Now;
		foreach (YearMonth m in YearMonth.Range(from, until))
		{
			if (!(m == month))
			{
				if (twins.TryGetValue(m.Key, out var t))
				{
					before.Add(Copy(t));
					t.Origin = EntryOrigin.Fixed;
					t.Group = group;
					t.UpdatedAt = DateTime.Now;
				}
				else
				{
					Entry row = NewRow(entry, m);
					db.Entries.Add(row);
					created.Add(row.Id);
				}
			}
		}
		Write(db, new FixedSeries(group, until.Key, null));
		Audit.Record(db, "Entry", id, "FixedMake", $"{from.Key} {created.Count} new, {before.Count - 1} joined");
		db.SaveChanges();
		return new Change(before, created, group, null);
	}

	public static Change MakeNormal(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		Entry entry = db.Entries.FirstOrDefault((Entry e) => e.Id == id);
		if (entry == null || !IsFixed(entry))
		{
			return new Change(new List<Entry>(), new List<Guid>(), "", null);
		}
		string group = entry.Group;
		FixedSeries seriesBefore = Read(db, group);
		List<Entry> later = (from e in db.Entries.Where((Entry e) => e.Group == @group && (int)e.Origin == 10).ToList()
			where string.CompareOrdinal(e.YearMonth, entry.YearMonth) > 0
			select e).ToList();
		List<Entry> list = new List<Entry>();
		list.Add(Copy(entry));
		list.AddRange(later.Select(Copy));
		db.Entries.RemoveRange(later);
		entry.Origin = EntryOrigin.Manual;
		entry.Group = null;
		entry.UpdatedAt = DateTime.Now;
		if ((object)seriesBefore != null)
		{
			Write(db, seriesBefore with
			{
				End = entry.YearMonth
			});
		}
		Audit.Record(db, "Entry", id, "FixedUnmake", $"{entry.YearMonth} {later.Count} removed");
		db.SaveChanges();
		return new Change(list, new List<Guid>(), group, seriesBefore);
	}

	public static void Undo(Change c)
	{
		using AppDbContext db = AppDbContext.Create();
		db.Entries.RemoveRange(db.Entries.Where((Entry entry) => c.Created.Contains(entry.Id)).ToList());
		foreach (Entry r in c.Before)
		{
			Entry e = db.Entries.FirstOrDefault((Entry x) => x.Id == r.Id);
			if (e == null)
			{
				db.Entries.Add(Copy(r));
				continue;
			}
			e.Origin = r.Origin;
			e.Group = r.Group;
			e.Description = r.Description;
			e.Amount = r.Amount;
			e.Kind = r.Kind;
			e.UpdatedAt = DateTime.Now;
		}
		string key = "FixedSeries:" + c.Group;
		if ((object)c.SeriesBefore != null)
		{
			Write(db, c.SeriesBefore);
		}
		else
		{
			Setting s = db.Settings.FirstOrDefault((Setting x) => x.Key == key);
			if (s != null)
			{
				db.Settings.Remove(s);
			}
		}
		db.SaveChanges();
	}

	public static int StartEarlier(string group, YearMonth newStart)
	{
		using AppDbContext db = AppDbContext.Create();
		List<Entry> rows = (from e in db.Entries.AsNoTracking()
			where e.Group == @group && (int)e.Origin == 10
			select e).ToList();
		if (rows.Count == 0)
		{
			return 0;
		}
		Entry first = rows.MinBy((Entry e) => e.YearMonth);
		YearMonth firstMonth = YearMonth.ParseKey(first.YearMonth);
		if (newStart >= firstMonth)
		{
			return 0;
		}
		int n = 0;
		foreach (YearMonth m in YearMonth.Range(newStart, firstMonth.Previous))
		{
			db.Entries.Add(NewRow(first, m));
			n++;
		}
		Audit.Record(db, "Entry", null, "FixedStartEarlier", $"{newStart.Key} {n} row(s)");
		db.SaveChanges();
		return n;
	}

	public static void Extend(YearMonth until)
	{
		YearMonth cap = YearMonth.Today.AddMonths(120);
		if (until > cap)
		{
			until = cap;
		}
		using AppDbContext db = AppDbContext.Create();
		List<FixedSeries> series = (from s in (from x in db.Settings.AsNoTracking()
				where x.Key.StartsWith("FixedSeries:")
				select x).ToList()
			select JsonSerializer.Deserialize<FixedSeries>(s.Amount, SettingsService.Json) into fixedSeries
			where fixedSeries.End == null && YearMonth.ParseKey(fixedSeries.Generated) < until
			select fixedSeries).ToList();
		if (series.Count == 0)
		{
			return;
		}
		foreach (FixedSeries f in series)
		{
			Entry last = (from e in db.Entries.AsNoTracking()
				where e.Group == f.Group && (int)e.Origin == 10
				orderby e.YearMonth descending
				select e).FirstOrDefault();
			if (last != null)
			{
				foreach (YearMonth m in YearMonth.Range(YearMonth.ParseKey(f.Generated).Next, until))
				{
					db.Entries.Add(NewRow(last, m));
				}
			}
			Write(db, f with
			{
				Generated = until.Key
			});
		}
		db.SaveChanges();
	}

	public static List<Entry> ApplyForward(Guid fromRow)
	{
		using AppDbContext db = AppDbContext.Create();
		Entry src = db.Entries.AsNoTracking().FirstOrDefault((Entry e) => e.Id == fromRow);
		if (src?.Group == null || src.Origin != EntryOrigin.Fixed)
		{
			return new List<Entry>();
		}
		List<Entry> later = (from e in db.Entries.Where((Entry e) => e.Group == src.Group && (int)e.Origin == 10).ToList()
			where string.CompareOrdinal(e.YearMonth, src.YearMonth) > 0
			select e).ToList();
		List<Entry> before = later.Select(Copy).ToList();
		foreach (Entry item in later)
		{
			item.Description = src.Description;
			item.Amount = src.Amount;
			item.Kind = src.Kind;
			item.CategoryId = src.CategoryId;
			item.UpdatedAt = DateTime.Now;
		}
		Audit.Record(db, "Entry", null, "FixedApplyForward", $"{src.YearMonth} {later.Count} row(s)");
		db.SaveChanges();
		return before;
	}

	public static void RestoreRows(IEnumerable<Entry> rows)
	{
		using AppDbContext db = AppDbContext.Create();
		foreach (Entry r in rows)
		{
			Entry e = db.Entries.FirstOrDefault((Entry x) => x.Id == r.Id);
			if (e == null)
			{
				db.Entries.Add(Copy(r));
				continue;
			}
			e.Description = r.Description;
			e.Amount = r.Amount;
			e.Kind = r.Kind;
			e.CategoryId = r.CategoryId;
			e.UpdatedAt = DateTime.Now;
		}
		db.SaveChanges();
	}

	public static (List<Entry> Removed, FixedSeries? Before) StopFrom(string group, YearMonth from)
	{
		using AppDbContext db = AppDbContext.Create();
		FixedSeries before = Read(db, group);
		List<Entry> rows = (from e in db.Entries.Where((Entry e) => e.Group == @group && (int)e.Origin == 10).ToList()
			where string.CompareOrdinal(e.YearMonth, @from.Key) >= 0
			select e).ToList();
		List<Entry> item = rows.Select(Copy).ToList();
		db.Entries.RemoveRange(rows);
		if ((object)before != null)
		{
			Write(db, before with
			{
				End = from.Previous.Key
			});
		}
		Audit.Record(db, "Entry", null, "FixedStop", $"{from.Key} {rows.Count} row(s)");
		db.SaveChanges();
		return (Removed: item, Before: before);
	}

	public static void UndoStop(IEnumerable<Entry> removed, FixedSeries? before)
	{
		AppDbContext db = AppDbContext.Create();
		try
		{
			foreach (Entry r in removed.Where((Entry entry) => !db.Entries.Any((Entry x) => x.Id == entry.Id)))
			{
				db.Entries.Add(Copy(r));
			}
			if ((object)before != null)
			{
				Write(db, before);
			}
			db.SaveChanges();
		}
		finally
		{
			if (db != null)
			{
				((IDisposable)db).Dispose();
			}
		}
	}

	private static Entry Copy(Entry e)
	{
		return new Entry
		{
			Id = e.Id,
			YearMonth = e.YearMonth,
			Kind = e.Kind,
			Description = e.Description,
			Amount = e.Amount,
			CategoryId = e.CategoryId,
			Origin = e.Origin,
			Group = e.Group,
			Installment = e.Installment,
			CreatedAt = e.CreatedAt,
			UpdatedAt = e.UpdatedAt
		};
	}
}
