using System;
using System.Collections.Generic;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Labor;
using MyFinances.Data;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class EntryViewModel : ObservableObject
{
	public sealed record KindOption(EntryKind Kind, string Name, string Hint);

	private readonly Guid? _id;

	private KindOption _kind;

	private string _description;

	private string _amount;

	private string _month;

	private string _repeat = "1";

	private string _error = "";

	private bool _installments;

	private bool _isFixed;

	private readonly decimal? _original;

	private readonly bool _wasFixed;

	public IReadOnlyList<KindOption> Kinds { get; } = new _003C_003Ez__ReadOnlyArray<KindOption>(new KindOption[4]
	{
		new KindOption(EntryKind.Expense, "Despesa", "Vai para Despesas e gastos."),
		new KindOption(EntryKind.Inflow, "Entrada na conta", "Dinheiro que entra na conta fora do salário (venda, pix, reembolso...)."),
		new KindOption(EntryKind.Earning, "Provento no salário", "Soma no salário bruto (comissão, hora extra...). Entra no INSS."),
		new KindOption(EntryKind.Deduction, "Desconto no salário", "Sai do salário antes de cair na conta (ex.: parcela de consignado).")
	});

	public bool Edit
	{
		get
		{
			Guid? id = _id;
			return id.HasValue;
		}
	}

	public string WindowTitle
	{
		get
		{
			if (!Edit)
			{
				return "Novo lançamento";
			}
			return "Editar lançamento";
		}
	}

	public KindOption Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			if (Set(ref _kind, value, "Kind"))
			{
				OnPropertyChanged("Hint");
			}
		}
	}

	public string Hint => _kind.Hint + (Installments ? " Será gravado como \"Nome 1/N\", \"Nome 2/N\"..." : "");

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			Set(ref _description, value, "Description");
		}
	}

	public string Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (Set(ref _amount, value, "Amount"))
			{
				OnPropertyChanged("AmountResult");
			}
		}
	}

	public string AmountResult
	{
		get
		{
			if (!MoneyExpression.IsCalculation(_amount, _original) || !MoneyExpression.TryEvaluate(_amount, _original, out var v))
			{
				return "";
			}
			return "= " + Money.Fmt(v);
		}
	}

	public string Month
	{
		get
		{
			return _month;
		}
		set
		{
			Set(ref _month, value, "Month");
		}
	}

	public string Repeat
	{
		get
		{
			return _repeat;
		}
		set
		{
			Set(ref _repeat, value, "Repeat");
		}
	}

	public bool Installments
	{
		get
		{
			return _installments;
		}
		set
		{
			if (Set(ref _installments, value, "Installments"))
			{
				OnPropertyChanged("Hint");
			}
		}
	}

	public bool IsFixed
	{
		get
		{
			return _isFixed;
		}
		set
		{
			if (Set(ref _isFixed, value, "IsFixed"))
			{
				OnPropertyChanged("ShowRepeat");
			}
		}
	}

	public bool ShowRepeat
	{
		get
		{
			if (!Edit)
			{
				return !_isFixed;
			}
			return false;
		}
	}

	public FixedEntries.Change? FixedChange { get; private set; }

	public string? FixedChangeText { get; private set; }

	public string Error
	{
		get
		{
			return _error;
		}
		set
		{
			Set(ref _error, value, "Error");
		}
	}

	public EntryViewModel(EntryEdit e)
	{
		_id = e.Id;
		_kind = Kinds.First((KindOption t) => t.Kind == e.Kind);
		_description = e.Description;
		decimal? amount = e.Amount;
		string amount2;
		if (amount.HasValue)
		{
			decimal v = amount.GetValueOrDefault();
			amount2 = Money.Num(v);
		}
		else
		{
			amount2 = "";
		}
		_amount = amount2;
		_original = e.Amount;
		_month = Input.Month(e.Month);
		Guid? id = e.Id;
		if (id.HasValue)
		{
			Guid id2 = id.GetValueOrDefault();
			Entry current = Svc.Finance.Get(id2);
			if (current != null)
			{
				_wasFixed = (_isFixed = FixedEntries.IsFixed(current));
			}
		}
	}

	public bool Save()
	{
		try
		{
			List<string> errors = new List<string>();
			decimal amount = Input.Dec(Amount, "Valor", errors, required: true, allowNegative: false, _original);
			YearMonth month = Input.Month(Month, "Mês", errors);
			int rep = ((Edit || IsFixed) ? 1 : Input.Int(Repeat, "Repetir por", errors, 1, 240));
			Input.Check(errors);
			if (!Edit && IsFixed)
			{
				FixedEntries.Create(Kind.Kind, Description, Money.R2(amount), month);
				return true;
			}
			YearMonth? startEarlier = null;
			if (Edit && IsFixed && !_wasFixed && month > YearMonth.Today)
			{
				string answer = Dialogs.AskText("Lançamento fixo", $"Este lançamento está em {month.SheetName} e vai se repetir daqui pra frente.\nSe ele também vale nos meses anteriores, digite o mês em que começa (mm/aaaa) — por exemplo {Input.Month(YearMonth.Today)}.\nPara começar em {month.SheetName}, deixe como está.", password: false, Input.Month(month));
				if (answer == null)
				{
					return false;
				}
				List<string> e2 = new List<string>();
				YearMonth chosen = Input.Month(answer, "Mês de início", e2);
				Input.Check(e2);
				if (chosen < month)
				{
					startEarlier = chosen;
				}
			}
			if (Edit && !IsFixed && _wasFixed && !Dialogs.Confirm($"\"{Description.Trim()}\" vai deixar de ser fixo.\n\nEle fica só em {month.SheetName} e sai dos meses seguintes. Continuar?", "Lançamento fixo"))
			{
				return false;
			}
			if (Edit || (rep == 1 && !Installments))
			{
				Svc.Finance.Save(new Entry
				{
					Id = (_id ?? Guid.NewGuid()),
					YearMonth = month.Key,
					Kind = (int)Kind.Kind,
					Description = Description.Trim(),
					Amount = Money.R2(amount),
					Origin = EntryOrigin.Manual
				});
			}
			else
			{
				Svc.Finance.PostRepeated(Kind.Kind, Description, Money.R2(amount), month, rep, Installments, null);
			}
			if (Edit)
			{
				Guid? id = _id;
				if (id.HasValue)
				{
					Guid id2 = id.GetValueOrDefault();
					if (IsFixed != _wasFixed)
					{
						FixedChange = (IsFixed ? FixedEntries.MakeFixed(id2, startEarlier) : FixedEntries.MakeNormal(id2));
						FixedChangeText = (IsFixed ? ("\"" + Description.Trim() + "\" virou lançamento fixo") : ("\"" + Description.Trim() + "\" deixou de ser fixo"));
					}
				}
			}
			return true;
		}
		catch (ValidationException ex)
		{
			Error = string.Join(" ", ex.Errors);
			return false;
		}
	}
}
