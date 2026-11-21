using MyFinances.App.Infra;

namespace MyFinances.App.ViewModels;

public sealed class BandVM : ObservableObject
{
	private string _until = "";

	private string _pct = "";

	private string _deduct = "";

	public string Until
	{
		get
		{
			return _until;
		}
		set
		{
			Set(ref _until, value, "Until");
		}
	}

	public string Pct
	{
		get
		{
			return _pct;
		}
		set
		{
			Set(ref _pct, value, "Pct");
		}
	}

	public string Deduct
	{
		get
		{
			return _deduct;
		}
		set
		{
			Set(ref _deduct, value, "Deduct");
		}
	}
}
