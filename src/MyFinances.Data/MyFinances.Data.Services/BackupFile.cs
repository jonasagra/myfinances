namespace MyFinances.Data.Services;

public sealed record BackupFile(string Name, string Sha256, long Bytes);
