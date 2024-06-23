﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SocialWelfare.Models.Entities;

#nullable disable

namespace SocialWelfare.Migrations
{
    [DbContext(typeof(SocialWelfareDepartmentContext))]
    partial class SocialWelfareDepartmentContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AddressJoin", b =>
                {
                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("AddressId")
                        .HasColumnType("int");

                    b.Property<string>("Block")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("BlockId")
                        .HasColumnType("int");

                    b.Property<string>("District")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("DistrictId")
                        .HasColumnType("int");

                    b.Property<string>("PanchayatMuncipality")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Pincode")
                        .HasColumnType("int");

                    b.Property<string>("Tehsil")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("TehsilId")
                        .HasColumnType("int");

                    b.Property<string>("Village")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Ward")
                        .HasColumnType("nvarchar(max)");

                    b.ToTable("AddressJoins");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Address", b =>
                {
                    b.Property<int>("AddressId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AddressId"));

                    b.Property<string>("AddressDetails")
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<int?>("BlockId")
                        .HasColumnType("int");

                    b.Property<int?>("DistrictId")
                        .HasColumnType("int");

                    b.Property<int?>("HalqaPanchayatId")
                        .HasColumnType("int");

                    b.Property<int?>("PincodeId")
                        .HasColumnType("int");

                    b.Property<int?>("TehsilId")
                        .HasColumnType("int");

                    b.Property<int?>("VillageId")
                        .HasColumnType("int");

                    b.Property<int?>("WardId")
                        .HasColumnType("int");

                    b.HasKey("AddressId");

                    b.ToTable("Address", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Application", b =>
                {
                    b.Property<string>("ApplicationId")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ApplicantImage")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ApplicantName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ApplicationStatus")
                        .IsRequired()
                        .HasMaxLength(15)
                        .IsUnicode(false)
                        .HasColumnType("varchar(15)");

                    b.Property<string>("BankDetails")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Category")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int?>("CitizenId")
                        .HasColumnType("int");

                    b.Property<string>("DateOfBirth")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Documents")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EditList")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("MobileNumber")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("PermanentAddressId")
                        .HasMaxLength(10)
                        .HasColumnType("nchar(10)")
                        .IsFixedLength();

                    b.Property<string>("Phase")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("[]");

                    b.Property<string>("PresentAddressId")
                        .HasMaxLength(10)
                        .HasColumnType("nchar(10)")
                        .IsFixedLength();

                    b.Property<string>("Relation")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("RelationName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ServiceId")
                        .HasColumnType("int");

                    b.Property<string>("ServiceSpecific")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("SubmissionDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("UpdateRequest")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ApplicationId")
                        .HasName("PK__tmp_ms_x__C93A4C99B10E9956");

                    b.HasIndex("ServiceId");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.ApplicationPerDistrict", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int>("CountValue")
                        .HasColumnType("int");

                    b.Property<int>("DistrictId")
                        .HasColumnType("int");

                    b.Property<string>("FinancialYear")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Uuid");

                    b.ToTable("ApplicationPerDistrict", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.ApplicationsHistory", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<string>("ApplicationId")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("History")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("[]");

                    b.HasKey("Uuid");

                    b.HasIndex("ApplicationId");

                    b.ToTable("ApplicationsHistory", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Block", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int?>("BlockId")
                        .HasColumnType("int");

                    b.Property<string>("BlockName")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("DistrictId")
                        .HasColumnType("int")
                        .HasColumnName("DistrictID");

                    b.HasKey("Uuid");

                    b.ToTable("Block", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.District", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int>("DistrictId")
                        .HasColumnType("int")
                        .HasColumnName("DistrictID");

                    b.Property<string>("DistrictName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("DistrictShort")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("Division")
                        .HasColumnType("int");

                    b.HasKey("Uuid");

                    b.ToTable("District", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Feedback", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ServiceRelated")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("SubmittedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Uuid");

                    b.ToTable("Feedback", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.HalqaPanchayat", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int?>("BlockId")
                        .HasColumnType("int");

                    b.Property<string>("PanchayatName")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Uuid");

                    b.ToTable("HalqaPanchayat", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.OfficersDesignation", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<string>("Designation")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<string>("DesignationShort")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Uuid");

                    b.ToTable("OfficersDesignations");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Pincode", b =>
                {
                    b.Property<int>("PincodeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("pincode_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("PincodeId"));

                    b.Property<int?>("Pincode1")
                        .HasColumnType("int")
                        .HasColumnName("Pincode");

                    b.HasKey("PincodeId");

                    b.ToTable("Pincode", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Service", b =>
                {
                    b.Property<int>("ServiceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ServiceId"));

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("Department")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("FormElement")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ServiceName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("WorkForceOfficers")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ServiceId")
                        .HasName("PK__tmp_ms_x__C51BB00A0C58849B");

                    b.ToTable("Services");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Tehsil", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int?>("DistrictId")
                        .HasColumnType("int")
                        .HasColumnName("DistrictID");

                    b.Property<int?>("TehsilId")
                        .HasColumnType("int");

                    b.Property<string>("TehsilName")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Uuid");

                    b.ToTable("Tehsil", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<string>("BackupCodes")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("EmailValid")
                        .HasColumnType("bit");

                    b.Property<string>("MobileNumber")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("RegisteredDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("UserSpecificDetails")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserType")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Village", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int?>("HalqaPanchayatId")
                        .HasColumnType("int");

                    b.Property<int?>("TehsilId")
                        .HasColumnType("int");

                    b.Property<string>("VillageName")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Uuid");

                    b.ToTable("Village", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Ward", b =>
                {
                    b.Property<int>("Uuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UUID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Uuid"));

                    b.Property<int?>("VillageId")
                        .HasColumnType("int");

                    b.Property<string>("WardName")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Uuid");

                    b.ToTable("Ward", (string)null);
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Application", b =>
                {
                    b.HasOne("SocialWelfare.Models.Entities.Service", "Service")
                        .WithMany("Applications")
                        .HasForeignKey("ServiceId")
                        .IsRequired()
                        .HasConstraintName("FK__Applicati__Servi__25DB9BFC");

                    b.Navigation("Service");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.ApplicationsHistory", b =>
                {
                    b.HasOne("SocialWelfare.Models.Entities.Application", "Application")
                        .WithMany("ApplicationsHistories")
                        .HasForeignKey("ApplicationId")
                        .IsRequired()
                        .HasConstraintName("FK_ApplicationsHistory_Applications");

                    b.Navigation("Application");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Application", b =>
                {
                    b.Navigation("ApplicationsHistories");
                });

            modelBuilder.Entity("SocialWelfare.Models.Entities.Service", b =>
                {
                    b.Navigation("Applications");
                });
#pragma warning restore 612, 618
        }
    }
}
