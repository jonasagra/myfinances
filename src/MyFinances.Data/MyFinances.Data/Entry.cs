using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Entry
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[MaxLength(7)]
	public string YearMonth { get; set; } = "";

	public int Kind { get; set; }

	[Encrypted]
	public string Description { get; set; } = "";

	public decimal Amount { get; set; }

	public Guid? CategoryId { get; set; }

	public Category? Category { get; set; }

	public EntryOrigin Origin { get; set; }

	[MaxLength(120)]
	public string? Group { get; set; }

	[MaxLength(10)]
	public string? Installment { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
