CREATE TABLE SocialWelfareDepartment.dbo.Address (
	AddressId int IDENTITY(1,1) NOT NULL,
	DistrictId int NOT NULL,
	TehsilId int NOT NULL,
	BlockId int NOT NULL,
	HalqaPanchayatId int NOT NULL,
	VillageId int NOT NULL,
	WardId int NOT NULL,
	PincodeId int NOT NULL,
	AddressDetails varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Address PRIMARY KEY (AddressId)
);


-- SocialWelfareDepartment.dbo.ApplicationList definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.ApplicationList;

CREATE TABLE SocialWelfareDepartment.dbo.ApplicationList (
	UUID int IDENTITY(1,1) NOT NULL,
	ServiceId int NOT NULL,
	Officer varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessLevel varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessCode int NOT NULL,
	ApprovalList varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	PoolList varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_ApplicationList PRIMARY KEY (UUID)
);


-- SocialWelfareDepartment.dbo.District definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.District;

CREATE TABLE SocialWelfareDepartment.dbo.District (
	DistrictID int NOT NULL,
	DistrictName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DistrictShort varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Division int NOT NULL,
	CONSTRAINT PK_District PRIMARY KEY (DistrictID)
);


-- SocialWelfareDepartment.dbo.OfficersDesignations definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.OfficersDesignations;

CREATE TABLE SocialWelfareDepartment.dbo.OfficersDesignations (
	UUID int IDENTITY(1,1) NOT NULL,
	Designation varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DesignationShort varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessLevel varchar(40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_OfficersDesignations PRIMARY KEY (UUID)
);


-- SocialWelfareDepartment.dbo.Pincode definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Pincode;

CREATE TABLE SocialWelfareDepartment.dbo.Pincode (
	pincode_id int IDENTITY(1,1) NOT NULL,
	Pincode int NOT NULL,
	CONSTRAINT PK_Pincode PRIMARY KEY (pincode_id)
);


-- SocialWelfareDepartment.dbo.Services definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Services;

CREATE TABLE SocialWelfareDepartment.dbo.Services (
	ServiceId int IDENTITY(1,1) NOT NULL,
	ServiceName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Department varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FormElement varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WorkForceOfficers varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UpdateColumn varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LetterUpdateDetails varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CreatedAt decimal(23,3) NOT NULL,
	Active bit DEFAULT 0 NOT NULL,
	CONSTRAINT PK_Services PRIMARY KEY (ServiceId)
);


-- SocialWelfareDepartment.dbo.UniqueIDTable definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.UniqueIDTable;

CREATE TABLE SocialWelfareDepartment.dbo.UniqueIDTable (
	DistrictNameShort varchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	MonthShort varchar(3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	LastSequentialNumber bigint NOT NULL
);


-- SocialWelfareDepartment.dbo.Users definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Users;

CREATE TABLE SocialWelfareDepartment.dbo.Users (
	UserId int IDENTITY(1,1) NOT NULL,
	Username varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Email varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Password varbinary(MAX) NOT NULL,
	MobileNumber varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	UserSpecificDetails varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	UserType varchar(30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	BackupCodes varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	EmailValid bit DEFAULT 0 NOT NULL,
	RegisteredDate nvarchar(120) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Users PRIMARY KEY (UserId)
);


-- SocialWelfareDepartment.dbo.ApplicationPerDistrict definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.ApplicationPerDistrict;

CREATE TABLE SocialWelfareDepartment.dbo.ApplicationPerDistrict (
	UUID int IDENTITY(1,1) NOT NULL,
	DistrictId int NOT NULL,
	FinancialYear varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CountValue int NOT NULL,
	CONSTRAINT PK_ApplicationPerDistrict PRIMARY KEY (UUID),
	CONSTRAINT FK_ApplicationPerDistrict_District FOREIGN KEY (DistrictId) REFERENCES SocialWelfareDepartment.dbo.District(DistrictID)
);


-- SocialWelfareDepartment.dbo.Applications definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Applications;

CREATE TABLE SocialWelfareDepartment.dbo.Applications (
	ApplicationId varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CitizenId int NOT NULL,
	ServiceId int NOT NULL,
	ApplicantName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ApplicantImage varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Email varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	MobileNumber varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Relation varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	RelationName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DateOfBirth varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Category varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ServiceSpecific varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	PresentAddressId varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT '0' NOT NULL,
	PermanentAddressId varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT '0' NOT NULL,
	BankDetails varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Documents varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	EditList varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT '[]' NOT NULL,
	Phase int DEFAULT 0 NOT NULL,
	ApplicationStatus varchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	SubmissionDate nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT getdate() NOT NULL,
	CONSTRAINT PK_Applications PRIMARY KEY (ApplicationId),
	CONSTRAINT FK_Applications_Services FOREIGN KEY (ServiceId) REFERENCES SocialWelfareDepartment.dbo.Services(ServiceId),
	CONSTRAINT FK_Applications_Users FOREIGN KEY (CitizenId) REFERENCES SocialWelfareDepartment.dbo.Users(UserId)
);


-- SocialWelfareDepartment.dbo.ApplicationsHistory definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.ApplicationsHistory;

CREATE TABLE SocialWelfareDepartment.dbo.ApplicationsHistory (
	UUID int IDENTITY(1,1) NOT NULL,
	ApplicationId varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	History varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_ApplicationsHistory PRIMARY KEY (UUID),
	CONSTRAINT FK_ApplicationsHistory_Applications FOREIGN KEY (ApplicationId) REFERENCES SocialWelfareDepartment.dbo.Applications(ApplicationId)
);


-- SocialWelfareDepartment.dbo.BankFiles definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.BankFiles;

CREATE TABLE SocialWelfareDepartment.dbo.BankFiles (
	FileId int IDENTITY(1,1) NOT NULL,
	DistrictId int NOT NULL,
	ServiceId int NOT NULL,
	FileName varchar(510) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	GeneratedDate varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	TotalRecords int NOT NULL,
	FileSent bit NOT NULL,
	ResponseFile varchar(510) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_BankFiles PRIMARY KEY (FileId),
	CONSTRAINT FK_BankFiles_District FOREIGN KEY (DistrictId) REFERENCES SocialWelfareDepartment.dbo.District(DistrictID),
	CONSTRAINT FK_BankFiles_Services FOREIGN KEY (ServiceId) REFERENCES SocialWelfareDepartment.dbo.Services(ServiceId)
);


-- SocialWelfareDepartment.dbo.Block definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Block;

CREATE TABLE SocialWelfareDepartment.dbo.Block (
	DistrictID int NOT NULL,
	BlockId int NOT NULL,
	BlockName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Block PRIMARY KEY (BlockId),
	CONSTRAINT FK_Block_District FOREIGN KEY (DistrictID) REFERENCES SocialWelfareDepartment.dbo.District(DistrictID)
);


-- SocialWelfareDepartment.dbo.Certificates definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Certificates;

CREATE TABLE SocialWelfareDepartment.dbo.Certificates (
	UUID int IDENTITY(1,1) NOT NULL,
	OfficerId int NOT NULL,
	EncryptedCertificateData varbinary(MAX) NOT NULL,
	EncryptedPassword varbinary(MAX) NOT NULL,
	encryptionKey varbinary(MAX) NOT NULL,
	encryptionIV varbinary(MAX) NOT NULL,
	RegisteredDate nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT getdate() NOT NULL,
	CONSTRAINT PK_Certificates PRIMARY KEY (UUID),
	CONSTRAINT FK_Certificates_Users FOREIGN KEY (OfficerId) REFERENCES SocialWelfareDepartment.dbo.Users(UserId)
);


-- SocialWelfareDepartment.dbo.CurrentPhase definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.CurrentPhase;

CREATE TABLE SocialWelfareDepartment.dbo.CurrentPhase (
	PhaseId int IDENTITY(1,1) NOT NULL,
	ApplicationId varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	ReceivedOn varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Officer varchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessCode int NOT NULL,
	ActionTaken varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Remarks varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[File] varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS DEFAULT 'NIL' NULL,
	CanPull bit NOT NULL,
	Previous int NOT NULL,
	[Next] int NOT NULL,
	CONSTRAINT PK_CurrentPhase PRIMARY KEY (PhaseId),
	CONSTRAINT FK_CurrentPhase_Applications FOREIGN KEY (ApplicationId) REFERENCES SocialWelfareDepartment.dbo.Applications(ApplicationId)
);


-- SocialWelfareDepartment.dbo.Feedback definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Feedback;

CREATE TABLE SocialWelfareDepartment.dbo.Feedback (
	UUID int IDENTITY(1,1) NOT NULL,
	UserId int NOT NULL,
	ServiceRelated varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Message varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	SubmittedAt decimal(23,3) NOT NULL,
	CONSTRAINT PK_Feedback PRIMARY KEY (UUID),
	CONSTRAINT FK_Feedback_Users FOREIGN KEY (UserId) REFERENCES SocialWelfareDepartment.dbo.Users(UserId)
);


-- SocialWelfareDepartment.dbo.HalqaPanchayat definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.HalqaPanchayat;

CREATE TABLE SocialWelfareDepartment.dbo.HalqaPanchayat (
	UUID int IDENTITY(1,1) NOT NULL,
	BlockId int NOT NULL,
	PanchayatName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_HalqaPanchayat PRIMARY KEY (UUID),
	CONSTRAINT FK_HalqaPanchayat_Block FOREIGN KEY (BlockId) REFERENCES SocialWelfareDepartment.dbo.Block(BlockId)
);


-- SocialWelfareDepartment.dbo.Logs definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Logs;

CREATE TABLE SocialWelfareDepartment.dbo.Logs (
	LogId int IDENTITY(1,1) NOT NULL,
	UserId int NOT NULL,
	UserType varchar(30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	IpAddress varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Action] varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DateOfAction varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Logs PRIMARY KEY (LogId),
	CONSTRAINT FK_Logs_Users FOREIGN KEY (UserId) REFERENCES SocialWelfareDepartment.dbo.Users(UserId)
);


-- SocialWelfareDepartment.dbo.RecordCount definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.RecordCount;

CREATE TABLE SocialWelfareDepartment.dbo.RecordCount (
	RecordId int IDENTITY(1,1) NOT NULL,
	ServiceId int NOT NULL,
	Officer varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AccessCode int NOT NULL,
	Pending int NOT NULL,
	PendingWithCitizen int NOT NULL,
	Forward int NOT NULL,
	Sanction int NOT NULL,
	[Return] int NOT NULL,
	Reject int NOT NULL,
	CONSTRAINT PK_RecordCount PRIMARY KEY (RecordId),
	CONSTRAINT FK_RecordCount_Services FOREIGN KEY (ServiceId) REFERENCES SocialWelfareDepartment.dbo.Services(ServiceId)
);


-- SocialWelfareDepartment.dbo.Tehsil definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Tehsil;

CREATE TABLE SocialWelfareDepartment.dbo.Tehsil (
	DistrictID int NOT NULL,
	TehsilId int NOT NULL,
	TehsilName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Tehsil PRIMARY KEY (TehsilId),
	CONSTRAINT FK_Tehsil_District FOREIGN KEY (DistrictID) REFERENCES SocialWelfareDepartment.dbo.District(DistrictID)
);


-- SocialWelfareDepartment.dbo.UpdatedLetterDetails definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.UpdatedLetterDetails;

CREATE TABLE SocialWelfareDepartment.dbo.UpdatedLetterDetails (
	ApplicationId varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	UpdatedDetails varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT FK_UpdatedLetterDetails_Applications FOREIGN KEY (ApplicationId) REFERENCES SocialWelfareDepartment.dbo.Applications(ApplicationId)
);


-- SocialWelfareDepartment.dbo.Village definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Village;

CREATE TABLE SocialWelfareDepartment.dbo.Village (
	UUID int IDENTITY(1,1) NOT NULL,
	HalqaPanchayatId int NOT NULL,
	TehsilId int NOT NULL,
	VillageName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Village PRIMARY KEY (UUID),
	CONSTRAINT FK_Village_HalqaPanchayat FOREIGN KEY (HalqaPanchayatId) REFERENCES SocialWelfareDepartment.dbo.HalqaPanchayat(UUID),
	CONSTRAINT FK_Village_Tehsil FOREIGN KEY (TehsilId) REFERENCES SocialWelfareDepartment.dbo.Tehsil(TehsilId)
);


-- SocialWelfareDepartment.dbo.Ward definition

-- Drop table

-- DROP TABLE SocialWelfareDepartment.dbo.Ward;

CREATE TABLE SocialWelfareDepartment.dbo.Ward (
	UUID int IDENTITY(1,1) NOT NULL,
	VillageId int NOT NULL,
	WardName varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_Ward PRIMARY KEY (UUID),
	CONSTRAINT FK_Ward_Village FOREIGN KEY (VillageId) REFERENCES SocialWelfareDepartment.dbo.Village(UUID)
);