using System;
using System.Collections.Generic;

namespace MyFinances.Data;

public class Loan
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public int Code { get; set; }

	public Guid ClientId { get; set; }

	public Client? Client { get; set; }

	public DateOnly ContractDate { get; set; }

	public int State { get; set; }

	public DateTime? StateDate { get; set; }

	[Encrypted]
	public string? StateReason { get; set; }

	[Encrypted]
	public string? Notes { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public List<LoanVersion> Versions { get; set; } = new List<LoanVersion>();

	public List<Payment> Payments { get; set; } = new List<Payment>();
}
