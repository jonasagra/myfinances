using MyFinances.Core.Common;

namespace MyFinances.Core.Labor;

public sealed record OvertimeInput(decimal Salary, int MonthlyHours, YearMonth BankDueDate, int RegularDays, int RegularOvertimeMinutesPerDay, int HolidaysWorked, int HolidayMinutes, int HolidayOvertimeMinutesPerDay, decimal CurrentMonthInssBase, int DailyMinutesLimit = 105);
