using System;
using System.IO;

namespace MyFinances.Data.Infra;

public static class AppPaths
{
	public static string Root { get; private set; } = Default();

	public static string Database => Path.Combine(Root, "myfinances.db");

	public static string Key => Path.Combine(Root, "key.dat");

	public static string Attachments => Path.Combine(Root, "attachments");

	public static string Backups => Path.Combine(Root, "backups");

	public static string AutoBackups => Path.Combine(Backups, "auto");

	public static string Logs => Path.Combine(Root, "logs");

	public static string Temp => Path.Combine(Root, "temp");

	public static string CreatorMode => Path.Combine(Root, "creator-mode.txt");

	public static string LocalConfig => Path.Combine(Root, "local.json");

	public static string ConnectionString => "Data Source=" + Database + ";Foreign Keys=True;Pooling=False";

	private static string Default()
	{
		string env = Environment.GetEnvironmentVariable("MYFINANCES_DATA");
		if (!string.IsNullOrWhiteSpace(env))
		{
			return env;
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyFinances");
	}

	public static void SetRoot(string folder)
	{
		Root = folder;
	}

	public static void Ensure()
	{
		string[] array = new string[6] { Root, Attachments, Backups, AutoBackups, Logs, Temp };
		for (int i = 0; i < array.Length; i++)
		{
			Directory.CreateDirectory(array[i]);
		}
	}
}
