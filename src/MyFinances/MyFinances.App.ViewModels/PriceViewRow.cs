namespace MyFinances.App.ViewModels;

public sealed record PriceViewRow(int Number, string Month, decimal Installment, decimal Interest, decimal Amortization, decimal Balance, bool Paid);
