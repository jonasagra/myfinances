using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MyFinances.App.Infra;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class BackupViewModel : ScreenViewModel
{
	private BackupRow? _sel;

	public override string Title => "Backup";

	public ObservableCollection<BackupRow> Automatic { get; } = new ObservableCollection<BackupRow>();

	public BackupRow? Selected
	{
		get
		{
			return _sel;
		}
		set
		{
			Set(ref _sel, value, "Selected");
		}
	}

	public string DataFolder => AppPaths.Root;

	public RelayCommand CreateCommand { get; }

	public RelayCommand RestoreFileCommand { get; }

	public RelayCommand RestoreSelectedCommand { get; }

	public RelayCommand OpenFolderCommand { get; }

	public RelayCommand CreateAutoNowCommand { get; }

	public string Instructions => "COMO LEVAR PARA OUTRO COMPUTADOR\n1. Aqui: clique em \"Criar backup portátil\", defina uma senha (mínimo 8 caracteres) e salve o arquivo .mfbak (pendrive, nuvem pessoal...).\n2. No outro PC: instale o MyFinances pelo MSI e abra uma vez.\n3. Lá: Backup > \"Restaurar de um arquivo\", escolha o .mfbak e digite a senha. O app confere a integridade antes de trocar qualquer coisa.\n\nSem a senha o backup portátil não abre — guarde-a bem. Os backups automáticos (lista abaixo) só restauram neste mesmo usuário do Windows.";

	public BackupViewModel()
	{
		CreateCommand = new RelayCommand(Create);
		RestoreFileCommand = new RelayCommand(() =>
		{
			string text = Dialogs.OpenFile("Backup do MyFinances|*.mfbak;*.bsbak", "Escolha o backup");
			if (text != null)
			{
				Restore(text);
			}
		});
		RestoreSelectedCommand = new RelayCommand(() =>
		{
			if ((object)Selected != null)
			{
				Restore(Selected.Path);
			}
		}, () => (object)Selected != null);
		OpenFolderCommand = new RelayCommand(() =>
		{
			Process.Start(new ProcessStartInfo(AppPaths.Root)
			{
				UseShellExecute = true
			});
		});
		CreateAutoNowCommand = new RelayCommand(() =>
		{
			if (Svc.Backup.CreateAutomatic("manual") == null)
			{
				Dialogs.Warn("Não foi possível criar o backup (veja o log).");
			}
			else
			{
				Load();
			}
		});
	}

	public override void Load()
	{
		Automatic.Clear();
		foreach (string f in Svc.Backup.ListAutomatic())
		{
			FileInfo fi = new FileInfo(f);
			Automatic.Add(new BackupRow(f, fi.Name, fi.LastWriteTime, $"{Math.Max(1L, fi.Length / 1024)} KB"));
		}
	}

	private void Create()
	{
		string s1 = Dialogs.AskText("Senha do backup", "Crie uma senha para este backup (mínimo 8 caracteres). Ela será pedida para restaurar:", password: true);
		if (s1 == null)
		{
			return;
		}
		string s2 = Dialogs.AskText("Senha do backup", "Digite a senha de novo:", password: true);
		if (s2 == null)
		{
			return;
		}
		if (s1 != s2)
		{
			Dialogs.Warn("As senhas não conferem.");
			return;
		}
		string destination = Dialogs.SaveFile("Backup do MyFinances|*.mfbak;*.bsbak", "Salvar backup", $"myfinances-{DateTime.Now:yyyy-MM-dd}.mfbak");
		if (destination != null)
		{
			Svc.Backup.CreatePortable(destination, s1);
			BackupInfo info = Svc.Backup.Verify(destination);
			Dialogs.Info("Backup criado e conferido.\n" + info.Detail + "\n\n" + destination);
		}
	}

	private void Restore(string filePath)
	{
		BackupInfo info = Svc.Backup.Verify(filePath);
		if (!info.IntegrityOk)
		{
			Dialogs.Warn("Backup com problema:\n" + info.Detail);
			return;
		}
		BackupManifest m = info.Manifest;
		if (!Dialogs.Confirm($"Backup de {m.CreatedAt:dd/MM/yyyy HH:mm} (versão {m.AppVersion}, computador {m.Machine}, {m.Kind}).\n{info.Detail}\n\n" + "RESTAURAR vai SUBSTITUIR todos os dados atuais deste usuário por os do backup. Antes, uma cópia de segurança dos dados atuais será guardada em backups\\antes-da-restauracao-...\n\nContinuar?", "Restaurar backup"))
		{
			return;
		}
		string password = null;
		if ((object)m.Key != null)
		{
			password = Dialogs.AskText("Senha do backup", "Digite a senha deste backup:", password: true);
			if (password == null)
			{
				return;
			}
		}
		string safety = Svc.Backup.Restore(filePath, password);
		Dialogs.Info("Backup restaurado. Cópia do estado anterior em:\n" + safety + "\n\nO MyFinances vai reiniciar agora.");
		string exe = Environment.ProcessPath;
		if (exe != null)
		{
			Process.Start(new ProcessStartInfo(exe, "--restarting")
			{
				UseShellExecute = true
			});
		}
		Application.Current.Shutdown();
	}
}
