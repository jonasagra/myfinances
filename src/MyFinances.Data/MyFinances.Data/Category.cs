using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Category
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[MaxLength(80)]
	public string Name { get; set; } = "";

	public int? Kind { get; set; }

	[MaxLength(9)]
	public string Color { get; set; } = "#10B981";

	public bool Active { get; set; } = true;
}
