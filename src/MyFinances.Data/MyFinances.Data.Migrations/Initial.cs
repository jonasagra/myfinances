using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace MyFinances.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017193926_Initial")]
public class Initial : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable("AuditEntries", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<long>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			OperationBuilder<AddColumnOperation> timestamp = table.Column<DateTime>("TEXT");
			int? maxLength = 40;
			OperationBuilder<AddColumnOperation> entity = table.Column<string>("TEXT", null, maxLength);
			OperationBuilder<AddColumnOperation> entityId = table.Column<Guid>("TEXT", null, null, rowVersion: false, null, nullable: true);
			maxLength = 40;
			OperationBuilder<AddColumnOperation> action = table.Column<string>("TEXT", null, maxLength);
			maxLength = 300;
			return new
			{
				Id = id,
				Timestamp = timestamp,
				Entity = entity,
				EntityId = entityId,
				Action = action,
				Summary = table.Column<string>("TEXT", null, maxLength)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_AuditEntries", x => x.Id);
		});
		migrationBuilder.CreateTable("Categories", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			int? maxLength = 80;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("TEXT", null, maxLength);
			OperationBuilder<AddColumnOperation> kind = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true);
			maxLength = 9;
			return new
			{
				Id = id,
				Name = name,
				Kind = kind,
				Color = table.Column<string>("TEXT", null, maxLength),
				Active = table.Column<bool>("INTEGER")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Categories", x => x.Id);
		});
		migrationBuilder.CreateTable("Clients", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> fullName = table.Column<string>("TEXT");
			OperationBuilder<AddColumnOperation> cpf = table.Column<string>("TEXT");
			int? maxLength = 64;
			OperationBuilder<AddColumnOperation> cpfHash = table.Column<string>("TEXT", null, maxLength);
			OperationBuilder<AddColumnOperation> birthDate = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> declaredIncome = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> phone = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> email = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> socialMedia = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> zipCode = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> street = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> number = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> addressLine = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> district = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> city = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true);
			maxLength = 2;
			return new
			{
				Id = id,
				FullName = fullName,
				Cpf = cpf,
				CpfHash = cpfHash,
				BirthDate = birthDate,
				DeclaredIncome = declaredIncome,
				Phone = phone,
				Email = email,
				SocialMedia = socialMedia,
				ZipCode = zipCode,
				Street = street,
				Number = number,
				AddressLine2 = addressLine,
				District = district,
				City = city,
				StateCode = table.Column<string>("TEXT", null, maxLength, rowVersion: false, null, nullable: true),
				Notes = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Active = table.Column<bool>("INTEGER"),
				CreatedAt = table.Column<DateTime>("TEXT"),
				UpdatedAt = table.Column<DateTime>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Clients", x => x.Id);
		});
		migrationBuilder.CreateTable("Months", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			int? maxLength = 7;
			return new
			{
				Id = id,
				YearMonth = table.Column<string>("TEXT", null, maxLength),
				SalarySet = table.Column<decimal>("TEXT", null, null, rowVersion: false, null, nullable: true),
				AbsenceDays = table.Column<decimal>("TEXT"),
				LostDsr = table.Column<decimal>("TEXT"),
				LateHours = table.Column<decimal>("TEXT"),
				Note = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Months", x => x.Id);
		});
		migrationBuilder.CreateTable("Settings", (ColumnsBuilder table) =>
		{
			int? maxLength = 100;
			return new
			{
				Key = table.Column<string>("TEXT", null, maxLength),
				Amount = table.Column<string>("TEXT"),
				UpdatedAt = table.Column<DateTime>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Settings", x => x.Key);
		});
		migrationBuilder.CreateTable("Entries", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			int? maxLength = 7;
			OperationBuilder<AddColumnOperation> yearMonth = table.Column<string>("TEXT", null, maxLength);
			OperationBuilder<AddColumnOperation> kind = table.Column<int>("INTEGER");
			OperationBuilder<AddColumnOperation> description = table.Column<string>("TEXT");
			OperationBuilder<AddColumnOperation> amount = table.Column<decimal>("TEXT");
			OperationBuilder<AddColumnOperation> categoryId = table.Column<Guid>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> origin = table.Column<int>("INTEGER");
			maxLength = 120;
			OperationBuilder<AddColumnOperation> operationBuilder = table.Column<string>("TEXT", null, maxLength, rowVersion: false, null, nullable: true);
			maxLength = 10;
			return new
			{
				Id = id,
				YearMonth = yearMonth,
				Kind = kind,
				Description = description,
				Amount = amount,
				CategoryId = categoryId,
				Origin = origin,
				Group = operationBuilder,
				Installment = table.Column<string>("TEXT", null, maxLength, rowVersion: false, null, nullable: true),
				CreatedAt = table.Column<DateTime>("TEXT"),
				UpdatedAt = table.Column<DateTime>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Entries", x => x.Id);
			table.ForeignKey("FK_Entries_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", null, ReferentialAction.NoAction, ReferentialAction.SetNull);
		});
		migrationBuilder.CreateTable("Attachments", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> clientId = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> kind = table.Column<int>("INTEGER");
			OperationBuilder<AddColumnOperation> originalName = table.Column<string>("TEXT");
			int? maxLength = 10;
			OperationBuilder<AddColumnOperation> extension = table.Column<string>("TEXT", null, maxLength);
			OperationBuilder<AddColumnOperation> sizeBytes = table.Column<long>("INTEGER");
			maxLength = 64;
			OperationBuilder<AddColumnOperation> sha = table.Column<string>("TEXT", null, maxLength);
			maxLength = 200;
			return new
			{
				Id = id,
				ClientId = clientId,
				Kind = kind,
				OriginalName = originalName,
				Extension = extension,
				SizeBytes = sizeBytes,
				Sha256 = sha,
				File = table.Column<string>("TEXT", null, maxLength),
				CreatedAt = table.Column<DateTime>("TEXT"),
				ReplacedAt = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Attachments", x => x.Id);
			table.ForeignKey("FK_Attachments_Clients_ClientId", x => x.ClientId, "Clients", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("Loans", (ColumnsBuilder table) => new
		{
			Id = table.Column<Guid>("TEXT"),
			Code = table.Column<int>("INTEGER"),
			ClientId = table.Column<Guid>("TEXT"),
			ContractDate = table.Column<DateOnly>("TEXT"),
			State = table.Column<int>("INTEGER"),
			StateDate = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true),
			StateReason = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			Notes = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			CreatedAt = table.Column<DateTime>("TEXT")
		}, null, table =>
		{
			table.PrimaryKey("PK_Loans", x => x.Id);
			table.ForeignKey("FK_Loans_Clients_ClientId", x => x.ClientId, "Clients", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("LoanVersions", (ColumnsBuilder table) => new
		{
			Id = table.Column<Guid>("TEXT"),
			LoanId = table.Column<Guid>("TEXT"),
			Number = table.Column<int>("INTEGER"),
			Start = table.Column<DateTime>("TEXT"),
			Kind = table.Column<int>("INTEGER"),
			TermsJson = table.Column<string>("TEXT"),
			InstallmentsJson = table.Column<string>("TEXT"),
			DiscountGranted = table.Column<decimal>("TEXT"),
			Reason = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true)
		}, null, table =>
		{
			table.PrimaryKey("PK_LoanVersions", x => x.Id);
			table.ForeignKey("FK_LoanVersions_Loans_LoanId", x => x.LoanId, "Loans", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateTable("Payments", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> loanId = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> timestamp = table.Column<DateTime>("TEXT");
			OperationBuilder<AddColumnOperation> amount = table.Column<decimal>("TEXT");
			int? maxLength = 30;
			return new
			{
				Id = id,
				LoanId = loanId,
				Timestamp = timestamp,
				Amount = amount,
				PaymentMethod = table.Column<string>("TEXT", null, maxLength),
				Note = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				ExtraAmortization = table.Column<bool>("INTEGER"),
				Reversed = table.Column<bool>("INTEGER"),
				ReversedAt = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true),
				ReversalReason = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				CreatedAt = table.Column<DateTime>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Payments", x => x.Id);
			table.ForeignKey("FK_Payments_Loans_LoanId", x => x.LoanId, "Loans", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateIndex("IX_Attachments_ClientId", "Attachments", "ClientId");
		migrationBuilder.CreateIndex("IX_AuditEntries_Timestamp", "AuditEntries", "Timestamp");
		migrationBuilder.CreateIndex("IX_Clients_CpfHash", "Clients", "CpfHash", null, unique: true);
		migrationBuilder.CreateIndex("IX_Entries_CategoryId", "Entries", "CategoryId");
		migrationBuilder.CreateIndex("IX_Entries_Group", "Entries", "Group");
		migrationBuilder.CreateIndex("IX_Entries_YearMonth_Kind", "Entries", new string[2] { "YearMonth", "Kind" });
		migrationBuilder.CreateIndex("IX_Loans_ClientId", "Loans", "ClientId");
		migrationBuilder.CreateIndex("IX_Loans_Code", "Loans", "Code", null, unique: true);
		migrationBuilder.CreateIndex("IX_LoanVersions_LoanId_Number", "LoanVersions", new string[2] { "LoanId", "Number" }, null, unique: true);
		migrationBuilder.CreateIndex("IX_Months_YearMonth", "Months", "YearMonth", null, unique: true);
		migrationBuilder.CreateIndex("IX_Payments_LoanId_Timestamp", "Payments", new string[2] { "LoanId", "Timestamp" });
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("Attachments");
		migrationBuilder.DropTable("AuditEntries");
		migrationBuilder.DropTable("Entries");
		migrationBuilder.DropTable("LoanVersions");
		migrationBuilder.DropTable("Months");
		migrationBuilder.DropTable("Payments");
		migrationBuilder.DropTable("Settings");
		migrationBuilder.DropTable("Categories");
		migrationBuilder.DropTable("Loans");
		migrationBuilder.DropTable("Clients");
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
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
