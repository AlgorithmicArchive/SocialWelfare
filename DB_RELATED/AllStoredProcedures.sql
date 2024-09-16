CREATE PROCEDURE [dbo].[CheckAndInsertAddress]
    @DistrictId INT,
    @TehsilId INT,
    @BlockId INT,
    @HalqaPanchayatName VARCHAR(255),
    @VillageName VARCHAR(255),
    @WardName VARCHAR(255),
    @Pincode INT,
    @AddressDetails VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HalqaPanchayatId INT,
            @VillageId INT,
            @WardId INT,
            @PincodeId INT;

    -- Check if HalqaPanchayat exists, if not, insert
    SELECT @HalqaPanchayatId = UUID 
    FROM dbo.HalqaPanchayat 
    WHERE PanchayatName = @HalqaPanchayatName AND BlockId = @BlockId;

    IF @HalqaPanchayatId IS NULL
    BEGIN
        INSERT INTO dbo.HalqaPanchayat (BlockId, PanchayatName)
        VALUES (@BlockId, @HalqaPanchayatName);
        SET @HalqaPanchayatId = SCOPE_IDENTITY();
    END

    -- Check if Village exists, if not, insert
    SELECT @VillageId = UUID
    FROM dbo.Village
    WHERE VillageName = @VillageName AND HalqaPanchayatId = @HalqaPanchayatId AND TehsilId = @TehsilId;

    IF @VillageId IS NULL
    BEGIN
        INSERT INTO dbo.Village (HalqaPanchayatId, TehsilId, VillageName)
        VALUES (@HalqaPanchayatId, @TehsilId, @VillageName);
        SET @VillageId = SCOPE_IDENTITY();
    END

    -- Check if Ward exists, if not, insert
    SELECT @WardId = UUID
    FROM dbo.Ward
    WHERE WardName = @WardName AND VillageId = @VillageId;

    IF @WardId IS NULL
    BEGIN
        INSERT INTO dbo.Ward (VillageId, WardName)
        VALUES (@VillageId, @WardName);
        SET @WardId = SCOPE_IDENTITY();
    END

    -- Check if Pincode exists, if not, insert
    SELECT @PincodeId = pincode_id
    FROM dbo.Pincode
    WHERE Pincode = @Pincode;

    IF @PincodeId IS NULL
    BEGIN
        INSERT INTO dbo.Pincode (Pincode)
        VALUES (@Pincode);
        SET @PincodeId = SCOPE_IDENTITY();
    END

    -- Insert into Address table
    INSERT INTO dbo.Address (
        DistrictId, TehsilId, BlockId, HalqaPanchayatId, VillageId, WardId, PincodeId, AddressDetails
    )
    VALUES (
        @DistrictId, @TehsilId, @BlockId, @HalqaPanchayatId, @VillageId, @WardId, @PincodeId, @AddressDetails
    );


    SELECT * FROM Address WHERE AddressId = SCOPE_IDENTITY();

END;

CREATE PROCEDURE CheckAndUpdateAddress
    @AddressId INT,
    @DistrictId INT = NULL,
    @TehsilId INT = NULL,
    @BlockId INT = NULL,
    @HalqaPanchayatName VARCHAR(255) = NULL,
    @VillageName VARCHAR(255) = NULL,
    @WardName VARCHAR(255) = NULL,
    @Pincode INT = NULL,
    @AddressDetails VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HalqaPanchayatId INT,
            @VillageId INT,
            @WardId INT,
            @PincodeId INT;

    -- Check for HalqaPanchayatName if provided
    IF @HalqaPanchayatName IS NOT NULL
    BEGIN
        SELECT @HalqaPanchayatId = UUID 
        FROM dbo.HalqaPanchayat 
        WHERE PanchayatName = @HalqaPanchayatName AND BlockId = ISNULL(@BlockId, BlockId);

        IF @HalqaPanchayatId IS NULL
        BEGIN
            INSERT INTO dbo.HalqaPanchayat (BlockId, PanchayatName)
            VALUES (@BlockId, @HalqaPanchayatName);
            SET @HalqaPanchayatId = SCOPE_IDENTITY();
        END
    END

    -- Check for VillageName if provided
    IF @VillageName IS NOT NULL
    BEGIN
        SELECT @VillageId = UUID
        FROM dbo.Village
        WHERE VillageName = @VillageName AND HalqaPanchayatId = ISNULL(@HalqaPanchayatId, HalqaPanchayatId);

        IF @VillageId IS NULL
        BEGIN
            INSERT INTO dbo.Village (HalqaPanchayatId, TehsilId, VillageName)
            VALUES (@HalqaPanchayatId, @TehsilId, @VillageName);
            SET @VillageId = SCOPE_IDENTITY();
        END
    END

    -- Check for WardName if provided
    IF @WardName IS NOT NULL
    BEGIN
        SELECT @WardId = UUID
        FROM dbo.Ward
        WHERE WardName = @WardName AND VillageId = ISNULL(@VillageId, VillageId);

        IF @WardId IS NULL
        BEGIN
            INSERT INTO dbo.Ward (VillageId, WardName)
            VALUES (@VillageId, @WardName);
            SET @WardId = SCOPE_IDENTITY();
        END
    END

    -- Check for Pincode if provided
    IF @Pincode IS NOT NULL
    BEGIN
        SELECT @PincodeId = pincode_id
        FROM dbo.Pincode
        WHERE Pincode = @Pincode;

        IF @PincodeId IS NULL
        BEGIN
            INSERT INTO dbo.Pincode (Pincode)
            VALUES (@Pincode);
            SET @PincodeId = SCOPE_IDENTITY();
        END
    END

    -- Build dynamic SQL for updating only provided columns
    DECLARE @sql NVARCHAR(MAX) = N'UPDATE dbo.Address SET ';
    DECLARE @params NVARCHAR(MAX) = N'';

    IF @DistrictId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'DistrictId = @DistrictId, ';
        SET @params = @params + N'@DistrictId INT, ';
    END

    IF @TehsilId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'TehsilId = @TehsilId, ';
        SET @params = @params + N'@TehsilId INT, ';
    END

    IF @BlockId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'BlockId = @BlockId, ';
        SET @params = @params + N'@BlockId INT, ';
    END

    IF @HalqaPanchayatId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'HalqaPanchayatId = @HalqaPanchayatId, ';
        SET @params = @params + N'@HalqaPanchayatId INT, ';
    END

    IF @VillageId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'VillageId = @VillageId, ';
        SET @params = @params + N'@VillageId INT, ';
    END

    IF @WardId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'WardId = @WardId, ';
        SET @params = @params + N'@WardId INT, ';
    END

    IF @PincodeId IS NOT NULL
    BEGIN
        SET @sql = @sql + N'PincodeId = @PincodeId, ';
        SET @params = @params + N'@PincodeId INT, ';
    END

    IF @AddressDetails IS NOT NULL
    BEGIN
        SET @sql = @sql + N'AddressDetails = @AddressDetails, ';
        SET @params = @params + N'@AddressDetails VARCHAR(MAX), ';
    END

    -- Remove trailing comma and space
    SET @sql = LEFT(@sql, LEN(@sql) - 2);

    -- Complete the SQL statement
    SET @sql = @sql + N' WHERE AddressId = @AddressId';
    SET @params = @params + N'@AddressId INT';

    -- Execute the dynamic SQL
    EXEC sp_executesql @sql, @params,
        @DistrictId = @DistrictId,
        @TehsilId = @TehsilId,
        @BlockId = @BlockId,
        @HalqaPanchayatId = @HalqaPanchayatId,
        @VillageId = @VillageId,
        @WardId = @WardId,
        @PincodeId = @PincodeId,
        @AddressDetails = @AddressDetails,
        @AddressId = @AddressId;

END;

CREATE PROCEDURE GetAddressDetails
    @AddressId INT = NULL -- Optional parameter to filter by AddressId
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        a.AddressId,
        a.AddressDetails AS Address,
        d.DistrictName AS District,
        d.DistrictId,
        t.TehsilName AS Tehsil,
        t.TehsilId,
        b.BlockName AS Block,
        b.BlockId,
        hp.PanchayatName AS PanchayatMuncipality,
        v.VillageName AS Village,
        w.WardName AS Ward,
        p.Pincode
    FROM 
        dbo.Address a
    INNER JOIN 
        dbo.District d ON a.DistrictId = d.DistrictId
    LEFT JOIN 
        dbo.Tehsil t ON a.TehsilId = t.TehsilId
    LEFT JOIN 
        dbo.Block b ON a.BlockId = b.BlockId
    LEFT JOIN 
        dbo.HalqaPanchayat hp ON a.HalqaPanchayatId = hp.UUID
    LEFT JOIN 
        dbo.Village v ON a.VillageId = v.UUID
    LEFT JOIN 
        dbo.Ward w ON a.WardId = w.UUID
    LEFT JOIN 
        dbo.Pincode p ON a.PincodeId = p.pincode_id
    WHERE 
        (@AddressId IS NULL OR a.AddressId = @AddressId);
END;

CREATE PROCEDURE [dbo].[GetDuplicateAccNo]
    @AccountNumber VARCHAR(50)  -- Input parameter to check for duplicate account number
AS
BEGIN
    -- Check if the AccountNumber exists in the JSON BankDetails field
    SELECT *
    FROM [dbo].[Applications]
    WHERE JSON_VALUE(BankDetails, '$.AccountNumber') = @AccountNumber;
    
    -- If no rows are returned, there is no duplicate; otherwise, duplicates exist.
END;

CREATE PROCEDURE InsertGeneralApplicationDetails
    @ApplicationId NVARCHAR(50),
    @CitizenId INT,
    @ServiceId INT,
    @ApplicantName NVARCHAR(255),
    @ApplicantImage NVARCHAR(MAX),
    @Email NVARCHAR(255),
    @MobileNumber NVARCHAR(15),
    @Relation NVARCHAR(50),
    @RelationName NVARCHAR(255),
    @DateOfBirth DATE,
    @Category NVARCHAR(50),
    @ServiceSpecific NVARCHAR(MAX),
    @BankDetails NVARCHAR(MAX),
    @Documents NVARCHAR(MAX),
    @ApplicationStatus NVARCHAR(50)
AS
BEGIN
    -- Ensure you handle any potential SQL injection here.
    -- Insert the details into the ApplicationDetails table (example name).
    INSERT INTO Applications
    (
        ApplicationId,
        CitizenId,
        ServiceId,
        ApplicantName,
        ApplicantImage,
        Email,
        MobileNumber,
        Relation,
        RelationName,
        DateOfBirth,
        Category,
        ServiceSpecific,
        BankDetails,
        Documents,
        ApplicationStatus
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
        @ApplicationStatus
    );
END;

CREATE PROCEDURE [dbo].[RegisterUser]
    @Username NVARCHAR(100),
    @Password NVARCHAR(100),
    @Email NVARCHAR(100),
    @MobileNumber NVARCHAR(20),
    @UserSpecificDetails NVARCHAR(MAX),
    @UserType NVARCHAR(50),
    @BackupCodes NVARCHAR(MAX),
    @RegisteredDate NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;

    -- Hash the password (example using a simple SHA-256 hash; adjust as needed)
    DECLARE @HashedPassword VARBINARY(64); -- Adjust size as needed for SHA-256
    SET @HashedPassword = HASHBYTES('SHA2_256', @Password);

    -- Insert the user record into the Users table
    INSERT INTO Users (Username, [Password], Email, MobileNumber, UserSpecificDetails, UserType, BackupCodes,RegisteredDate)
    VALUES (@Username, @HashedPassword, @Email, @MobileNumber, @UserSpecificDetails, @UserType, @BackupCodes,@RegisteredDate);

        -- Return a success result with the new UserId (assuming UserId is auto-incremented)
    SELECT * FROM Users WHERE UserId = SCOPE_IDENTITY();
   
END;

CREATE PROCEDURE UpdateApplication
    @ColumnName NVARCHAR(255),
    @ColumnValue NVARCHAR(MAX),
    @ApplicationId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Construct the dynamic SQL statement
    DECLARE @Sql NVARCHAR(MAX);

    -- Ensure the column name and value are safe (basic validation)
    IF @ColumnName IS NULL OR @ApplicationId IS NULL
    BEGIN
        RAISERROR('ColumnName and ApplicationId cannot be NULL', 16, 1);
        RETURN;
    END

    -- Construct the SQL update command
    SET @Sql = N'UPDATE Applications
                 SET ' + QUOTENAME(@ColumnName) + ' = @ColumnValue
                 WHERE ApplicationId = @ApplicationId';

    -- Execute the dynamic SQL command
    EXEC sp_executesql @Sql, 
        N'@ColumnValue NVARCHAR(MAX), @ApplicationId NVARCHAR(255)',
        @ColumnValue, @ApplicationId;
END;

CREATE PROCEDURE UpdateApplicationColumns
    @ApplicationId VARCHAR(50),
    @CitizenId INT = NULL,
    @ServiceId INT = NULL,
    @ApplicantName VARCHAR(255) = NULL,
    @ApplicantImage VARCHAR(MAX) = NULL,
    @Email VARCHAR(50) = NULL,
    @MobileNumber VARCHAR(50) = NULL,
    @Relation VARCHAR(50) = NULL,
    @RelationName VARCHAR(255) = NULL,
    @DateOfBirth VARCHAR(50) = NULL,
    @Category VARCHAR(100) = NULL,
    @ServiceSpecific VARCHAR(MAX) = NULL,
    @PresentAddressId VARCHAR(20) = NULL,
    @PermanentAddressId VARCHAR(20) = NULL,
    @BankDetails VARCHAR(MAX) = NULL,
    @Documents VARCHAR(MAX) = NULL,
    @EditList VARCHAR(MAX) = NULL,
    @Phase INT = NULL,
    @ApplicationStatus VARCHAR(15) = NULL,
    @SubmissionDate VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Construct dynamic SQL
    DECLARE @Sql NVARCHAR(MAX);

    SET @Sql = 'UPDATE Applications SET ';

    -- Check and append columns dynamically
    IF @CitizenId IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'CitizenId = @CitizenId, ';
    END
    IF @ServiceId IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'ServiceId = @ServiceId, ';
    END
    IF @ApplicantName IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'ApplicantName = @ApplicantName, ';
    END
    IF @ApplicantImage IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'ApplicantImage = @ApplicantImage, ';
    END
    IF @Email IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'Email = @Email, ';
    END
    IF @MobileNumber IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'MobileNumber = @MobileNumber, ';
    END
    IF @Relation IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'Relation = @Relation, ';
    END
    IF @RelationName IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'RelationName = @RelationName, ';
    END
    IF @DateOfBirth IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'DateOfBirth = @DateOfBirth, ';
    END
    IF @Category IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'Category = @Category, ';
    END
    IF @ServiceSpecific IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'ServiceSpecific = @ServiceSpecific, ';
    END
    IF @PresentAddressId IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'PresentAddressId = @PresentAddressId, ';
    END
    IF @PermanentAddressId IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'PermanentAddressId = @PermanentAddressId, ';
    END
    IF @BankDetails IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'BankDetails = @BankDetails, ';
    END
    IF @Documents IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'Documents = @Documents, ';
    END
    IF @EditList IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'EditList = @EditList, ';
    END
    IF @Phase IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'Phase = @Phase, ';
    END
    IF @ApplicationStatus IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'ApplicationStatus = @ApplicationStatus, ';
    END
    IF @SubmissionDate IS NOT NULL
    BEGIN
        SET @Sql = @Sql + 'SubmissionDate = @SubmissionDate, ';
    END

    -- Remove trailing comma and space
    SET @Sql = LEFT(@Sql, LEN(@Sql) - 2);

    SET @Sql = @Sql + ' WHERE ApplicationId = @ApplicationId';

    -- Execute dynamic SQL
    EXEC sp_executesql @Sql, 
        N'@ApplicationId VARCHAR(50), @CitizenId INT, @ServiceId INT, @ApplicantName VARCHAR(255), @ApplicantImage VARCHAR(MAX), @Email VARCHAR(50), @MobileNumber VARCHAR(50), @Relation VARCHAR(50), @RelationName VARCHAR(255), @DateOfBirth VARCHAR(50), @Category VARCHAR(100), @ServiceSpecific VARCHAR(MAX), @PresentAddressId VARCHAR(20), @PermanentAddressId VARCHAR(20), @BankDetails VARCHAR(MAX), @Documents VARCHAR(MAX), @EditList VARCHAR(MAX), @Phase INT, @ApplicationStatus VARCHAR(15), @SubmissionDate VARCHAR(100)',
        @ApplicationId, @CitizenId, @ServiceId, @ApplicantName, @ApplicantImage, @Email, @MobileNumber, @Relation, @RelationName, @DateOfBirth, @Category, @ServiceSpecific, @PresentAddressId, @PermanentAddressId, @BankDetails, @Documents, @EditList, @Phase, @ApplicationStatus, @SubmissionDate;
END;

CREATE PROCEDURE [dbo].[UserLogin]
    @Username NVARCHAR(50),
    @Password NVARCHAR(50)
AS
BEGIN
    -- Declare a variable to hold the hashed password
    DECLARE @PasswordHash VARBINARY(64);
    
    -- Hash the input password using SHA2_256 (or SHA2_512)
    SET @PasswordHash = HASHBYTES('SHA2_256', @Password);

    -- Retrieve user details where the username matches and the hashed password matches
    SELECT *
    FROM Users
    WHERE Username = @Username AND [Password] = @PasswordHash;
END;