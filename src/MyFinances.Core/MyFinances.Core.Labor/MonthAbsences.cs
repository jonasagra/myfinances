namespace MyFinances.Core.Labor;

public sealed record MonthAbsences(decimal AbsenceDays = 0m, decimal LostDsr = 0m, decimal LateHours = 0m)
{
	public static MonthAbsences None { get; } = new MonthAbsences();

	public bool Empty
	{
		get
		{
			if (AbsenceDays == 0m && LostDsr == 0m)
			{
				return LateHours == 0m;
			}
			return false;
		}
	}
}
