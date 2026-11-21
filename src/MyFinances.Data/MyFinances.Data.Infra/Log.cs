using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace MyFinances.Data.Infra;

public static class Log
{
	private static readonly object Lock = new object();

	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
	private static Regex Digits()
	{
		return _003CRegexGenerator_g_003EF7671E4A10DE00986434E86130BAC0F2D1F329F52C68A46B65364C745353F8809__Digits_0.Instance;
	}

	public static string Clear(string? text)
	{
		return Digits().Replace(text ?? "", "[omitido]");
	}

	public static void Info(string message)
	{
		Write("INFO", message);
	}

	public static void Notice(string message)
	{
		Write("AVISO", message);
	}

	public static void Error(string context, Exception ex)
	{
		Exception inner = ex;
		List<string> kinds = new List<string>();
		while (inner != null)
		{
			kinds.Add(inner.GetType().Name + ": " + Clear(inner.Message));
			inner = inner.InnerException;
		}
		Write("ERRO", context + " | " + string.Join(" <- ", kinds) + Environment.NewLine + ex.StackTrace);
	}

	private static void Write(string level, string message)
	{
		try
		{
			Directory.CreateDirectory(AppPaths.Logs);
			string file = Path.Combine(AppPaths.Logs, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
			lock (Lock)
			{
				File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {Clear(message)}{Environment.NewLine}");
			}
		}
		catch
		{
		}
	}

	public static void PruneOld(int days = 30)
	{
		try
		{
			if (!Directory.Exists(AppPaths.Logs))
			{
				return;
			}
			string[] files = Directory.GetFiles(AppPaths.Logs, "*.log");
			foreach (string f in files)
			{
				if (File.GetLastWriteTime(f) < DateTime.Now.AddDays(-days))
				{
					File.Delete(f);
				}
			}
		}
		catch
		{
		}
	}
}
