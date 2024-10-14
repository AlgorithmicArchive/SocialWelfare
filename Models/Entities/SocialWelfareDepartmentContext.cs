using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SocialWelfare.Models.Entities;

public partial class SocialWelfareDepartmentContext : DbContext
{
    public SocialWelfareDepartmentContext()
    {
    }

    public SocialWelfareDepartmentContext(DbContextOptions<SocialWelfareDepartmentContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<ApplicationList> ApplicationLists { get; set; }

    public virtual DbSet<ApplicationPerDistrict> ApplicationPerDistricts { get; set; }

    public virtual DbSet<ApplicationsHistory> ApplicationsHistories { get; set; }

    public virtual DbSet<BankFile> BankFiles { get; set; }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<CurrentPhase> CurrentPhases { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<HalqaPanchayat> HalqaPanchayats { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<OfficersDesignation> OfficersDesignations { get; set; }

    public virtual DbSet<Pincode> Pincodes { get; set; }

    public virtual DbSet<RecordCount> RecordCounts { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Tehsil> Tehsils { get; set; }

    public virtual DbSet<UniqueIdtable> UniqueIdtables { get; set; }

    public virtual DbSet<UpdatedLetterDetail> UpdatedLetterDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Village> Villages { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }
    public virtual DbSet<AddressJoin> AddressJoins { get; set; }
    public virtual DbSet<BankFileModel> BankFileModels { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddressJoin>().HasNoKey();
        modelBuilder.Entity<BankFileModel>().HasNoKey();
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("Address");

            entity.Property(e => e.AddressDetails).IsUnicode(false);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApplicantImage).IsUnicode(false);
            entity.Property(e => e.ApplicantName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ApplicationStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.BankDetails).IsUnicode(false);
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DateOfBirth)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Documents).IsUnicode(false);
            entity.Property(e => e.EditList)
                .IsUnicode(false)
                .HasDefaultValue("[]");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PermanentAddressId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PresentAddressId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Relation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RelationName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ServiceSpecific).IsUnicode(false);
            entity.Property(e => e.SubmissionDate)
                .HasMaxLength(50)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Citizen).WithMany(p => p.Applications)
                .HasForeignKey(d => d.CitizenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Users");

            entity.HasOne(d => d.Service).WithMany(p => p.Applications)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Services");
        });

        modelBuilder.Entity<ApplicationList>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationList");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.AccessLevel)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovalList).IsUnicode(false);
            entity.Property(e => e.Officer)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PoolList).IsUnicode(false);
        });

        modelBuilder.Entity<ApplicationPerDistrict>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationPerDistrict");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.FinancialYear)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.District).WithMany(p => p.ApplicationPerDistricts)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApplicationPerDistrict_District");
        });

        modelBuilder.Entity<ApplicationsHistory>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationsHistory");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.History).IsUnicode(false);

            entity.HasOne(d => d.Application).WithMany(p => p.ApplicationsHistories)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApplicationsHistory_Applications");
        });

        modelBuilder.Entity<BankFile>(entity =>
        {
            entity.HasKey(e => e.FileId);

            entity.Property(e => e.FileName)
                .HasMaxLength(510)
                .IsUnicode(false);
            entity.Property(e => e.GeneratedDate)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ResponseFile)
                .HasMaxLength(510)
                .IsUnicode(false);

            entity.HasOne(d => d.District).WithMany(p => p.BankFiles)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BankFiles_District");

            entity.HasOne(d => d.Service).WithMany(p => p.BankFiles)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BankFiles_Services");
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("Block");

            entity.Property(e => e.BlockId).ValueGeneratedNever();
            entity.Property(e => e.BlockName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DistrictId).HasColumnName("DistrictID");

            entity.HasOne(d => d.District).WithMany(p => p.Blocks)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Block_District");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.EncryptionIv).HasColumnName("encryptionIV");
            entity.Property(e => e.EncryptionKey).HasColumnName("encryptionKey");
            entity.Property(e => e.RegisteredDate)
                .HasMaxLength(50)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Officer).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.OfficerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Certificates_Users");
        });

        modelBuilder.Entity<CurrentPhase>(entity =>
        {
            entity.HasKey(e => e.PhaseId);

            entity.ToTable("CurrentPhase");

            entity.Property(e => e.ActionTaken)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.File)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Officer)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ReceivedOn)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Remarks).IsUnicode(false);

            entity.HasOne(d => d.Application).WithMany(p => p.CurrentPhases)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrentPhase_Applications");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.ToTable("District");

            entity.Property(e => e.DistrictId)
                .ValueGeneratedNever()
                .HasColumnName("DistrictID");
            entity.Property(e => e.DistrictName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DistrictShort)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Feedback");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.Message).IsUnicode(false);
            entity.Property(e => e.ServiceRelated)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SubmittedAt).HasColumnType("decimal(23, 3)");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Users");
        });

        modelBuilder.Entity<HalqaPanchayat>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("HalqaPanchayat");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.PanchayatName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Block).WithMany(p => p.HalqaPanchayats)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HalqaPanchayat_Block");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.Property(e => e.Action).IsUnicode(false);
            entity.Property(e => e.DateOfAction)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Logs_Users");
        });

        modelBuilder.Entity<OfficersDesignation>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.AccessLevel)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Designation).IsUnicode(false);
            entity.Property(e => e.DesignationShort)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pincode>(entity =>
        {
            entity.ToTable("Pincode");

            entity.Property(e => e.PincodeId).HasColumnName("pincode_id");
            entity.Property(e => e.Pincode1).HasColumnName("Pincode");
        });

        modelBuilder.Entity<RecordCount>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            entity.ToTable("RecordCount");

            entity.Property(e => e.Officer)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Service).WithMany(p => p.RecordCounts)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecordCount_Services");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasColumnType("decimal(23, 3)");
            entity.Property(e => e.Department)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FormElement).IsUnicode(false);
            entity.Property(e => e.LetterUpdateDetails).IsUnicode(false);
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UpdateColumn).IsUnicode(false);
            entity.Property(e => e.WorkForceOfficers).IsUnicode(false);
        });

        modelBuilder.Entity<Tehsil>(entity =>
        {
            entity.ToTable("Tehsil");

            entity.Property(e => e.TehsilId).ValueGeneratedNever();
            entity.Property(e => e.DistrictId).HasColumnName("DistrictID");
            entity.Property(e => e.TehsilName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.District).WithMany(p => p.Tehsils)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tehsil_District");
        });

        modelBuilder.Entity<UniqueIdtable>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("UniqueIDTable");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.DistrictNameShort)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MonthShort)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UpdatedLetterDetail>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedDetails).IsUnicode(false);

            entity.HasOne(d => d.Application).WithMany()
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UpdatedLetterDetails_Applications");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.BackupCodes).IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RegisteredDate).HasMaxLength(120);
            entity.Property(e => e.UserSpecificDetails).IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Village>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Village");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.VillageName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.HalqaPanchayat).WithMany(p => p.Villages)
                .HasForeignKey(d => d.HalqaPanchayatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Village_HalqaPanchayat");

            entity.HasOne(d => d.Tehsil).WithMany(p => p.Villages)
                .HasForeignKey(d => d.TehsilId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Village_Tehsil");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Ward");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.WardName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Village).WithMany(p => p.Wards)
                .HasForeignKey(d => d.VillageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ward_Village");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
