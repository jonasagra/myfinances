using System.Linq;

namespace MyFinances.Core.Common;

public static class Cpf
{
	public static string Normalize(string? cpf)
	{
		return new string((cpf ?? "").Where(char.IsDigit).ToArray());
	}

	public static bool Validate(string? cpf)
	{
		string d = Normalize(cpf);
		if (d.Length != 11)
		{
			return false;
		}
		if (d.Distinct().Count() == 1)
		{
			return false;
		}
		if (Digit(9) == d[9] - 48)
		{
			return Digit(10) == d[10] - 48;
		}
		return false;
		int Digit(int size)
		{
			int sum = 0;
			for (int i = 0; i < size; i++)
			{
				sum += (d[i] - 48) * (size + 1 - i);
			}
			int rest = sum % 11;
			if (rest >= 2)
			{
				return 11 - rest;
			}
			return 0;
		}
	}

	public static string Format(string? cpf)
	{
		string d = Normalize(cpf);
		if (d.Length != 11)
		{
			return cpf ?? "";
		}
		return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}";
	}

	public static string Mask(string? cpf)
	{
		string d = Normalize(cpf);
		if (d.Length != 11)
		{
			return "***";
		}
		return "***.***." + d.Substring(6, 3) + "-" + d.Substring(9, 2);
	}
}
