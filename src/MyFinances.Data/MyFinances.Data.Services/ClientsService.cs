using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MyFinances.Core.Common;
using MyFinances.Data.Infra;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data.Services;

public sealed class ClientsService(SettingsService config)
{
	public static readonly string[] AllowedExtensions = new string[6] { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic" };

	public static List<string> Validate(ClientData c)
	{
		List<string> e = new List<string>();
		string name = (c.FullName ?? "").Trim();
		if (name.Length < 3)
		{
			e.Add("Informe o nome completo.");
		}
		else if (!name.Contains(' '))
		{
			e.Add("Informe nome e sobrenome.");
		}
		if (name.Length > 150)
		{
			e.Add("Nome muito longo.");
		}
		if (!Cpf.Validate(c.Cpf))
		{
			e.Add("CPF inválido: confira os 11 dígitos.");
		}
		DateOnly? birthDate = c.BirthDate;
		if (birthDate.HasValue)
		{
			DateOnly dn = birthDate.GetValueOrDefault();
			if (dn > DateOnly.FromDateTime(DateTime.Today))
			{
				e.Add("Data de nascimento no futuro.");
			}
			else if (dn < new DateOnly(1900, 1, 1))
			{
				e.Add("Data de nascimento inválida.");
			}
			else if (dn > DateOnly.FromDateTime(DateTime.Today).AddYears(-18))
			{
				e.Add("O cliente precisa ter 18 anos ou mais para contratar empréstimo.");
			}
		}
		if (c.DeclaredIncome < (decimal?)0m)
		{
			e.Add("Renda não pode ser negativa.");
		}
		if (!string.IsNullOrWhiteSpace(c.Email) && !c.Email.Contains('@'))
		{
			e.Add("E-mail inválido.");
		}
		string phone = new string((c.Phone ?? "").Where(char.IsDigit).ToArray());
		bool flag = phone.Length > 0;
		if (flag)
		{
			int length = phone.Length;
			bool flag2 = ((length < 10 || length > 13) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			e.Add("Telefone: use DDD + número (10 ou 11 dígitos).");
		}
		string zip = new string((c.ZipCode ?? "").Where(char.IsDigit).ToArray());
		if (zip.Length > 0 && zip.Length != 8)
		{
			e.Add("CEP precisa ter 8 dígitos.");
		}
		if (!string.IsNullOrWhiteSpace(c.StateCode) && c.StateCode.Trim().Length != 2)
		{
			e.Add("UF: duas letras (ex.: PB).");
		}
		return e;
	}

	public List<ClientData> ListAll(bool includeInactive = false)
	{
		using AppDbContext db = AppDbContext.Create();
		IQueryable<Client> q = db.Clients.AsNoTracking();
		if (!includeInactive)
		{
			q = q.Where((Client c) => c.Active);
		}
		return q.Select((Client c) => new
		{
			c = c,
			LoanCnt = c.Loans.Count,
			AttCnt = c.Attachments.Count
		}).ToList().Select(x =>
		{
			ClientData clientData = ToData(x.c);
			clientData.LoanCount = x.LoanCnt;
			clientData.AttachmentCount = x.AttCnt;
			return clientData;
		})
			.OrderBy((ClientData c) => c.FullName, StringComparer.Create(Money.PtBr, ignoreCase: true))
			.ToList();
	}

	public ClientData? Get(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		Client c = db.Clients.AsNoTracking().FirstOrDefault((Client x) => x.Id == id);
		return (c == null) ? null : ToData(c);
	}

	private static ClientData ToData(Client c)
	{
		DateOnly dn;
		decimal r;
		return new ClientData
		{
			Id = c.Id,
			FullName = c.FullName,
			Cpf = c.Cpf,
			BirthDate = (DateOnly.TryParseExact(c.BirthDate, "yyyy-MM-dd", out dn) ? new DateOnly?(dn) : ((DateOnly?)null)),
			DeclaredIncome = (decimal.TryParse(c.DeclaredIncome, NumberStyles.Number, CultureInfo.InvariantCulture, out r) ? new decimal?(r) : ((decimal?)null)),
			Phone = c.Phone,
			Email = c.Email,
			SocialMedia = c.SocialMedia,
			ZipCode = c.ZipCode,
			Street = c.Street,
			Number = c.Number,
			AddressLine2 = c.AddressLine2,
			District = c.District,
			City = c.City,
			StateCode = c.StateCode,
			Notes = c.Notes,
			Active = c.Active,
			CreatedAt = c.CreatedAt
		};
	}

	public void Save(ClientData d)
	{
		List<string> errors = Validate(d);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		Vault vault = Vault.Current ?? throw new InvalidOperationException("Cofre não aberto.");
		string cpf = Cpf.Normalize(d.Cpf);
		string hash = vault.HashCpf(cpf);
		using AppDbContext db = AppDbContext.Create();
		if (db.Clients.Any((Client client) => client.CpfHash == hash && client.Id != d.Id))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Já existe um cliente com esse CPF."));
		}
		Client c = db.Clients.FirstOrDefault((Client x) => x.Id == d.Id);
		bool newValue = c == null;
		if (c == null)
		{
			c = new Client
			{
				Id = d.Id,
				CreatedAt = DateTime.Now
			};
		}
		c.FullName = d.FullName.Trim();
		c.Cpf = cpf;
		c.CpfHash = hash;
		c.BirthDate = d.BirthDate?.ToString("yyyy-MM-dd");
		c.DeclaredIncome = d.DeclaredIncome?.ToString(CultureInfo.InvariantCulture);
		c.Phone = Empty(d.Phone);
		c.Email = Empty(d.Email);
		c.SocialMedia = Empty(d.SocialMedia);
		c.ZipCode = Empty(d.ZipCode);
		c.Street = Empty(d.Street);
		c.Number = Empty(d.Number);
		c.AddressLine2 = Empty(d.AddressLine2);
		c.District = Empty(d.District);
		c.City = Empty(d.City);
		c.StateCode = Empty(d.StateCode)?.ToUpperInvariant();
		c.Notes = Empty(d.Notes);
		c.Active = d.Active;
		c.UpdatedAt = DateTime.Now;
		if (newValue)
		{
			db.Clients.Add(c);
		}
		Audit.Record(db, "Client", c.Id, newValue ? "Create" : "Update");
		db.SaveChanges();
	}

	private static string? Empty(string? s)
	{
		if (!string.IsNullOrWhiteSpace(s))
		{
			return s.Trim();
		}
		return null;
	}

	public void Delete(Guid id)
	{
		using AppDbContext db = AppDbContext.Create();
		Client c = db.Clients.Include((Client x) => x.Attachments).Include((Client x) => x.Loans).FirstOrDefault((Client x) => x.Id == id);
		if (c == null)
		{
			return;
		}
		if (c.Loans.Count > 0)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Este cliente tem empréstimos registrados e não pode ser excluído (o histórico precisa ficar). Use \"Inativar\"."));
		}
		foreach (Attachment attachment in c.Attachments)
		{
			DeleteFile(attachment.File);
		}
		db.Attachments.RemoveRange(c.Attachments);
		db.Clients.Remove(c);
		Audit.Record(db, "Client", id, "Delete", $"{c.Attachments.Count} attachment(s) removed");
		db.SaveChanges();
	}

	public List<Attachment> Attachments(Guid clientId)
	{
		using AppDbContext db = AppDbContext.Create();
		return (from a in db.Attachments.AsNoTracking()
			where a.ClientId == clientId
			orderby a.Kind, a.CreatedAt
			select a).ToList();
	}

	public List<string> ValidateFile(string path, out byte[] content)
	{
		content = Array.Empty<byte>();
		List<string> e = new List<string>();
		if (!File.Exists(path))
		{
			e.Add("Arquivo não encontrado.");
			return e;
		}
		string ext = Path.GetExtension(path).ToLowerInvariant();
		if (!AllowedExtensions.Contains(ext))
		{
			e.Add("Tipo não permitido (" + ext + "). Aceitos: PDF, JPG, PNG, WEBP, HEIC.");
			return e;
		}
		long max = (long)config.Preferences().MaxAttachmentMb * 1024L * 1024;
		long size = new FileInfo(path).Length;
		if (size == 0L)
		{
			e.Add("O arquivo está vazio.");
			return e;
		}
		if (size > max)
		{
			e.Add($"Arquivo grande demais ({size / 1024 / 1024} MB). Máximo: {max / 1024 / 1024} MB.");
			return e;
		}
		content = File.ReadAllBytes(path);
		if (!SignatureMatches(ext, content))
		{
			e.Add("O conteúdo do arquivo não corresponde à extensão (arquivo corrompido ou renomeado).");
		}
		return e;
	}

	public static bool SignatureMatches(string ext, byte[] b)
	{
		switch (ext)
		{
		case ".pdf":
			return Starts(new byte[4] { 37, 80, 68, 70 });
		case ".jpg":
		case ".jpeg":
			return Starts(new byte[3] { 255, 216, 255 });
		case ".png":
			return Starts(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
		case ".webp":
			return Starts(new byte[4] { 82, 73, 70, 70 }) && b.Length > 12 && b[8] == 87 && b[9] == 69 && b[10] == 66 && b[11] == 80;
		case ".heic":
			return b.Length > 12 && b[4] == 102 && b[5] == 116 && b[6] == 121 && b[7] == 112;
		default:
			return false;
		}
		bool Starts(params byte[] s)
		{
			if (b.Length >= s.Length)
			{
				return ((ReadOnlySpan<byte>)b.AsSpan(0, s.Length)).SequenceEqual((ReadOnlySpan<byte>)s);
			}
			return false;
		}
	}

	public Attachment AddAttachment(Guid clientId, AttachmentKind kind, string path)
	{
		List<string> errors = ValidateFile(path, out byte[] bytes);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		Vault vault = Vault.Current;
		using AppDbContext db = AppDbContext.Create();
		if (!db.Clients.Any((Client c) => c.Id == clientId))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Salve o cliente antes de anexar documentos."));
		}
		Attachment a = new Attachment
		{
			ClientId = clientId,
			Kind = kind,
			OriginalName = Path.GetFileName(path),
			Extension = Path.GetExtension(path).ToLowerInvariant(),
			SizeBytes = bytes.Length,
			Sha256 = Convert.ToHexString(SHA256.HashData(bytes))
		};
		a.File = $"anexos/{a.Id:N}.bin";
		StoreEncrypted(a.File, vault.EncryptBytes(bytes));
		db.Attachments.Add(a);
		Audit.Record(db, "Attachment", a.Id, "Add", $"kind {kind}, {bytes.Length / 1024} KB");
		db.SaveChanges();
		return a;
	}

	public void ReplaceAttachment(Guid attachmentId, string path)
	{
		List<string> errors = ValidateFile(path, out byte[] bytes);
		if (errors.Count > 0)
		{
			throw new ValidationException(errors);
		}
		using AppDbContext db = AppDbContext.Create();
		Attachment a = db.Attachments.Find(attachmentId) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Anexo não encontrado."));
		string newFile = $"anexos/{Guid.NewGuid():N}.bin";
		StoreEncrypted(newFile, Vault.Current.EncryptBytes(bytes));
		string file = a.File;
		a.File = newFile;
		a.OriginalName = Path.GetFileName(path);
		a.Extension = Path.GetExtension(path).ToLowerInvariant();
		a.SizeBytes = bytes.Length;
		a.Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
		a.ReplacedAt = DateTime.Now;
		Audit.Record(db, "Attachment", a.Id, "Replace");
		db.SaveChanges();
		DeleteFile(file);
	}

	public void RemoveAttachment(Guid attachmentId)
	{
		using AppDbContext db = AppDbContext.Create();
		Attachment a = db.Attachments.Find(attachmentId);
		if (a != null)
		{
			db.Attachments.Remove(a);
			Audit.Record(db, "Attachment", a.Id, "Remove");
			db.SaveChanges();
			DeleteFile(a.File);
		}
	}

	public string OpenAttachment(Guid attachmentId)
	{
		using AppDbContext db = AppDbContext.Create();
		Attachment a = db.Attachments.AsNoTracking().FirstOrDefault((Attachment x) => x.Id == attachmentId) ?? throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("Anexo não encontrado."));
		string file = Path.Combine(AppPaths.Root, a.File);
		if (!File.Exists(file))
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("O arquivo do anexo sumiu da pasta de dados. Restaure um backup."));
		}
		byte[] bytes = Vault.Current.DecryptBytes(File.ReadAllBytes(file));
		if (Convert.ToHexString(SHA256.HashData(bytes)) != a.Sha256)
		{
			throw new ValidationException(new _003C_003Ez__ReadOnlySingleElementList<string>("O anexo não passou na verificação de integridade (arquivo alterado ou corrompido)."));
		}
		Directory.CreateDirectory(AppPaths.Temp);
		string text = Path.Combine(AppPaths.Temp, $"{a.Id:N}{a.Extension}");
		File.WriteAllBytes(text, bytes);
		Audit.Record(db, "Attachment", a.Id, "Open");
		db.SaveChanges();
		Process.Start(new ProcessStartInfo(text)
		{
			UseShellExecute = true
		});
		return text;
	}

	public static void ClearTemp()
	{
		try
		{
			if (!Directory.Exists(AppPaths.Temp))
			{
				return;
			}
			string[] files = Directory.GetFiles(AppPaths.Temp);
			foreach (string f in files)
			{
				try
				{
					File.Delete(f);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void StoreEncrypted(string relative, byte[] data)
	{
		string destination = Path.Combine(AppPaths.Root, relative);
		Directory.CreateDirectory(Path.GetDirectoryName(destination));
		string text = destination + ".tmp";
		File.WriteAllBytes(text, data);
		File.Move(text, destination, overwrite: true);
	}

	private static void DeleteFile(string relative)
	{
		try
		{
			string f = Path.Combine(AppPaths.Root, relative);
			if (File.Exists(f))
			{
				File.Delete(f);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Delete attachment", ex);
		}
	}

	public static string KindName(AttachmentKind t)
	{
		return t switch
		{
			AttachmentKind.IdCard => "RG", 
			AttachmentKind.CpfDocument => "CPF (documento)", 
			AttachmentKind.DriverLicense => "CNH", 
			AttachmentKind.ProofOfAddress => "Comprovante de residência", 
			AttachmentKind.ProofOfIncome => "Comprovante de renda", 
			AttachmentKind.SignedContract => "Contrato assinado", 
			_ => "Outro", 
		};
	}
}
