-- Apply to the TemplarCMS database before enabling UserDirectory on existing installations.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.DirectoryUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DirectoryUsers (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DirectoryUsers PRIMARY KEY,
        FirstName nvarchar(100) NOT NULL,
        LastName nvarchar(100) NOT NULL,
        Email nvarchar(254) NOT NULL,
        NormalizedEmail nvarchar(254) NOT NULL,
        Language nvarchar(35) NOT NULL,
        Status nvarchar(20) NOT NULL,
        RolesJson nvarchar(max) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        LastLogin datetimeoffset NULL,
        Revision uniqueidentifier NOT NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DirectoryUsers') AND name = N'IX_DirectoryUsers_NormalizedEmail')
    CREATE UNIQUE INDEX IX_DirectoryUsers_NormalizedEmail ON dbo.DirectoryUsers (NormalizedEmail);
COMMIT TRANSACTION;
