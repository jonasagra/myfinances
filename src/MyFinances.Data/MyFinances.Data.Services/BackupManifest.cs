using System;
using System.Collections.Generic;
using MyFinances.Data.Infra;

namespace MyFinances.Data.Services;

public sealed record BackupManifest(int Layout, string AppVersion, DateTime CreatedAt, string Machine, string Kind, List<BackupFile> Files, Vault.KeyInBackup? Key);
