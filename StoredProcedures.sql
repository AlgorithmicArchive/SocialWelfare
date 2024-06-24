SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckAndInsertAddress]
    @DistrictId INT,
    @TehsilId INT,
    @BlockId INT,
    @HalqaPanchayatName NVARCHAR(255),
    @VillageName NVARCHAR(255),
    @WardName NVARCHAR(255),
    @Pincode NVARCHAR(50),
    @AddressDetails NVARCHAR(MAX)
AS
BEGIN
    DECLARE @HalqaPanchayatId INT;
    DECLARE @VillageId INT;
    DECLARE @WardId INT;
    DECLARE @PincodeId INT;

    -- Check or insert HalqaPanchayat
    IF EXISTS (SELECT 1 FROM [dbo].[HalqaPanchayat] WHERE PanchayatName = @HalqaPanchayatName)
    BEGIN
        SELECT @HalqaPanchayatId = UUID FROM [dbo].[HalqaPanchayat] WHERE PanchayatName = @HalqaPanchayatName;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[HalqaPanchayat] (BlockId, PanchayatName) VALUES (@BlockId, @HalqaPanchayatName);
        SET @HalqaPanchayatId = SCOPE_IDENTITY();
    END

    -- Check or insert Village
    IF EXISTS (SELECT 1 FROM [dbo].[Village] WHERE VillageName = @VillageName)
    BEGIN
        SELECT @VillageId = UUID FROM [dbo].[Village] WHERE VillageName = @VillageName;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[Village] (HalqaPanchayatId, TehsilId, VillageName) VALUES (@HalqaPanchayatId, @TehsilId, @VillageName);
        SET @VillageId = SCOPE_IDENTITY();
    END

    -- Check or insert Ward
    IF EXISTS (SELECT 1 FROM [dbo].[Ward] WHERE WardName = @WardName)
    BEGIN
        SELECT @WardId = UUID FROM [dbo].[Ward] WHERE WardName = @WardName;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[Ward] (VillageId, WardName) VALUES (@VillageId, @WardName);
        SET @WardId = SCOPE_IDENTITY();
    END

    -- Check or insert Pincode
    IF EXISTS (SELECT 1 FROM [dbo].[Pincode] WHERE Pincode = @Pincode)
    BEGIN
        SELECT @PincodeId = pincode_id FROM [dbo].[Pincode] WHERE Pincode = @Pincode;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[Pincode] (Pincode) VALUES (@Pincode);
        SET @PincodeId = SCOPE_IDENTITY();
    END

    -- Insert the new address
    INSERT INTO [dbo].[Address] ([DistrictId], [TehsilId], [BlockId], [HalqaPanchayatId], [VillageId], [WardId], [PincodeId], [AddressDetails])
    VALUES (@DistrictId, @TehsilId, @BlockId, @HalqaPanchayatId, @VillageId, @WardId, @PincodeId, @AddressDetails);

    -- Return the newly inserted address
    SELECT * 
    FROM [dbo].[Address]
    WHERE [AddressId] = SCOPE_IDENTITY();
END
GO

------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CountPerDistrict]
    @DistrictId INT,
    @FinancialYear VARCHAR(50)
AS
BEGIN
    SELECT *
    FROM [dbo].[ApplicationPerDistrict]
    WHERE [DistrictId] = @DistrictId
      AND [FinancialYear] = @FinancialYear
END
GO

---------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetAddressDetails]
    @AddressId INT
AS
BEGIN
    SELECT 
        a.AddressId,
        a.AddressDetails AS Address,
        d.DistrictName AS District,
        d.DistrictID AS DistrictId,
        t.TehsilName AS Tehsil,
        t.TehsilId AS TehsilId,
        b.BlockName AS Block,
        b.BlockId AS BlockId,
        hp.PanchayatName AS PanchayatMuncipality,
        v.VillageName AS Village,
        w.WardName AS Ward,
        p.Pincode AS Pincode
    FROM 
        [dbo].[Address] a
    LEFT JOIN 
        [dbo].[District] d ON a.DistrictId = d.DistrictID
    LEFT JOIN 
        [dbo].[Tehsil] t ON a.TehsilId = t.TehsilId
    LEFT JOIN 
        [dbo].[Block] b ON a.BlockId = b.BlockId
    LEFT JOIN 
        [dbo].[HalqaPanchayat] hp ON a.HalqaPanchayatId = hp.UUID
    LEFT JOIN 
        [dbo].[Village] v ON a.VillageId = v.UUID
    LEFT JOIN 
        [dbo].[Ward] w ON a.WardId = w.UUID
    LEFT JOIN 
        [dbo].[Pincode] p ON a.PincodeId = p.pincode_id
    WHERE 
        a.AddressId = @AddressId;
END
GO

---------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetApplicationCountForOfficer]
  @OfficerDesignation VARCHAR(255),
  @District VARCHAR(20)
AS
BEGIN
SELECT DISTINCT  a.*
FROM Applications a
CROSS APPLY OPENJSON(a.Phase) AS app
WHERE
    JSON_VALUE(app.value, '$.Officer') = @OfficerDesignation AND
    JSON_VALUE(a.ServiceSpecific, '$.District') = @District;
END;
GO

------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetApplications]
    @Condition1 NVARCHAR(MAX),
    @Condition2 NVARCHAR(MAX)
AS
BEGIN
    DECLARE @SQL NVARCHAR(MAX);

    SET @SQL = N'
        SELECT DISTINCT  a.*
        FROM Applications a
        CROSS APPLY OPENJSON(a.Phase) AS app
        WHERE 1 = 1 ' + @Condition1 + @Condition2;

    EXEC sp_executesql @SQL;
END;
GO

--------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetApplicationsForOfficer]
  @OfficerDesignation VARCHAR(255),
  @ActionTaken VARCHAR(15),
  @District VARCHAR(20),
  @ServiceId INT
AS
BEGIN

DECLARE @ActionTakenTable TABLE (Value VARCHAR(15));
INSERT INTO @ActionTakenTable (Value)
SELECT value FROM STRING_SPLIT(@ActionTaken, ',');


SELECT DISTINCT  a.*
FROM Applications a
CROSS APPLY OPENJSON(a.Phase) AS app
WHERE
    JSON_VALUE(app.value, '$.Officer') = @OfficerDesignation 
    AND JSON_VALUE(app.value,'$.ActionTaken') IN (SELECT Value FROM @ActionTakenTable) 
    AND JSON_VALUE(a.ServiceSpecific, '$.District') = @District
    AND a.ServiceId = @ServiceId;
END;
GO


------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertGneralApplicationDetails]
    @ApplicationId VARCHAR(50),
    @CitizenId INT,
    @ServiceId INT,
    @ApplicantName VARCHAR(255),
    @ApplicantImage NVARCHAR(MAX),
    @Email VARCHAR(50),
    @MobileNumber VARCHAR(50),
    @Relation VARCHAR(50),
    @RelationName VARCHAR(255),
    @DateOfBirth VARCHAR(50),
    @Category NVARCHAR(50),
    @ServiceSpecific NVARCHAR(MAX),
    @BankDetails NVARCHAR(MAX),
    @Documents NVARCHAR(MAX),
    @ApplicationStatus VARCHAR(15)
AS
BEGIN
    INSERT INTO [dbo].[Applications]
    (
        [ApplicationId],
        [CitizenId],
        [ServiceId],
        [ApplicantName],
        [ApplicantImage],
        [Email],
        [MobileNumber],
        [Relation],
        [RelationName],
        [DateOfBirth],
        [Category],
        [ServiceSpecific],
        [BankDetails],
        [Documents],
        [ApplicationStatus],
        [SubmissionDate]
    )
    VALUES
    (
        @ApplicationId,
        @CitizenId,
        @ServiceId,
        @ApplicantName,
        @ApplicantImage,
        @Email,
        @MobileNumber,
        @Relation,
        @RelationName,
        @DateOfBirth,
        @Category,
        @ServiceSpecific,
        @BankDetails,
        @Documents,
        @ApplicationStatus,
        GETDATE()
    )
END
GO

------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[RegisterUser]
    @Username NVARCHAR(50),
    @Email NVARCHAR(50),
    @Password NVARCHAR(100),
    @MobileNumber NVARCHAR(20),
    @UserSpecificDetails NVARCHAR(MAX),
    @UserType NVARCHAR(50),
    @BackupCodes NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT;
    DECLARE @HashedPassword VARBINARY(64);

    -- Hash the password using SHA2_256
    SET @HashedPassword = HASHBYTES('SHA2_256', @Password);

    -- Insert the user details into the Users table
    INSERT INTO Users (Username, Email, Password, MobileNumber, UserSpecificDetails, UserType, BackupCodes)
    VALUES (@Username, @Email, CONVERT(NVARCHAR(100), @HashedPassword, 1), @MobileNumber, @UserSpecificDetails, @UserType, @BackupCodes);

    -- Get the UserId of the newly inserted user
    SELECT @UserId = SCOPE_IDENTITY();

    -- Return the UserId of the newly registered user
    SELECT * FROM Users WHERE UserId = @UserId;
END
GO

------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateApplication]
    @ColumnName NVARCHAR(MAX),
    @ColumnValue NVARCHAR(MAX),
    @ApplicationId VARCHAR(50)
AS
BEGIN
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = N'UPDATE [dbo].[Applications] SET ' + QUOTENAME(@ColumnName) + N' = @ColumnValue WHERE [ApplicationId] = @ApplicationId';

    EXEC sp_executesql @SQL, N'@ColumnValue NVARCHAR(MAX), @ApplicationId VARCHAR(50)', @ColumnValue, @ApplicationId;
END
GO


-------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UserLogin]
    @Username NVARCHAR(50),
    @Password NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HashedPassword VARBINARY(64);

    -- Hash the input password using SHA2_256
    SET @HashedPassword = HASHBYTES('SHA2_256', @Password);

    -- Select the user details where the username matches and the hashed password matches
    SELECT 
       *
    FROM 
        Users 
    WHERE 
        Username = @Username 
        AND Password = CONVERT(NVARCHAR(100), @HashedPassword, 1);
END
GO


--------------------------------------------------------