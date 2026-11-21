namespace MyFinances.Data.Services;

public sealed record BackupInfo(string Path, BackupManifest Manifest, bool IntegrityOk, string Detail);
