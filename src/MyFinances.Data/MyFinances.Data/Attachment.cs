using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinances.Data;

public class Attachment
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid ClientId { get; set; }

	public Client? Client { get; set; }

	public AttachmentKind Kind { get; set; }

	[Encrypted]
	public string OriginalName { get; set; } = "";

	[MaxLength(10)]
	public string Extension { get; set; } = "";

	public long SizeBytes { get; set; }

	[MaxLength(64)]
	public string Sha256 { get; set; } = "";

	[MaxLength(200)]
	public string File { get; set; } = "";

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public DateTime? ReplacedAt { get; set; }
}
