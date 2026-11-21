using System;

namespace MyFinances.App.ViewModels;

public sealed record VersionRow(int Number, DateTime Start, string Kind, string Summary, string? Reason, decimal Deduction);
