using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public sealed class SettingsService
{
	public static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = false,
		Converters = { (JsonConverter)new JsonStringEnumConverter() },
		NumberHandling = JsonNumberHandling.AllowReadingFromString
	};

	private const string PrefsKey = "Preferences";

	private const string ParametersKey = "LegalParameters:";

	public Preferences Preferences()
	{
		using AppDbContext db = AppDbContext.Create();
		Setting c = db.Settings.AsNoTracking().FirstOrDefault((Setting x) => x.Key == "Preferences");
		if (c == null)
		{
			return new Preferences();
		}
		try
		{
			return JsonSerializer.Deserialize<Preferences>(c.Amount, Json) ?? new Preferences();
		}
		catch
		{
			return new Preferences();
		}
	}

	public void Save(Preferences p)
	{
		Store("Preferences", JsonSerializer.Serialize(p, Json));
	}

	public (LegalParameters Parameters, bool Estimated) ParametersForYear(int year)
	{
		Dictionary<int, LegalParameters> all = AllParameters();
		if (all.TryGetValue(year, out var p))
		{
			return (Parameters: p, Estimated: false);
		}
		int previous = all.Keys.Where((int k) => k < year).DefaultIfEmpty(all.Keys.Min()).Max();
		return (Parameters: all[previous], Estimated: true);
	}

	public Dictionary<int, LegalParameters> AllParameters()
	{
		Dictionary<int, LegalParameters> r = new Dictionary<int, LegalParameters> { [2026] = LegalParameters.Default2026 };
		using AppDbContext db = AppDbContext.Create();
		foreach (Setting c in (from x in db.Settings.AsNoTracking()
			where x.Key.StartsWith("LegalParameters:")
			select x).ToList())
		{
			try
			{
				LegalParameters p = JsonSerializer.Deserialize<LegalParameters>(c.Amount, Json);
				if ((object)p != null)
				{
					r[p.Year] = p;
				}
			}
			catch
			{
			}
		}
		return r;
	}

	public void SaveParameters(LegalParameters p)
	{
		Store("LegalParameters:" + p.Year, JsonSerializer.Serialize(p, Json));
	}

	public void RemoveParameters(int year)
	{
		using AppDbContext db = AppDbContext.Create();
		Setting c = db.Settings.FirstOrDefault((Setting x) => x.Key == "LegalParameters:" + year);
		if (c != null)
		{
			db.Remove(c);
			db.SaveChanges();
		}
	}

	private static void Store(string key, string amount)
	{
		using AppDbContext db = AppDbContext.Create();
		Setting c = db.Settings.FirstOrDefault((Setting x) => x.Key == key);
		if (c == null)
		{
			db.Settings.Add(new Setting
			{
				Key = key,
				Amount = amount
			});
		}
		else
		{
			c.Amount = amount;
			c.UpdatedAt = DateTime.Now;
		}
		db.SaveChanges();
	}

	public Calendar Calendar()
	{
		return new Calendar(Preferences().LocalHolidays);
	}
}
