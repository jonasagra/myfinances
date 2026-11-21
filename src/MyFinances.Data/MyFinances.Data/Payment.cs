using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Payment
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid LoanId { get; set; }

	public Loan? Loan { get; set; }

	public DateTime Timestamp { get; set; }

	public decimal Amount { get; set; }

	[MaxLength(30)]
	public string PaymentMethod { get; set; } = "Pix";

	[Encrypted]
	public string? Note { get; set; }

	public bool ExtraAmortization { get; set; }

	public bool Reversed { get; set; }

	public DateTime? ReversedAt { get; set; }

	[Encrypted]
	public string? ReversalReason { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
