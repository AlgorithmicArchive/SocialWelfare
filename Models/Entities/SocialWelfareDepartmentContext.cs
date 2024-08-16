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

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<CurrentPhase> CurrentPhases { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<HalqaPanchayat> HalqaPanchayats { get; set; }

    public virtual DbSet<OfficersDesignation> OfficersDesignations { get; set; }

    public virtual DbSet<Pincode> Pincodes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Tehsil> Tehsils { get; set; }

    public virtual DbSet<UpdatedLetterDetail> UpdatedLetterDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Village> Villages { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    public virtual DbSet<AddressJoin> AddressJoins { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddressJoin>().HasNoKey();

        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("Address");

            entity.Property(e => e.AddressDetails).IsUnicode(false);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__tmp_ms_x__C93A4C99B10E9956");

            entity.HasIndex(e => e.ServiceId, "IX_Applications_ServiceId");

            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApplicantName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ApplicationStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.BankDetails).HasDefaultValue("{}");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.DateOfBirth)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Documents).HasDefaultValue("[]");
            entity.Property(e => e.EditList).HasDefaultValue("[]");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PermanentAddressId)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.PresentAddressId)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Relation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RelationName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SubmissionDate).HasMaxLength(50);

            entity.HasOne(d => d.Service).WithMany(p => p.Applications)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Applicati__Servi__25DB9BFC");
        });

        modelBuilder.Entity<ApplicationList>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationList");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.AccessLevel)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovalList).HasDefaultValue("[]");
            entity.Property(e => e.Officer).HasMaxLength(50);
            entity.Property(e => e.PoolList).HasDefaultValue("[]");
        });

        modelBuilder.Entity<ApplicationPerDistrict>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationPerDistrict");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.FinancialYear)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ApplicationsHistory>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("ApplicationsHistory");

            entity.HasIndex(e => e.ApplicationId, "IX_ApplicationsHistory_ApplicationId");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.ApplicationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.History).HasDefaultValue("[]");

            entity.HasOne(d => d.Application).WithMany(p => p.ApplicationsHistories)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApplicationsHistory_Applications");
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Block");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.BlockName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DistrictId).HasColumnName("DistrictID");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.EncryptionIv).HasColumnName("encryptionIV");
            entity.Property(e => e.EncryptionKey).HasColumnName("encryptionKey");
            entity.Property(e => e.RegisteredDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<CurrentPhase>(entity =>
        {
            entity.HasKey(e => e.PhaseId);

            entity.ToTable("CurrentPhase");

            entity.Property(e => e.ActionTaken)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.ApplicationId).HasMaxLength(50);
            entity.Property(e => e.File).HasMaxLength(50);
            entity.Property(e => e.Officer)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ReceivedOn)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Remarks).HasColumnType("text");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("District");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.DistrictId).HasColumnName("DistrictID");
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
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.ServiceRelated).HasMaxLength(50);
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<HalqaPanchayat>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("HalqaPanchayat");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.PanchayatName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<OfficersDesignation>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.Designation).IsUnicode(false);
            entity.Property(e => e.DesignationShort).HasMaxLength(50);
        });

        modelBuilder.Entity<Pincode>(entity =>
        {
            entity.ToTable("Pincode");

            entity.Property(e => e.PincodeId).HasColumnName("pincode_id");
            entity.Property(e => e.Pincode1).HasColumnName("Pincode");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__tmp_ms_x__C51BB00A0C58849B");

            entity.Property(e => e.BankDispatchFile).HasDefaultValue("");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Department)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.LetterUpdateDetails).HasDefaultValue("[]");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Tehsil>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Tehsil");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.DistrictId).HasColumnName("DistrictID");
            entity.Property(e => e.TehsilName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UpdatedLetterDetail>(entity =>
        {
            entity.HasKey(e => e.ApplicationId);

            entity.Property(e => e.ApplicationId).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.MobileNumber).HasMaxLength(10);
            entity.Property(e => e.RegisteredDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserType).HasMaxLength(15);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Village>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Village");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.VillageName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.Uuid);

            entity.ToTable("Ward");

            entity.Property(e => e.Uuid).HasColumnName("UUID");
            entity.Property(e => e.WardName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
