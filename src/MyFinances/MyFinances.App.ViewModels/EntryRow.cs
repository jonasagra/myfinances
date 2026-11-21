using System;

namespace MyFinances.App.ViewModels;

public sealed record EntryRow(Guid? Id, string Description, decimal Amount, string? Group, bool Fixed, string Origin)
{
	public bool CanEdit
	{
		get
		{
			if (!Fixed)
			{
				return Id.HasValue;
			}
			return false;
		}
	}

	public string? Hint
	{
		get
		{
			if (!Fixed)
			{
				if (!Recurring)
				{
					return null;
				}
				return "Lançamento fixo: se repete todo mês. Ao editar ou excluir, você escolhe se vale só para este mês ou daqui pra frente.";
			}
			return "Calculado pelo app a partir do salário e das faltas do mês.";
		}
	}

	public bool Recurring { get; init; }

	public bool ShowLock
	{
		get
		{
			if (!Fixed)
			{
				return Recurring;
			}
			return true;
		}
	}

	public int Twins { get; init; } = 1;

	public bool HasTwins
	{
		get
		{
			if (CanEdit)
			{
				return Twins > 1;
			}
			return false;
		}
	}

	public string MergeTwinsText => $"Mesclar os {Twins} \"{Description}\" em um só";

	public bool IsCut { get; init; }

	public bool IsAdjustment { get; init; }
}
