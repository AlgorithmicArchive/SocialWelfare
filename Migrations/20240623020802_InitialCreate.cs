using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialWelfare.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    AddressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    TehsilId = table.Column<int>(type: "int", nullable: true),
                    BlockId = table.Column<int>(type: "int", nullable: true),
                    HalqaPanchayatId = table.Column<int>(type: "int", nullable: true),
                    VillageId = table.Column<int>(type: "int", nullable: true),
                    WardId = table.Column<int>(type: "int", nullable: true),
                    PincodeId = table.Column<int>(type: "int", nullable: true),
                    AddressDetails = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.AddressId);
                });

            migrationBuilder.CreateTable(
                name: "AddressJoins",
                columns: table => new
                {
                    AddressId = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    Tehsil = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TehsilId = table.Column<int>(type: "int", nullable: true),
                    Block = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockId = table.Column<int>(type: "int", nullable: true),
                    PanchayatMuncipality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Village = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pincode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ApplicationPerDistrict",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    FinancialYear = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CountValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPerDistrict", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Block",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictID = table.Column<int>(type: "int", nullable: true),
                    BlockId = table.Column<int>(type: "int", nullable: true),
                    BlockName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Block", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "District",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictID = table.Column<int>(type: "int", nullable: false),
                    DistrictName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    DistrictShort = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Division = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_District", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ServiceRelated = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "HalqaPanchayat",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockId = table.Column<int>(type: "int", nullable: true),
                    PanchayatName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HalqaPanchayat", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "OfficersDesignations",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Designation = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    DesignationShort = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficersDesignations", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Pincode",
                columns: table => new
                {
                    pincode_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pincode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pincode", x => x.pincode_id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Department = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FormElement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkForceOfficers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__C51BB00A0C58849B", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "Tehsil",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictID = table.Column<int>(type: "int", nullable: true),
                    TehsilId = table.Column<int>(type: "int", nullable: true),
                    TehsilName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tehsil", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UserSpecificDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    BackupCodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailValid = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Village",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HalqaPanchayatId = table.Column<int>(type: "int", nullable: true),
                    TehsilId = table.Column<int>(type: "int", nullable: true),
                    VillageName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Village", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Ward",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VillageId = table.Column<int>(type: "int", nullable: true),
                    WardName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ward", x => x.UUID);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CitizenId = table.Column<int>(type: "int", nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ApplicantName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    ApplicantImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MobileNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Relation = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    RelationName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    DateOfBirth = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceSpecific = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PresentAddressId = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    PermanentAddressId = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    BankDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Documents = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdateRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    ApplicationStatus = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__C93A4C99B10E9956", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK__Applicati__Servi__25DB9BFC",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "ServiceId");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationsHistory",
                columns: table => new
                {
                    UUID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    History = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationsHistory", x => x.UUID);
                    table.ForeignKey(
                        name: "FK_ApplicationsHistory_Applications",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ServiceId",
                table: "Applications",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationsHistory_ApplicationId",
                table: "ApplicationsHistory",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "AddressJoins");

            migrationBuilder.DropTable(
                name: "ApplicationPerDistrict");

            migrationBuilder.DropTable(
                name: "ApplicationsHistory");

            migrationBuilder.DropTable(
                name: "Block");

            migrationBuilder.DropTable(
                name: "District");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "HalqaPanchayat");

            migrationBuilder.DropTable(
                name: "OfficersDesignations");

            migrationBuilder.DropTable(
                name: "Pincode");

            migrationBuilder.DropTable(
                name: "Tehsil");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Village");

            migrationBuilder.DropTable(
                name: "Ward");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
