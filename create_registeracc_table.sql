-- ============================================================
-- RegisterAcc Table — Semester I Student Account Requests
-- Run this script on SmartCampusDb database
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'RegisterAcc'
)
BEGIN
    CREATE TABLE RegisterAcc (
        RegisterAccId   INT IDENTITY(1,1) PRIMARY KEY,
        Full_Name       NVARCHAR(100)  NOT NULL,
        Email           NVARCHAR(150)  NOT NULL,
        Phone           NVARCHAR(20)   NULL,
        Form_No         NVARCHAR(50)   NULL,
        Exam_Seat_No    NVARCHAR(50)   NULL,
        Status          NVARCHAR(20)   NOT NULL DEFAULT 'Pending',   -- Pending | Approved | Rejected
        RejectionReason NVARCHAR(500)  NULL,
        CreatedDateTime DATETIME       NOT NULL DEFAULT GETDATE(),
        ReviewedDateTime DATETIME      NULL,
        ReviewedBy      NVARCHAR(100)  NULL
    );

    PRINT 'RegisterAcc table created successfully.';
END
ELSE
BEGIN
    PRINT 'RegisterAcc table already exists — skipped.';
END
GO
