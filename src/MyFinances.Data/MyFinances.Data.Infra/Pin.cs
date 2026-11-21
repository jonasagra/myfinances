using System;
using System.Security.Cryptography;

namespace MyFinances.Data.Infra;

public static class Pin
{
	public static string GenerateHash(string pin)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] h = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 200000, HashAlgorithmName.SHA256, 32);
		return "pbkdf2$200000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(h);
	}

	public static bool Check(string pin, string? hash)
	{
		if (string.IsNullOrEmpty(hash))
		{
			return true;
		}
		string[] p = hash.Split('$');
		if (p.Length != 4 || !int.TryParse(p[1], out var it))
		{
			return false;
		}
		return CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(pin, Convert.FromBase64String(p[2]), it, HashAlgorithmName.SHA256, 32), Convert.FromBase64String(p[3]));
	}
}
