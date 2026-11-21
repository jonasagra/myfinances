using System.Linq;
using System.Reflection;
using MyFinances.Data.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyFinances.Data;

public class AppDbContext : DbContext
{
	public DbSet<Setting> Settings => Set<Setting>();

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<FinanceMonth> Months => Set<FinanceMonth>();

	public DbSet<Entry> Entries => Set<Entry>();

	public DbSet<Client> Clients => Set<Client>();

	public DbSet<Attachment> Attachments => Set<Attachment>();

	public DbSet<Loan> Loans => Set<Loan>();

	public DbSet<LoanVersion> LoanVersions => Set<LoanVersion>();

	public DbSet<Payment> Payments => Set<Payment>();

	public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public static DbContextOptions<AppDbContext> Options(string? connectionString = null)
	{
		return new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString ?? AppPaths.ConnectionString).Options;
	}

	public static AppDbContext Create()
	{
		return new AppDbContext(Options());
	}

	protected override void OnModelCreating(ModelBuilder b)
	{
		ValueConverter<string, string> encrypt = new ValueConverter<string, string>((string v) => Vault.Current.Encrypt(v), (string v) => Vault.Current.Decrypt(v));
		foreach (IMutableEntityType et in b.Model.GetEntityTypes())
		{
			foreach (PropertyInfo pi in from p in et.ClrType.GetProperties()
				where p.GetCustomAttribute<EncryptedAttribute>() != null
				select p)
			{
				b.Entity(et.ClrType).Property(pi.Name).HasConversion(encrypt);
			}
		}
		b.Entity<FinanceMonth>().HasIndex((FinanceMonth m) => m.YearMonth).IsUnique();
		b.Entity<Entry>().HasIndex((Entry l) => new { l.YearMonth, l.Kind });
		b.Entity<Entry>().HasIndex((Entry l) => l.Group);
		b.Entity<Entry>().HasOne((Entry l) => l.Category).WithMany()
			.HasForeignKey((Entry l) => l.CategoryId)
			.OnDelete(DeleteBehavior.SetNull);
		b.Entity<Client>().HasIndex((Client c) => c.CpfHash).IsUnique();
		b.Entity<Attachment>().HasOne((Attachment a) => a.Client).WithMany((Client c) => c.Attachments)
			.HasForeignKey((Attachment a) => a.ClientId)
			.OnDelete(DeleteBehavior.Restrict);
		b.Entity<Loan>().HasIndex((Loan e) => e.Code).IsUnique();
		b.Entity<Loan>().HasOne((Loan e) => e.Client).WithMany((Client c) => c.Loans)
			.HasForeignKey((Loan e) => e.ClientId)
			.OnDelete(DeleteBehavior.Restrict);
		b.Entity<LoanVersion>().HasOne((LoanVersion v) => v.Loan).WithMany((Loan e) => e.Versions)
			.HasForeignKey((LoanVersion v) => v.LoanId)
			.OnDelete(DeleteBehavior.Cascade);
		b.Entity<LoanVersion>().HasIndex((LoanVersion v) => new { v.LoanId, v.Number }).IsUnique();
		b.Entity<Payment>().HasOne((Payment p) => p.Loan).WithMany((Loan e) => e.Payments)
			.HasForeignKey((Payment p) => p.LoanId)
			.OnDelete(DeleteBehavior.Restrict);
		b.Entity<Payment>().HasIndex((Payment p) => new { p.LoanId, p.Timestamp });
		b.Entity<AuditEntry>().HasIndex((AuditEntry a) => a.Timestamp);
	}
}
