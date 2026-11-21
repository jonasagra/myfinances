using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Client
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[Encrypted]
	public string FullName { get; set; } = "";

	[Encrypted]
	public string Cpf { get; set; } = "";

	[MaxLength(64)]
	public string CpfHash { get; set; } = "";

	[Encrypted]
	public string? BirthDate { get; set; }

	[Encrypted]
	public string? DeclaredIncome { get; set; }

	[Encrypted]
	public string? Phone { get; set; }

	[Encrypted]
	public string? Email { get; set; }

	[Encrypted]
	public string? SocialMedia { get; set; }

	[Encrypted]
	public string? ZipCode { get; set; }

	[Encrypted]
	public string? Street { get; set; }

	[Encrypted]
	public string? Number { get; set; }

	[Encrypted]
	public string? AddressLine2 { get; set; }

	[Encrypted]
	public string? District { get; set; }

	[Encrypted]
	public string? City { get; set; }

	[MaxLength(2)]
	public string? StateCode { get; set; }

	[Encrypted]
	public string? Notes { get; set; }

	public bool Active { get; set; } = true;

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	public List<Attachment> Attachments { get; set; } = new List<Attachment>();

	public List<Loan> Loans { get; set; } = new List<Loan>();
}
