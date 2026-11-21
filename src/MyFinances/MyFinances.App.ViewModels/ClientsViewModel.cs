using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Data;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;

namespace MyFinances.App.ViewModels;

public sealed class ClientsViewModel : ScreenViewModel
{
	public sealed record AttachmentKindOption(AttachmentKind Kind, string Name);

	private List<ClientData> _all = new List<ClientData>();

	private string _search = "";

	private bool _showInactive;

	private ClientData? _selected;

	private ClientData _form = new ClientData();

	private string _cpf = "";

	private string _birth = "";

	private string _income = "";

	private string _error = "";

	private AttachmentRow? _attachmentSel;

	private AttachmentKindOption _attachmentKind;

	public override string Title => "Clientes";

	public IReadOnlyList<AttachmentKindOption> AttachmentKinds { get; } = (from t in Enum.GetValues<AttachmentKind>()
		select new AttachmentKindOption(t, ClientsService.KindName(t))).ToList();

	public IReadOnlyList<string> StateCodes { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[28]
	{
		"", "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
		"MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ",
		"RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
	});

	public ObservableCollection<ClientData> ItemList { get; } = new ObservableCollection<ClientData>();

	public ObservableCollection<AttachmentRow> Attachments { get; } = new ObservableCollection<AttachmentRow>();

	public string Search
	{
		get
		{
			return _search;
		}
		set
		{
			if (Set(ref _search, value, "Search"))
			{
				ApplyFilter();
			}
		}
	}

	public bool ShowInactive
	{
		get
		{
			return _showInactive;
		}
		set
		{
			if (Set(ref _showInactive, value, "ShowInactive"))
			{
				Load();
			}
		}
	}

	public ClientData? Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (Set(ref _selected, value, "Selected") && value != null)
			{
				Edit(value);
			}
		}
	}

	public ClientData Form
	{
		get
		{
			return _form;
		}
		private set
		{
			Set(ref _form, value, "Form");
			OnPropertyChanged("IsNew");
			OnPropertyChanged("FormTitle");
		}
	}

	public bool IsNew => _all.All((ClientData c) => c.Id != _form.Id);

	public string FormTitle
	{
		get
		{
			if (!IsNew)
			{
				return "Editar cliente";
			}
			return "Novo cliente";
		}
	}

	public string Cpf
	{
		get
		{
			return _cpf;
		}
		set
		{
			if (Set(ref _cpf, value, "Cpf"))
			{
				OnPropertyChanged("CpfStatus");
			}
		}
	}

	public string CpfStatus
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Cpf))
			{
				if (!MyFinances.Core.Common.Cpf.Validate(Cpf))
				{
					return "✗ CPF inválido";
				}
				return "✓ CPF válido: " + MyFinances.Core.Common.Cpf.Format(Cpf);
			}
			return "";
		}
	}

	public string Birth
	{
		get
		{
			return _birth;
		}
		set
		{
			Set(ref _birth, value, "Birth");
		}
	}

	public string Income
	{
		get
		{
			return _income;
		}
		set
		{
			Set(ref _income, value, "Income");
		}
	}

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

	public AttachmentRow? SelectedAttachment
	{
		get
		{
			return _attachmentSel;
		}
		set
		{
			Set(ref _attachmentSel, value, "SelectedAttachment");
		}
	}

	public AttachmentKindOption AttachmentKind
	{
		get
		{
			return _attachmentKind;
		}
		set
		{
			Set(ref _attachmentKind, value, "AttachmentKind");
		}
	}

	public string AttachmentRule => $"Aceitos: PDF, JPG, PNG, WEBP, HEIC — até {Svc.Config.Preferences().MaxAttachmentMb} MB. O conteúdo é conferido (não só a extensão) e guardado cifrado.";

	public RelayCommand NewCommand { get; }

	public RelayCommand SaveCommand { get; }

	public RelayCommand DeleteCommand { get; }

	public RelayCommand ToggleActiveCommand { get; }

	public RelayCommand AddAttachmentCommand { get; }

	public RelayCommand OpenAttachmentCommand { get; }

	public RelayCommand ReplaceAttachmentCommand { get; }

	public RelayCommand RemoveAttachmentCommand { get; }

	public ClientsViewModel()
	{
		_attachmentKind = AttachmentKinds[0];
		NewCommand = new RelayCommand(() =>
		{
			Selected = null;
			Edit(new ClientData());
		});
		SaveCommand = new RelayCommand(Save);
		DeleteCommand = new RelayCommand(Delete, () => !IsNew);
		ToggleActiveCommand = new RelayCommand(() =>
		{
			Form.Active = !Form.Active;
			Save();
		}, () => !IsNew);
		AddAttachmentCommand = new RelayCommand(AddAttachment, () => !IsNew);
		OpenAttachmentCommand = new RelayCommand(OpenAttachment, () => (object)SelectedAttachment != null);
		ReplaceAttachmentCommand = new RelayCommand(ReplaceAttachment, () => (object)SelectedAttachment != null);
		RemoveAttachmentCommand = new RelayCommand(RemoveAttachment, () => (object)SelectedAttachment != null);
	}

	public override void Load()
	{
		_all = Svc.Clients.ListAll(ShowInactive);
		ApplyFilter();
		if (_form.Id != Guid.Empty)
		{
			ClientData current = _all.FirstOrDefault((ClientData c) => c.Id == _form.Id);
			if (current != null)
			{
				Edit(current);
				return;
			}
		}
		Edit(new ClientData());
	}

	private void ApplyFilter()
	{
		string b = (Search ?? "").Trim();
		string digits = MyFinances.Core.Common.Cpf.Normalize(b);
		ItemList.Clear();
		foreach (ClientData c in _all.Where((ClientData clientData) => b.Length == 0 || clientData.FullName.Contains(b, StringComparison.CurrentCultureIgnoreCase) || (digits.Length >= 3 && clientData.Cpf.Contains(digits)) || (clientData.Phone ?? "").Contains(b)))
		{
			ItemList.Add(c);
		}
	}

	private void Edit(ClientData c)
	{
		Form = new ClientData
		{
			Id = c.Id,
			FullName = c.FullName,
			Cpf = c.Cpf,
			BirthDate = c.BirthDate,
			DeclaredIncome = c.DeclaredIncome,
			Phone = c.Phone,
			Email = c.Email,
			SocialMedia = c.SocialMedia,
			ZipCode = c.ZipCode,
			Street = c.Street,
			Number = c.Number,
			AddressLine2 = c.AddressLine2,
			District = c.District,
			City = c.City,
			StateCode = c.StateCode,
			Notes = c.Notes,
			Active = c.Active,
			CreatedAt = c.CreatedAt,
			LoanCount = c.LoanCount,
			AttachmentCount = c.AttachmentCount
		};
		Cpf = ((c.Cpf.Length == 11) ? MyFinances.Core.Common.Cpf.Format(c.Cpf) : c.Cpf);
		DateOnly? birthDate = c.BirthDate;
		string birth;
		if (birthDate.HasValue)
		{
			DateOnly d = birthDate.GetValueOrDefault();
			birth = Input.Date(d);
		}
		else
		{
			birth = "";
		}
		Birth = birth;
		decimal? declaredIncome = c.DeclaredIncome;
		string income;
		if (declaredIncome.HasValue)
		{
			decimal r = declaredIncome.GetValueOrDefault();
			income = Money.Num(r);
		}
		else
		{
			income = "";
		}
		Income = income;
		Error = "";
		LoadAttachments();
	}

	private void LoadAttachments()
	{
		Attachments.Clear();
		if (IsNew)
		{
			return;
		}
		foreach (Attachment a in Svc.Clients.Attachments(Form.Id))
		{
			Attachments.Add(new AttachmentRow(a.Id, ClientsService.KindName(a.Kind), a.OriginalName, $"{Math.Max(1L, a.SizeBytes / 1024)} KB", a.ReplacedAt ?? a.CreatedAt, a.ReplacedAt.HasValue));
		}
	}

	private void Save()
	{
		try
		{
			List<string> errors = new List<string>();
			Form.Cpf = Cpf;
			Form.BirthDate = Input.OptionalDate(Birth, "Data de nascimento", errors);
			Form.DeclaredIncome = (string.IsNullOrWhiteSpace(Income) ? ((decimal?)null) : new decimal?(Input.Dec(Income, "Renda informada", errors)));
			errors.AddRange(ClientsService.Validate(Form));
			Input.Check(errors);
			Svc.Clients.Save(Form);
			Error = "";
			Load();
			Dialogs.Info("Cliente salvo.");
		}
		catch (ValidationException ex)
		{
			Error = string.Join("\n", ex.Errors);
		}
	}

	private void Delete()
	{
		if (Dialogs.Confirm("Excluir este cliente e TODOS os anexos dele? Isso não pode ser desfeito (a não ser por backup)."))
		{
			Svc.Clients.Delete(Form.Id);
			Edit(new ClientData());
			Load();
		}
	}

	private void AddAttachment()
	{
		string file = Dialogs.OpenFile("Documentos (PDF, imagens)|*.pdf;*.jpg;*.jpeg;*.png;*.webp;*.heic", "Anexar " + AttachmentKind.Name);
		if (file != null)
		{
			Svc.Clients.AddAttachment(Form.Id, AttachmentKind.Kind, file);
			LoadAttachments();
		}
	}

	private bool CheckPin()
	{
		Preferences p = Svc.Config.Preferences();
		if (string.IsNullOrEmpty(p.PinHash) || !p.PinForAttachments)
		{
			return true;
		}
		string pin = Dialogs.AskText("PIN", "Digite o PIN para abrir documentos de clientes:", password: true);
		if (pin == null)
		{
			return false;
		}
		if (Pin.Check(pin, p.PinHash))
		{
			return true;
		}
		Dialogs.Warn("PIN incorreto.");
		return false;
	}

	private void OpenAttachment()
	{
		if ((object)SelectedAttachment != null && CheckPin())
		{
			Svc.Clients.OpenAttachment(SelectedAttachment.Id);
		}
	}

	private void ReplaceAttachment()
	{
		if ((object)SelectedAttachment != null && CheckPin())
		{
			string file = Dialogs.OpenFile("Documentos (PDF, imagens)|*.pdf;*.jpg;*.jpeg;*.png;*.webp;*.heic", "Substituir anexo");
			if (file != null && Dialogs.Confirm("Substituir \"" + SelectedAttachment.Name + "\" pelo arquivo novo? O antigo será apagado."))
			{
				Svc.Clients.ReplaceAttachment(SelectedAttachment.Id, file);
				LoadAttachments();
			}
		}
	}

	private void RemoveAttachment()
	{
		if ((object)SelectedAttachment != null && CheckPin() && Dialogs.Confirm("Remover o anexo \"" + SelectedAttachment.Name + "\"?"))
		{
			Svc.Clients.RemoveAttachment(SelectedAttachment.Id);
			LoadAttachments();
		}
	}
}
