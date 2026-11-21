namespace MyFinances.App.Infra;

public abstract class ScreenViewModel : ObservableObject
{
	public abstract string Title { get; }

	public virtual void Load()
	{
	}
}
