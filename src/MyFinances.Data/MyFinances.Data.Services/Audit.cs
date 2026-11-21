using System;

namespace MyFinances.Data.Services;

public static class Audit
{
	public static void Record(AppDbContext db, string entity, Guid? id, string action, string summary = "")
	{
		db.AuditEntries.Add(new AuditEntry
		{
			Entity = entity,
			EntityId = id,
			Action = action,
			Summary = ((summary.Length > 300) ? summary.Substring(0, 300) : summary)
		});
	}
}
