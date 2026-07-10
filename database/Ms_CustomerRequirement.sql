IF OBJECT_ID(N'dbo.Ms_CustomerRequirement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ms_CustomerRequirement
    (
        CustomerCode_ECC6 nchar(10) NOT NULL,
        CustomerName_ECC6 nvarchar(200) NULL,
        CustomerRequirement nvarchar(max) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Ms_CustomerRequirement_IsActive DEFAULT (1),
        CreatedAt datetime NOT NULL CONSTRAINT DF_Ms_CustomerRequirement_CreatedAt DEFAULT (GETDATE()),
        CrateedBy nvarchar(50) NULL,
        UpdatedAt datetime NULL,
        UpdateBy nvarchar(50) NULL,
        CONSTRAINT PK_Ms_CustomerRequirement PRIMARY KEY (CustomerCode_ECC6)
    );
END;

IF COL_LENGTH(N'dbo.Ms_CustomerRequirement', N'CustomerName_ECC6') IS NULL
BEGIN
    ALTER TABLE dbo.Ms_CustomerRequirement
    ADD CustomerName_ECC6 nvarchar(200) NULL;
END;
