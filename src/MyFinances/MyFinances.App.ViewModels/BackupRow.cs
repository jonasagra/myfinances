using System;

namespace MyFinances.App.ViewModels;

public sealed record BackupRow(string Path, string Name, DateTime Data, string Size);
