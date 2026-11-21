using System;

namespace MyFinances.Data;

public class LoanVersion
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid LoanId { get; set; }

	public Loan? Loan { get; set; }

	public int Number { get; set; }

	public DateTime Start { get; set; }

	public int Kind { get; set; }

	public string TermsJson { get; set; } = "";

	public string InstallmentsJson { get; set; } = "";

	public decimal DiscountGranted { get; set; }

	[Encrypted]
	public string? Reason { get; set; }
}
