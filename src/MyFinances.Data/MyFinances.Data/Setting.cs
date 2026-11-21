using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Setting
{
	[Key]
	[MaxLength(100)]
	public string Key { get; set; } = "";

	public string Amount { get; set; } = "";

	public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
