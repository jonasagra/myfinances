using System;

namespace MyFinances.Core.Labor;

public sealed record PayrollLoanInput(decimal ReleasedAmount, decimal MonthlyRatePct, int Installments, DateOnly ContractDate, DateOnly FirstDueDate, decimal GrossPay, decimal MandatoryDeductions, decimal CurrentPayrollLoanInstallments, decimal IofAmount = 0m, decimal FixedFees = 0m, decimal ContractInstallment = 0m, string Description = "Empréstimo consignado (CLT)");
