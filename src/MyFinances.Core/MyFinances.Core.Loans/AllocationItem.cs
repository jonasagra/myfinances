namespace MyFinances.Core.Loans;

public sealed record AllocationItem(int Version, int Installment, Component Component, decimal Amount);
