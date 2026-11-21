using MyFinances.Data.Services;

namespace MyFinances.App.Infra;

public static class Svc
{
	public static SettingsService Config { get; } = new SettingsService();

	public static FinanceService Finance { get; } = new FinanceService(Config);

	public static ClientsService Clients { get; } = new ClientsService(Config);

	public static LoansService Loans { get; } = new LoansService();

	public static BackupService Backup { get; } = new BackupService(Config);

	public static ImportService Import { get; } = new ImportService(Finance, Config);
}
