-- ============================================================
-- NewStudentAcc Table — Semester I New Student Accounts
-- Stored separately from User table
-- Run this in SQL Server Management Studio (SSMS)
-- ============================================================

CREATE TABLE [dbo].[NewStudentAcc] (
    [NewStudentAccId]    INT            IDENTITY(1,1) NOT NULL,
    [RegisterAccId]      INT            NULL,
    [Full_Name]          NVARCHAR(100)  NOT NULL,
    [Email]              NVARCHAR(150)  NOT NULL,
    [Phone]              NVARCHAR(20)   NULL,
    [Username]           NVARCHAR(50)   NOT NULL,
    [PasswordHash]       NVARCHAR(255)  NOT NULL,
    [AccountStatus]      NVARCHAR(20)   NOT NULL  CONSTRAINT [DF_NewStudentAcc_AccountStatus] DEFAULT ('Active'),
    [MustChangePassword] BIT            NOT NULL  CONSTRAINT [DF_NewStudentAcc_MustChangePassword] DEFAULT (1),
    [CreatedDateTime]    DATETIME       NULL      CONSTRAINT [DF_NewStudentAcc_CreatedDateTime] DEFAULT (GETDATE()),
    [CreatedBy]          NVARCHAR(100)  NULL,
    [ModifiedDateTime]   DATETIME       NULL,
    [ModifiedBy]         NVARCHAR(100)  NULL,

    CONSTRAINT [PK_NewStudentAcc] PRIMARY KEY CLUSTERED ([NewStudentAccId] ASC),
    CONSTRAINT [UQ_NewStudentAcc_Username] UNIQUE ([Username])
);
GO

-- Optional: Index on Email for quick lookup
CREATE INDEX [IX_NewStudentAcc_Email] ON [dbo].[NewStudentAcc] ([Email]);
GO

-- Optional: Index on AccountStatus for filtering Active/Inactive
CREATE INDEX [IX_NewStudentAcc_AccountStatus] ON [dbo].[NewStudentAcc] ([AccountStatus]);
GO

PRINT 'NewStudentAcc table created successfully!';
GO
