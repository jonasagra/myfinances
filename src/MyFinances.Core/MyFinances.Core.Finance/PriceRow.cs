namespace MyFinances.Core.Finance;

public sealed record PriceRow(int Number, decimal Installment, decimal Interest, decimal Amortization, decimal Balance);
