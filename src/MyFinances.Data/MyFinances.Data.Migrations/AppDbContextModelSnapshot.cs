using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyFinances.Data.Migrations;

[DbContext(typeof(AppDbContext))]
internal class AppDbContextModelSnapshot : ModelSnapshot
{
	protected override void BuildModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
		modelBuilder.Entity("MyFinances.Data.Attachment", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<Guid>("ClientId").HasColumnType("TEXT");
			b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
			b.Property<string>("Extension").IsRequired().HasMaxLength(10)
				.HasColumnType("TEXT");
			b.Property<string>("File").IsRequired().HasMaxLength(200)
				.HasColumnType("TEXT");
			b.Property<int>("Kind").HasColumnType("INTEGER");
			b.Property<string>("OriginalName").IsRequired().HasColumnType("TEXT");
			b.Property<DateTime?>("ReplacedAt").HasColumnType("TEXT");
			b.Property<string>("Sha256").IsRequired().HasMaxLength(64)
				.HasColumnType("TEXT");
			b.Property<long>("SizeBytes").HasColumnType("INTEGER");
			b.HasKey("Id");
			b.HasIndex("ClientId");
			b.ToTable("Attachments");
		});
		modelBuilder.Entity("MyFinances.Data.AuditEntry", (EntityTypeBuilder b) =>
		{
			b.Property<long>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("Action").IsRequired().HasMaxLength(40)
				.HasColumnType("TEXT");
			b.Property<string>("Entity").IsRequired().HasMaxLength(40)
				.HasColumnType("TEXT");
			b.Property<Guid?>("EntityId").HasColumnType("TEXT");
			b.Property<string>("Summary").IsRequired().HasMaxLength(300)
				.HasColumnType("TEXT");
			b.Property<DateTime>("Timestamp").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("Timestamp");
			b.ToTable("AuditEntries");
		});
		modelBuilder.Entity("MyFinances.Data.Category", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<bool>("Active").HasColumnType("INTEGER");
			b.Property<string>("Color").IsRequired().HasMaxLength(9)
				.HasColumnType("TEXT");
			b.Property<int?>("Kind").HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasMaxLength(80)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("Categories");
		});
		modelBuilder.Entity("MyFinances.Data.Client", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<bool>("Active").HasColumnType("INTEGER");
			b.Property<string>("AddressLine2").HasColumnType("TEXT");
			b.Property<string>("BirthDate").HasColumnType("TEXT");
			b.Property<string>("City").HasColumnType("TEXT");
			b.Property<string>("Cpf").IsRequired().HasColumnType("TEXT");
			b.Property<string>("CpfHash").IsRequired().HasMaxLength(64)
				.HasColumnType("TEXT");
			b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
			b.Property<string>("DeclaredIncome").HasColumnType("TEXT");
			b.Property<string>("District").HasColumnType("TEXT");
			b.Property<string>("Email").HasColumnType("TEXT");
			b.Property<string>("FullName").IsRequired().HasColumnType("TEXT");
			b.Property<string>("Notes").HasColumnType("TEXT");
			b.Property<string>("Number").HasColumnType("TEXT");
			b.Property<string>("Phone").HasColumnType("TEXT");
			b.Property<string>("SocialMedia").HasColumnType("TEXT");
			b.Property<string>("StateCode").HasMaxLength(2).HasColumnType("TEXT");
			b.Property<string>("Street").HasColumnType("TEXT");
			b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
			b.Property<string>("ZipCode").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("CpfHash").IsUnique();
			b.ToTable("Clients");
		});
		modelBuilder.Entity("MyFinances.Data.Entry", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<decimal>("Amount").HasColumnType("TEXT");
			b.Property<Guid?>("CategoryId").HasColumnType("TEXT");
			b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
			b.Property<string>("Description").IsRequired().HasColumnType("TEXT");
			b.Property<string>("Group").HasMaxLength(120).HasColumnType("TEXT");
			b.Property<string>("Installment").HasMaxLength(10).HasColumnType("TEXT");
			b.Property<int>("Kind").HasColumnType("INTEGER");
			b.Property<int>("Origin").HasColumnType("INTEGER");
			b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
			b.Property<string>("YearMonth").IsRequired().HasMaxLength(7)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("CategoryId");
			b.HasIndex("Group");
			b.HasIndex("YearMonth", "Kind");
			b.ToTable("Entries");
		});
		modelBuilder.Entity("MyFinances.Data.FinanceMonth", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<decimal>("AbsenceDays").HasColumnType("TEXT");
			b.Property<decimal>("LateHours").HasColumnType("TEXT");
			b.Property<decimal>("LostDsr").HasColumnType("TEXT");
			b.Property<string>("Note").HasColumnType("TEXT");
			b.Property<decimal?>("SalarySet").HasColumnType("TEXT");
			b.Property<string>("YearMonth").IsRequired().HasMaxLength(7)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("YearMonth").IsUnique();
			b.ToTable("Months");
		});
		modelBuilder.Entity("MyFinances.Data.Loan", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<Guid>("ClientId").HasColumnType("TEXT");
			b.Property<int>("Code").HasColumnType("INTEGER");
			b.Property<DateOnly>("ContractDate").HasColumnType("TEXT");
			b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
			b.Property<string>("Notes").HasColumnType("TEXT");
			b.Property<int>("State").HasColumnType("INTEGER");
			b.Property<DateTime?>("StateDate").HasColumnType("TEXT");
			b.Property<string>("StateReason").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("ClientId");
			b.HasIndex("Code").IsUnique();
			b.ToTable("Loans");
		});
		modelBuilder.Entity("MyFinances.Data.LoanVersion", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<decimal>("DiscountGranted").HasColumnType("TEXT");
			b.Property<string>("InstallmentsJson").IsRequired().HasColumnType("TEXT");
			b.Property<int>("Kind").HasColumnType("INTEGER");
			b.Property<Guid>("LoanId").HasColumnType("TEXT");
			b.Property<int>("Number").HasColumnType("INTEGER");
			b.Property<string>("Reason").HasColumnType("TEXT");
			b.Property<DateTime>("Start").HasColumnType("TEXT");
			b.Property<string>("TermsJson").IsRequired().HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("LoanId", "Number").IsUnique();
			b.ToTable("LoanVersions");
		});
		modelBuilder.Entity("MyFinances.Data.Payment", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
			b.Property<decimal>("Amount").HasColumnType("TEXT");
			b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
			b.Property<bool>("ExtraAmortization").HasColumnType("INTEGER");
			b.Property<Guid>("LoanId").HasColumnType("TEXT");
			b.Property<string>("Note").HasColumnType("TEXT");
			b.Property<string>("PaymentMethod").IsRequired().HasMaxLength(30)
				.HasColumnType("TEXT");
			b.Property<string>("ReversalReason").HasColumnType("TEXT");
			b.Property<bool>("Reversed").HasColumnType("INTEGER");
			b.Property<DateTime?>("ReversedAt").HasColumnType("TEXT");
			b.Property<DateTime>("Timestamp").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("LoanId", "Timestamp");
			b.ToTable("Payments");
		});
		modelBuilder.Entity("MyFinances.Data.Setting", (EntityTypeBuilder b) =>
		{
			b.Property<string>("Key").HasMaxLength(100).HasColumnType("TEXT");
			b.Property<string>("Amount").IsRequired().HasColumnType("TEXT");
			b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
			b.HasKey("Key");
			b.ToTable("Settings");
		});
		modelBuilder.Entity("MyFinances.Data.Attachment", (EntityTypeBuilder b) =>
		{
			b.HasOne("MyFinances.Data.Client", "Client").WithMany("Attachments").HasForeignKey("ClientId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("Client");
		});
		modelBuilder.Entity("MyFinances.Data.Entry", (EntityTypeBuilder b) =>
		{
			b.HasOne("MyFinances.Data.Category", "Category").WithMany().HasForeignKey("CategoryId")
				.OnDelete(DeleteBehavior.SetNull);
			b.Navigation("Category");
		});
		modelBuilder.Entity("MyFinances.Data.Loan", (EntityTypeBuilder b) =>
		{
			b.HasOne("MyFinances.Data.Client", "Client").WithMany("Loans").HasForeignKey("ClientId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("Client");
		});
		modelBuilder.Entity("MyFinances.Data.LoanVersion", (EntityTypeBuilder b) =>
		{
			b.HasOne("MyFinances.Data.Loan", "Loan").WithMany("Versions").HasForeignKey("LoanId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("Loan");
		});
		modelBuilder.Entity("MyFinances.Data.Payment", (EntityTypeBuilder b) =>
		{
			b.HasOne("MyFinances.Data.Loan", "Loan").WithMany("Payments").HasForeignKey("LoanId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("Loan");
		});
		modelBuilder.Entity("MyFinances.Data.Client", (EntityTypeBuilder b) =>
		{
			b.Navigation("Attachments");
			b.Navigation("Loans");
		});
		modelBuilder.Entity("MyFinances.Data.Loan", (EntityTypeBuilder b) =>
		{
			b.Navigation("Payments");
			b.Navigation("Versions");
		});
	}
}
