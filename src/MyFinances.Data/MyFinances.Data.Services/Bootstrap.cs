using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using MyFinances.Core.Labor;
using MyFinances.Data.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public static class Bootstrap
{
	private static readonly (string, EntryKind?, string)[] InitialCategories = new (string, EntryKind?, string)[12]
	{
		("Moradia", EntryKind.Expense, "#10B981"),
		("Alimentação", EntryKind.Expense, "#F59E0B"),
		("Transporte", EntryKind.Expense, "#38BDF8"),
		("Internet e assinaturas", EntryKind.Expense, "#047857"),
		("Saúde e cuidados", EntryKind.Expense, "#34D399"),
		("Lazer e jogos", EntryKind.Expense, "#F472B6"),
		("Estudos", EntryKind.Expense, "#60A5FA"),
		("Empréstimos e dívidas", EntryKind.Expense, "#F87171"),
		("Compras", EntryKind.Expense, "#FBBF24"),
		("Outros", null, "#9CA3AF"),
		("Renda extra", EntryKind.Inflow, "#2DD4BF"),
		("Empréstimo recebido", EntryKind.Inflow, "#FB923C")
	};

	public static bool CreatorMode => File.Exists(AppPaths.CreatorMode);

	public static void Run(SettingsService config, BackupService backup)
	{
		AppPaths.Ensure();
		Log.PruneOld();
		ClientsService.ClearTemp();
		Vault.OpenOrCreate();
		bool existed = File.Exists(AppPaths.Database);
		using (AppDbContext db = AppDbContext.Create())
		{
			List<string> pending = db.Database.GetPendingMigrations().ToList();
			if (existed && pending.Count > 0)
			{
				Log.Info($"Migrações pendentes: {pending.Count}. Fazendo backup antes.");
				backup.CreateAutomatic("antes-de-atualizar");
			}
			db.Database.Migrate();
			using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
			{
				db.Database.OpenConnection();
				cmd.CommandText = "PRAGMA journal_mode=WAL;";
				cmd.ExecuteNonQuery();
			}
			if (!db.Categories.Any())
			{
				(string, EntryKind?, string)[] initialCategories = InitialCategories;
				for (int i = 0; i < initialCategories.Length; i++)
				{
					var (name, kind, color) = initialCategories[i];
					db.Categories.Add(new Category
					{
						Name = name,
						Kind = (int?)kind,
						Color = color
					});
				}
				db.SaveChanges();
			}
		}
		backup.DailyBackupIfNeeded();
		Log.Info("Startup finished.");
	}

	public static void SetCreatorMode(bool active)
	{
		if (active)
		{
			File.WriteAllText(AppPaths.CreatorMode, "Modo criador do MyFinances ativado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ". Apague este arquivo para voltar a ver a tela de doação.");
		}
		else if (File.Exists(AppPaths.CreatorMode))
		{
			File.Delete(AppPaths.CreatorMode);
		}
	}

	public static void DeleteLocalData()
	{
		SqliteConnection.ClearAllPools();
		if (Directory.Exists(AppPaths.Root))
		{
			Directory.Delete(AppPaths.Root, recursive: true);
		}
	}
}
