namespace MyFinances.Data.Services;

public sealed record DonationInfo
{
	public string Title { get; init; } = "Gostou do MyFinances?";

	public string Message { get; init; } = "O MyFinances é gratuito e continua gratuito. Se ele te ajuda, uma doação voluntária ajuda a manter o projeto. Nenhuma função depende disso.";

	public string? PixKey { get; init; }

	public string? Link { get; init; }
}
