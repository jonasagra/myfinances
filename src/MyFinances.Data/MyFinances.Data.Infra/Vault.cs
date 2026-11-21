using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyFinances.Data.Infra;

public sealed class Vault
{
	public sealed record KeyInBackup(string Salt, int Iterations, string Nonce, string Tag, string Encrypted);

	private const string Prefix = "v1:";

	private static readonly byte[] Entropy = "Blackshuffle.chave-mestra.v1"u8.ToArray();

	private readonly byte[] _key;

	private readonly byte[] _cpfKey;

	public const int Pbkdf2Iterations = 600000;

	public static Vault? Current { get; private set; }

	private Vault(byte[] key)
	{
		_key = key;
		_cpfKey = HMACSHA256.HashData(key, "cpf-hash"u8);
	}

	public static Vault OpenOrCreate()
	{
		AppPaths.Ensure();
		byte[] key;
		if (File.Exists(AppPaths.Key))
		{
			byte[] prot = File.ReadAllBytes(AppPaths.Key);
			try
			{
				key = ProtectedData.Unprotect(prot, Entropy, DataProtectionScope.CurrentUser);
			}
			catch (CryptographicException innerException)
			{
				throw new InvalidOperationException("Não consegui abrir a chave de proteção dos dados (chave.dat). Isso acontece quando a pasta de dados veio de outro computador ou outro usuário do Windows. Use Backup > Restaurar com o arquivo .mfbak e a senha do backup.", innerException);
			}
		}
		else
		{
			if (File.Exists(AppPaths.Database))
			{
				throw new InvalidOperationException("Existe um banco de dados mas a chave de proteção (chave.dat) sumiu. Os campos pessoais não podem ser lidos sem ela. Restaure um backup (.mfbak) ou coloque de volta o arquivo chave.dat.");
			}
			key = RandomNumberGenerator.GetBytes(32);
			StoreKey(key);
		}
		Current = new Vault(key);
		return Current;
	}

	internal static void StoreKey(byte[] key)
	{
		AppPaths.Ensure();
		byte[] prot = ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser);
		string text = AppPaths.Key + ".tmp";
		File.WriteAllBytes(text, prot);
		File.Move(text, AppPaths.Key, overwrite: true);
	}

	internal byte[] KeyCopy()
	{
		return (byte[])_key.Clone();
	}

	public string? Encrypt(string? text)
	{
		if (text == null)
		{
			return null;
		}
		if (text.Length == 0)
		{
			return "";
		}
		return "v1:" + Convert.ToBase64String(EncryptBytes(Encoding.UTF8.GetBytes(text)));
	}

	public string? Decrypt(string? amount)
	{
		if (amount == null)
		{
			return null;
		}
		if (amount.Length == 0)
		{
			return "";
		}
		if (!amount.StartsWith("v1:", StringComparison.Ordinal))
		{
			return amount;
		}
		Encoding uTF = Encoding.UTF8;
		int length = "v1:".Length;
		return uTF.GetString(DecryptBytes(Convert.FromBase64String(amount.Substring(length, amount.Length - length))));
	}

	public byte[] EncryptBytes(byte[] data)
	{
		byte[] nonce = RandomNumberGenerator.GetBytes(12);
		byte[] tag = new byte[16];
		byte[] exitDate = new byte[data.Length];
		using (AesGcm aes = new AesGcm(_key, 16))
		{
			aes.Encrypt(nonce, data, exitDate, tag);
		}
		byte[] r = new byte[28 + exitDate.Length];
		nonce.CopyTo(r, 0);
		tag.CopyTo(r, 12);
		exitDate.CopyTo(r, 28);
		return r;
	}

	public byte[] DecryptBytes(byte[] package)
	{
		if (package.Length < 28)
		{
			throw new CryptographicException("Dado cifrado inválido.");
		}
		Span<byte> nonce = package.AsSpan(0, 12);
		Span<byte> tag = package.AsSpan(12, 16);
		Span<byte> enc = package.AsSpan(28);
		byte[] exitDate = new byte[enc.Length];
		using AesGcm aes = new AesGcm(_key, 16);
		aes.Decrypt(nonce, enc, tag, exitDate);
		return exitDate;
	}

	public string HashCpf(string cpfDigits)
	{
		return Convert.ToHexString(HMACSHA256.HashData(_cpfKey, Encoding.ASCII.GetBytes(cpfDigits)));
	}

	public KeyInBackup ExportWithPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);
		byte[] nonce = RandomNumberGenerator.GetBytes(12);
		byte[] tag = new byte[16];
		byte[] enc = new byte[32];
		using (AesGcm aes = new AesGcm(key, 16))
		{
			aes.Encrypt(nonce, _key, enc, tag);
		}
		return new KeyInBackup(Convert.ToBase64String(salt), 600000, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(enc));
	}

	public static byte[]? ImportWithPassword(KeyInBackup c, string password)
	{
		try
		{
			byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(c.Salt), c.Iterations, HashAlgorithmName.SHA256, 32);
			byte[] r = new byte[32];
			using AesGcm aes = new AesGcm(key, 16);
			aes.Decrypt(Convert.FromBase64String(c.Nonce), Convert.FromBase64String(c.Encrypted), Convert.FromBase64String(c.Tag), r);
			return r;
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	internal static Vault WithKey(byte[] key)
	{
		return new Vault(key);
	}
}
