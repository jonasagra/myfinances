using System;
using MyFinances.Core.Common;

namespace MyFinances.Data.Services;

public sealed class ClientData
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string FullName { get; set; } = "";

	public string Cpf { get; set; } = "";

	public DateOnly? BirthDate { get; set; }

	public decimal? DeclaredIncome { get; set; }

	public string? Phone { get; set; }

	public string? Email { get; set; }

	public string? SocialMedia { get; set; }

	public string? ZipCode { get; set; }

	public string? Street { get; set; }

	public string? Number { get; set; }

	public string? AddressLine2 { get; set; }

	public string? District { get; set; }

	public string? City { get; set; }

	public string? StateCode { get; set; }

	public string? Notes { get; set; }

	public bool Active { get; set; } = true;

	public DateTime CreatedAt { get; set; }

	public int LoanCount { get; set; }

	public int AttachmentCount { get; set; }

	public string CpfFormatted => MyFinances.Core.Common.Cpf.Format(Cpf);

	public string CpfMasked => MyFinances.Core.Common.Cpf.Mask(Cpf);
}
