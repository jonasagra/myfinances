namespace MyFinances.App.ViewModels;

public sealed record PayslipCheckRow(string Name, decimal Payslip, decimal App)
{
	public bool Matches => Payslip == App;
}
