namespace MyFinances.App.ViewModels;

public sealed record PostedGroup(string Group, int Rows, string Months)
{
	public string Text => $"{Group} — {Rows} linha(s) em {Months}";
}
