using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class AuditEntry
{
	public long Id { get; set; }

	public DateTime Timestamp { get; set; } = DateTime.Now;

	[MaxLength(40)]
	public string Entity { get; set; } = "";

	public Guid? EntityId { get; set; }

	[MaxLength(40)]
	public string Action { get; set; } = "";

	[MaxLength(300)]
	public string Summary { get; set; } = "";
}
