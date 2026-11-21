namespace MyFinances.Core.Labor;

public sealed record PayslipLine(string Code, string Description, decimal Reference, decimal Earning, decimal Deduction)
{
	public decimal Amount
	{
		get
		{
			if (!(Earning != 0m))
			{
				return Deduction;
			}
			return Earning;
		}
	}

	public PayslipLineKind Kind => Payslip.Classify(this);

	public string FriendlyName => Payslip.Friendly(Description);
}
