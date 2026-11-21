using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using MyFinances.Data.Infra;
using Microsoft.Data.Sqlite;

namespace MyFinances.Data.Services;

public sealed class BackupService(SettingsService config)
{
	public const int Layout = 1;

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static string AppVersion => typeof(BackupService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

	public string CreatePortable(string destination, string password)
	{
		if (string.IsNullOrEmpty(password) || password.Length < 8)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("A senha do backup precisa ter pelo menos 8 caracteres."));
		}
		Vault vault = Vault.Current ?? throw new InvalidOperationException("Cofre não aberto.");
		Create(destination, "portatil", vault.ExportWithPassword(password));
		Log.Info("Portable backup created.");
		return destination;
	}

	public string? CreateAutomatic(string reason = "diario")
	{
		try
		{
			if (!File.Exists(AppPaths.Database))
			{
				return null;
			}
			Directory.CreateDirectory(AppPaths.AutoBackups);
			string destination = Path.Combine(AppPaths.AutoBackups, $"auto-{DateTime.Now:yyyyMMdd-HHmmss}-{reason}.mfbak");
			Create(destination, "automatico", null);
			int keep = Math.Max(1, config.Preferences().AutoBackupsKept);
			foreach (string item in (from f in Directory.GetFiles(AppPaths.AutoBackups, "auto-*.mfbak")
				orderby f descending
				select f).Skip(keep))
			{
				File.Delete(item);
			}
			return destination;
		}
		catch (Exception ex)
		{
			Log.Error("Automatic backup", ex);
			return null;
		}
	}

	public void DailyBackupIfNeeded()
	{
		if (config.Preferences().AutoBackup)
		{
			Directory.CreateDirectory(AppPaths.AutoBackups);
			string today = $"auto-{DateTime.Now:yyyyMMdd}";
			if (!Directory.GetFiles(AppPaths.AutoBackups, today + "*.mfbak").Any())
			{
				CreateAutomatic();
			}
		}
	}

	private static void Create(string destination, string kind, Vault.KeyInBackup? key)
	{
		string tmpDir = Path.Combine(AppPaths.Temp, "bk-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tmpDir);
		try
		{
			string tmpDb = Path.Combine(tmpDir, "blackshuffle.db");
			using (SqliteConnection origin = new SqliteConnection("Data Source=" + AppPaths.Database + ";Mode=ReadOnly;Pooling=False"))
			{
				using SqliteConnection copy = new SqliteConnection("Data Source=" + tmpDb + ";Pooling=False");
				origin.Open();
				copy.Open();
				origin.BackupDatabase(copy);
			}
			SqliteConnection.ClearAllPools();
			List<BackupFile> files = new List<BackupFile>
			{
				new BackupFile("blackshuffle.db", Sha(tmpDb), new FileInfo(tmpDb).Length)
			};
			string[] attachments = (Directory.Exists(AppPaths.Attachments) ? Directory.GetFiles(AppPaths.Attachments, "*.bin") : Array.Empty<string>());
			string[] array = attachments;
			foreach (string a in array)
			{
				files.Add(new BackupFile("anexos/" + Path.GetFileName(a), Sha(a), new FileInfo(a).Length));
			}
			BackupManifest manifest = new BackupManifest(1, AppVersion, DateTime.Now, Environment.MachineName, kind, files, key);
			string tmpZip = destination + ".tmp";
			if (File.Exists(tmpZip))
			{
				File.Delete(tmpZip);
			}
			using (ZipArchive zip = ZipFile.Open(tmpZip, ZipArchiveMode.Create))
			{
				zip.CreateEntryFromFile(tmpDb, "blackshuffle.db", CompressionLevel.Optimal);
				array = attachments;
				foreach (string a2 in array)
				{
					zip.CreateEntryFromFile(a2, "anexos/" + Path.GetFileName(a2), CompressionLevel.NoCompression);
				}
				using Stream s = zip.CreateEntry("manifest.json").Open();
				JsonSerializer.Serialize(s, manifest, Json);
			}
			File.Move(tmpZip, destination, overwrite: true);
		}
		finally
		{
			try
			{
				Directory.Delete(tmpDir, recursive: true);
			}
			catch
			{
			}
		}
	}

	private static string Sha(string filePath)
	{
		using FileStream f = File.OpenRead(filePath);
		return Convert.ToHexString(SHA256.HashData(f));
	}

	public BackupInfo Verify(string path)
	{
		using ZipArchive zip = ZipFile.OpenRead(path);
		BackupManifest m;
		using (Stream s = (zip.GetEntry("manifest.json") ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Arquivo não é um backup do MyFinances (sem manifesto)."))).Open())
		{
			m = JsonSerializer.Deserialize<BackupManifest>(s, Json) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Manifesto ilegível."));
		}
		if (m.Layout > 1)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Backup feito por uma versão mais nova do MyFinances (" + m.AppVersion + "). Atualize o app."));
		}
		foreach (BackupFile a in m.Files)
		{
			ZipArchiveEntry e = zip.GetEntry(a.Name);
			if (e == null)
			{
				return new BackupInfo(path, m, IntegrityOk: false, "Falta o arquivo " + a.Name + " dentro do backup.");
			}
			using Stream s2 = e.Open();
			if (Convert.ToHexString(SHA256.HashData(s2)) != a.Sha256)
			{
				return new BackupInfo(path, m, IntegrityOk: false, "O arquivo " + a.Name + " está corrompido (SHA-256 não confere).");
			}
		}
		string tmp = Path.Combine(AppPaths.Temp, "verify-" + Guid.NewGuid().ToString("N") + ".db");
		Directory.CreateDirectory(AppPaths.Temp);
		try
		{
			zip.GetEntry("blackshuffle.db").ExtractToFile(tmp);
			using SqliteConnection c = new SqliteConnection("Data Source=" + tmp + ";Mode=ReadOnly;Pooling=False");
			c.Open();
			using SqliteCommand cmd = c.CreateCommand();
			cmd.CommandText = "PRAGMA integrity_check;";
			if (!string.Equals(cmd.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
			{
				return new BackupInfo(path, m, IntegrityOk: false, "O banco dentro do backup falhou na verificação de integridade.");
			}
		}
		finally
		{
			SqliteConnection.ClearAllPools();
			try
			{
				File.Delete(tmp);
			}
			catch
			{
			}
		}
		return new BackupInfo(path, m, IntegrityOk: true, $"{m.Files.Count} arquivo(s) conferidos; banco íntegro.");
	}

	public string Restore(string path, string? password)
	{
		BackupInfo info = Verify(path);
		if (!info.IntegrityOk)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>(info.Detail));
		}
		byte[] key = null;
		Vault.KeyInBackup ch = info.Manifest.Key;
		if ((object)ch != null)
		{
			if (string.IsNullOrEmpty(password))
			{
				throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Digite a senha do backup."));
			}
			key = Vault.ImportWithPassword(ch, password) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Senha do backup incorreta."));
		}
		else if (!File.Exists(AppPaths.Key))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Este é um backup automático (sem senha): só restaura no mesmo usuário do Windows onde foi criado, com a chave atual."));
		}
		string safety = Path.Combine(AppPaths.Backups, $"antes-da-restauracao-{DateTime.Now:yyyyMMdd-HHmmss}");
		Directory.CreateDirectory(safety);
		SqliteConnection.ClearAllPools();
		if (File.Exists(AppPaths.Database))
		{
			File.Copy(AppPaths.Database, Path.Combine(safety, "blackshuffle.db"));
		}
		if (File.Exists(AppPaths.Key))
		{
			File.Copy(AppPaths.Key, Path.Combine(safety, "key.dat"));
		}
		if (Directory.Exists(AppPaths.Attachments))
		{
			Directory.CreateDirectory(Path.Combine(safety, "attachments"));
			string[] files = Directory.GetFiles(AppPaths.Attachments);
			foreach (string f in files)
			{
				File.Copy(f, Path.Combine(safety, "attachments", Path.GetFileName(f)));
			}
		}
		using (ZipArchive zip = ZipFile.OpenRead(path))
		{
			string newDb = AppPaths.Database + ".restaurando";
			zip.GetEntry("blackshuffle.db").ExtractToFile(newDb, overwrite: true);
			string newFolder = AppPaths.Attachments + ".restaurando";
			if (Directory.Exists(newFolder))
			{
				Directory.Delete(newFolder, recursive: true);
			}
			Directory.CreateDirectory(newFolder);
			foreach (ZipArchiveEntry e in zip.Entries.Where((ZipArchiveEntry zipArchiveEntry) => zipArchiveEntry.FullName.StartsWith("anexos/") && zipArchiveEntry.Name.Length > 0))
			{
				string destination = Path.GetFullPath(Path.Combine(newFolder, e.Name));
				if (destination.StartsWith(Path.GetFullPath(newFolder), StringComparison.OrdinalIgnoreCase))
				{
					e.ExtractToFile(destination, overwrite: true);
				}
			}
			string[] files = new string[2] { "-wal", "-shm" };
			foreach (string w in files)
			{
				if (File.Exists(AppPaths.Database + w))
				{
					File.Delete(AppPaths.Database + w);
				}
			}
			File.Move(newDb, AppPaths.Database, overwrite: true);
			if (Directory.Exists(AppPaths.Attachments))
			{
				Directory.Delete(AppPaths.Attachments, recursive: true);
			}
			Directory.Move(newFolder, AppPaths.Attachments);
		}
		if (key != null)
		{
			Vault.StoreKey(key);
		}
		Log.Info("Backup restored.");
		return safety;
	}

	public List<string> ListAutomatic()
	{
		if (!Directory.Exists(AppPaths.AutoBackups))
		{
			return new List<string>();
		}
		return (from f in Directory.GetFiles(AppPaths.AutoBackups, "*.mfbak")
			orderby f descending
			select f).ToList();
	}
}
