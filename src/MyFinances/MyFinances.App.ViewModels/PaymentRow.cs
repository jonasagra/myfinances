using System;

namespace MyFinances.App.ViewModels;

public sealed record PaymentRow(Guid Id, DateTime Timestamp, decimal Amount, string PaymentMethod, bool Reversed, string Status, string Breakdown, string? Note);
