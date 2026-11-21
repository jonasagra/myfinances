using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class FinanceMonth
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[MaxLength(7)]
	public string YearMonth { get; set; } = "";

	public decimal? SalarySet { get; set; }

	public decimal AbsenceDays { get; set; }

	public decimal LostDsr { get; set; }

	public decimal LateHours { get; set; }

	[Encrypted]
	public string? Note { get; set; }
}
