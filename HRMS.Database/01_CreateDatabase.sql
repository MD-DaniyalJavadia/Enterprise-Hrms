-- ============================================================================
-- HRMS Enterprise Database Creation Script (SQL Server 2022)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'HRMS_DB')
BEGIN
    CREATE DATABASE [HRMS_DB];
END
GO

USE [HRMS_DB];
GO
