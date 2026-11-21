using System;

namespace MyFinances.App.ViewModels;

public sealed record PreviewRow(int Number, DateOnly DueDate, decimal Principal, decimal Interest, decimal Fee, decimal Total, decimal Balance, string Breakdown);
