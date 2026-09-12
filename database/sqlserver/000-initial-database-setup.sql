/*
   Creates TemplarCMS authoring and published databases.
   Idempotent: skips creation if the database already exists.
*/

IF DB_ID(N'templarcms_authoring') IS NULL
BEGIN
    CREATE DATABASE [templarcms_authoring]
    ON PRIMARY
    (
        NAME = N'templarcms_authoring',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\templarcms_authoring.mdf'
    )
    LOG ON
    (
        NAME = N'templarcms_authoring_log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\templarcms_authoring_log.ldf'
    );
END;
GO

IF DB_ID(N'templarcms_published') IS NULL
BEGIN
    CREATE DATABASE [templarcms_published]
    ON PRIMARY
    (
        NAME = N'templarcms_published',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\templarcms_published.mdf'
    )
    LOG ON
    (
        NAME = N'templarcms_published_log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\templarcms_published_log.ldf'
    );
END;
GO