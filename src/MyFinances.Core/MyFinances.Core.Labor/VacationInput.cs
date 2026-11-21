using System;

namespace MyFinances.Core.Labor;

public sealed record VacationInput(DateOnly Start, int Days, decimal Salary, decimal OvertimeAverage, decimal MonthlyPayrollLoan, bool ReserveSalary, decimal TransportVoucherMonth1, decimal TransportVoucherMonth2);
