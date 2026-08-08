-- ===============================================================================
-- SMART CAMPUS PUMUB - MS SQL SERVER DATABASE CREATION & SEED SCRIPT
-- ===============================================================================
-- Target Database: SmartCampusDB
-- Usage: Run this script in SQL Server Management Studio (SSMS) or azure-cli / sqlcmd
-- ===============================================================================

USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'SmartCampusDB')
BEGIN
    CREATE DATABASE [SmartCampusDB];
END;
GO

USE [SmartCampusDB];
GO

-- ===============================================================================
-- 1. DROP EXISTING TABLES (IF RE-RUNNING)
-- ===============================================================================
IF OBJECT_ID(N'[dbo].[RoleHierarchy]', N'U') IS NOT NULL DROP TABLE [dbo].[RoleHierarchy];
IF OBJECT_ID(N'[dbo].[RolePermissions]', N'U') IS NOT NULL DROP TABLE [dbo].[RolePermissions];
IF OBJECT_ID(N'[dbo].[Permissions]', N'U') IS NOT NULL DROP TABLE [dbo].[Permissions];
IF OBJECT_ID(N'[dbo].[RegistrationPayments]', N'U') IS NOT NULL DROP TABLE [dbo].[RegistrationPayments];
IF OBJECT_ID(N'[dbo].[StudentRegistrations]', N'U') IS NOT NULL DROP TABLE [dbo].[StudentRegistrations];
IF OBJECT_ID(N'[dbo].[StudentPersonalInfo]', N'U') IS NOT NULL DROP TABLE [dbo].[StudentPersonalInfo];
IF OBJECT_ID(N'[dbo].[Students]', N'U') IS NOT NULL DROP TABLE [dbo].[Students];
IF OBJECT_ID(N'[dbo].[Tutors]', N'U') IS NOT NULL DROP TABLE [dbo].[Tutors];
IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL DROP TABLE [dbo].[Users];
IF OBJECT_ID(N'[dbo].[NewStudentAccs]', N'U') IS NOT NULL DROP TABLE [dbo].[NewStudentAccs];
IF OBJECT_ID(N'[dbo].[RegisterAccounts]', N'U') IS NOT NULL DROP TABLE [dbo].[RegisterAccounts];
IF OBJECT_ID(N'[dbo].[Major]', N'U') IS NOT NULL DROP TABLE [dbo].[Major];
IF OBJECT_ID(N'[dbo].[Subjects]', N'U') IS NOT NULL DROP TABLE [dbo].[Subjects];
IF OBJECT_ID(N'[dbo].[Semesters]', N'U') IS NOT NULL DROP TABLE [dbo].[Semesters];
IF OBJECT_ID(N'[dbo].[Departments]', N'U') IS NOT NULL DROP TABLE [dbo].[Departments];
IF OBJECT_ID(N'[dbo].[Faculty]', N'U') IS NOT NULL DROP TABLE [dbo].[Faculty];
IF OBJECT_ID(N'[dbo].[Positions]', N'U') IS NOT NULL DROP TABLE [dbo].[Positions];
IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NOT NULL DROP TABLE [dbo].[Roles];
IF OBJECT_ID(N'[dbo].[PaymentFees]', N'U') IS NOT NULL DROP TABLE [dbo].[PaymentFees];
IF OBJECT_ID(N'[dbo].[Books]', N'U') IS NOT NULL DROP TABLE [dbo].[Books];
IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NOT NULL DROP TABLE [dbo].[Categories];
IF OBJECT_ID(N'[dbo].[Activity]', N'U') IS NOT NULL DROP TABLE [dbo].[Activity];
IF OBJECT_ID(N'[dbo].[RulesRegulations]', N'U') IS NOT NULL DROP TABLE [dbo].[RulesRegulations];
GO

-- ===============================================================================
-- 2. CREATE TABLES
-- ===============================================================================

-- [Roles]
CREATE TABLE [dbo].[Roles] (
    [Role_Id] INT IDENTITY(1,1) NOT NULL,
    [Role_Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Role_Id] ASC)
);

-- [Faculty]
CREATE TABLE [dbo].[Faculty] (
    [Faculty_Id] INT IDENTITY(1,1) NOT NULL,
    [Faculty_Name] NVARCHAR(200) NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Faculty] PRIMARY KEY CLUSTERED ([Faculty_Id] ASC)
);

-- [Users]
CREATE TABLE [dbo].[Users] (
    [User_Id] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FullName] NVARCHAR(150) NULL,
    [Email] NVARCHAR(150) NULL,
    [Phone] NVARCHAR(50) NULL,
    [Role_id] INT NOT NULL,
    [Faculty_Id] INT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([User_Id] ASC),
    CONSTRAINT [FK_User_Role] FOREIGN KEY ([Role_id]) REFERENCES [dbo].[Roles] ([Role_Id]),
    CONSTRAINT [FK_User_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id])
);

-- [Major]
CREATE TABLE [dbo].[Major] (
    [Major_Id] INT IDENTITY(1,1) NOT NULL,
    [Major_Name] NVARCHAR(200) NOT NULL,
    [Faculty_Id] INT NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Major] PRIMARY KEY CLUSTERED ([Major_Id] ASC),
    CONSTRAINT [FK_Major_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id])
);

-- [Departments]
CREATE TABLE [dbo].[Departments] (
    [Department_Id] INT IDENTITY(1,1) NOT NULL,
    [Department_Name] NVARCHAR(200) NOT NULL,
    [Faculty_Id] INT NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([Department_Id] ASC),
    CONSTRAINT [FK_Department_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id])
);

-- [Positions]
CREATE TABLE [dbo].[Positions] (
    [Position_Id] INT IDENTITY(1,1) NOT NULL,
    [Position_Title] NVARCHAR(100) NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Positions] PRIMARY KEY CLUSTERED ([Position_Id] ASC)
);

-- [Tutors]
CREATE TABLE [dbo].[Tutors] (
    [Tutor_Id] INT IDENTITY(1,1) NOT NULL,
    [User_Id] INT NOT NULL,
    [Department_Id] INT NOT NULL,
    [Position_Id] INT NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Tutors] PRIMARY KEY CLUSTERED ([Tutor_Id] ASC),
    CONSTRAINT [FK_Tutor_User] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[Users] ([User_Id]),
    CONSTRAINT [FK_Tutor_Department] FOREIGN KEY ([Department_Id]) REFERENCES [dbo].[Departments] ([Department_Id]),
    CONSTRAINT [FK_Tutor_Position] FOREIGN KEY ([Position_Id]) REFERENCES [dbo].[Positions] ([Position_Id])
);

-- [Students]
CREATE TABLE [dbo].[Students] (
    [Student_Id] INT IDENTITY(1,1) NOT NULL,
    [User_Id] INT NOT NULL,
    [RollNo] NVARCHAR(50) NULL,
    [Major] NVARCHAR(100) NULL,
    [AcademicYear] NVARCHAR(50) NULL,
    [Status] NVARCHAR(50) NULL DEFAULT 'Active',
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Students] PRIMARY KEY CLUSTERED ([Student_Id] ASC),
    CONSTRAINT [FK_Student_User] FOREIGN KEY ([User_Id]) REFERENCES [dbo].[Users] ([User_Id])
);

-- [Semesters]
CREATE TABLE [dbo].[Semesters] (
    [Semester_Id] INT IDENTITY(1,1) NOT NULL,
    [Semester_Name] NVARCHAR(100) NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Semesters] PRIMARY KEY CLUSTERED ([Semester_Id] ASC)
);

-- [Subjects]
CREATE TABLE [dbo].[Subjects] (
    [Subject_Id] INT IDENTITY(1,1) NOT NULL,
    [Subject_Code] NVARCHAR(50) NOT NULL,
    [Subject_Name] NVARCHAR(200) NOT NULL,
    [Semester_Id] INT NOT NULL,
    [Faculty_Id] INT NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Subjects] PRIMARY KEY CLUSTERED ([Subject_Id] ASC),
    CONSTRAINT [FK_Subject_Semester] FOREIGN KEY ([Semester_Id]) REFERENCES [dbo].[Semesters] ([Semester_Id]),
    CONSTRAINT [FK_Subject_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id])
);

-- [NewStudentAccs]
CREATE TABLE [dbo].[NewStudentAccs] (
    [NewStudentAccId] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [MatricRollNo] NVARCHAR(50) NULL,
    [ExamPassedYear] INT NULL,
    [MustChangePassword] BIT NULL DEFAULT 1,
    [AccountStatus] NVARCHAR(50) NULL DEFAULT 'Active',
    [UserId] INT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_NewStudentAccs] PRIMARY KEY CLUSTERED ([NewStudentAccId] ASC),
    CONSTRAINT [UQ_NewStudentAcc_Username] UNIQUE ([Username])
);

-- [RegisterAccounts]
CREATE TABLE [dbo].[RegisterAccounts] (
    [RegisterAccId] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(150) NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [Status] NVARCHAR(50) NULL DEFAULT 'Pending',
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_RegisterAccounts] PRIMARY KEY CLUSTERED ([RegisterAccId] ASC)
);

-- [StudentPersonalInfo]
CREATE TABLE [dbo].[StudentPersonalInfo] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [NewStudentAccId] INT NULL,
    [AdmissionSerialNo] NVARCHAR(100) NULL,
    [academic_year_range] NVARCHAR(50) NULL,
    [academic_year_level] NVARCHAR(50) NULL,
    [major] NVARCHAR(150) NULL,
    [roll_no] NVARCHAR(50) NULL,
    [university_reg_no] NVARCHAR(100) NULL,
    [admission_year] INT NULL,
    [student_name_mm] NVARCHAR(200) NULL,
    [student_name_en] NVARCHAR(200) NULL,
    [mother_name] NVARCHAR(200) NULL,
    [father_name] NVARCHAR(200) NULL,
    [gender_relation] NVARCHAR(50) NULL,
    [ethnicity] NVARCHAR(100) NULL,
    [religion] NVARCHAR(100) NULL,
    [pob] NVARCHAR(200) NULL,
    [birth_place_region] NVARCHAR(100) NULL,
    [student_nrc_no] NVARCHAR(100) NULL,
    [nationality_status] NVARCHAR(100) NULL,
    [dob] DATETIME NULL,
    [email] NVARCHAR(150) NULL,
    [blood_type] NVARCHAR(20) NULL,
    [covid_vaccine_status] NVARCHAR(100) NULL,
    [current_address] NVARCHAR(MAX) NULL,
    [permanent_address_mm] NVARCHAR(MAX) NULL,
    [permanent_address_en] NVARCHAR(MAX) NULL,
    [matric_roll_no] NVARCHAR(50) NULL,
    [matric_passed_year] INT NULL,
    [exam_center] NVARCHAR(150) NULL,
    [father_occupation] NVARCHAR(150) NULL,
    [mother_occupation] NVARCHAR(150) NULL,
    [past_exam_major] NVARCHAR(150) NULL,
    [past_exam_roll_no] NVARCHAR(50) NULL,
    [past_exam_year] INT NULL,
    [past_exam_status] NVARCHAR(50) NULL,
    [previous_year_roll_no] NVARCHAR(50) NULL,
    [guardian_name] NVARCHAR(200) NULL,
    [guardian_relationship] NVARCHAR(100) NULL,
    [guardian_occupation] NVARCHAR(150) NULL,
    [guardian_address_phone] NVARCHAR(MAX) NULL,
    [app_guardian_name] NVARCHAR(200) NULL,
    [app_guardian_nrc] NVARCHAR(100) NULL,
    [app_guardian_phone] NVARCHAR(50) NULL,
    [app_guardian_address] NVARCHAR(MAX) NULL,
    [app_student_name] NVARCHAR(200) NULL,
    [app_student_phone] NVARCHAR(50) NULL,
    [stipend_requested] BIT NULL DEFAULT 0,
    [created_by] NVARCHAR(100) NULL,
    [nrc_state] NVARCHAR(20) NULL,
    [nrc_township] NVARCHAR(50) NULL,
    [nrc_type] NVARCHAR(20) NULL,
    [nrc_number] NVARCHAR(50) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [ModifiedDateTime] DATETIME NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_StudentPersonalInfo] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- [StudentRegistrations]
CREATE TABLE [dbo].[StudentRegistrations] (
    [registration_id] INT IDENTITY(1,1) NOT NULL,
    [user_id] INT NULL,
    [NewStudentAccId] INT NULL,
    [student_name_mm] NVARCHAR(200) NULL,
    [student_name_en] NVARCHAR(200) NULL,
    [nrc_no] NVARCHAR(100) NULL,
    [dob] DATETIME NULL,
    [gender] NVARCHAR(20) NULL,
    [father_name] NVARCHAR(200) NULL,
    [phone] NVARCHAR(50) NULL,
    [email] NVARCHAR(150) NULL,
    [academic_year_range] NVARCHAR(50) NULL,
    [major] NVARCHAR(150) NULL,
    [roll_no] NVARCHAR(50) NULL,
    [semester_id] INT NULL,
    [status] NVARCHAR(50) NULL DEFAULT 'Pending',
    [RegistrationStep] INT NULL DEFAULT 1,
    [AcademicYearLevel] NVARCHAR(50) NULL,
    [AdmissionSerialNo] NVARCHAR(100) NULL,
    [application_date] DATETIME NULL DEFAULT GETDATE(),
    [created_datetime] DATETIME NULL DEFAULT GETDATE(),
    [IsDelete] BIT NULL DEFAULT 0,
    [StipendRequested] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_StudentRegistrations] PRIMARY KEY CLUSTERED ([registration_id] ASC),
    CONSTRAINT [FK_StudentRegistrations_User] FOREIGN KEY ([user_id]) REFERENCES [dbo].[Users] ([User_Id])
);

-- [RegistrationPayments]
CREATE TABLE [dbo].[RegistrationPayments] (
    [Payment_Id] INT IDENTITY(1,1) NOT NULL,
    [Registration_Id] INT NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [PaymentMethod] NVARCHAR(50) NULL,
    [SlipImage] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(50) NULL DEFAULT 'Pending',
    [VerifyBy] INT NULL,
    [VerifyDate] DATETIME NULL,
    [RejectReason] NVARCHAR(MAX) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_RegistrationPayments] PRIMARY KEY CLUSTERED ([Payment_Id] ASC),
    CONSTRAINT [FK_RegistrationPayments_Registration] FOREIGN KEY ([Registration_Id]) REFERENCES [dbo].[StudentRegistrations] ([registration_id]),
    CONSTRAINT [FK_RegistrationPayments_User] FOREIGN KEY ([VerifyBy]) REFERENCES [dbo].[Users] ([User_Id])
);

-- [PaymentFees]
CREATE TABLE [dbo].[PaymentFees] (
    [Fees_Id] INT IDENTITY(1,1) NOT NULL,
    [FeeType] NVARCHAR(150) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [AcademicYear] NVARCHAR(50) NULL,
    [Status] NVARCHAR(50) NULL DEFAULT 'Active',
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_PaymentFees] PRIMARY KEY CLUSTERED ([Fees_Id] ASC)
);

-- [Categories]
CREATE TABLE [dbo].[Categories] (
    [Category_Id] INT IDENTITY(1,1) NOT NULL,
    [Category_Name] NVARCHAR(150) NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Category_Id] ASC)
);

-- [Books]
CREATE TABLE [dbo].[Books] (
    [Book_Id] INT IDENTITY(1,1) NOT NULL,
    [BookTitle] NVARCHAR(250) NOT NULL,
    [Author] NVARCHAR(150) NULL,
    [Category_Id] INT NOT NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED ([Book_Id] ASC),
    CONSTRAINT [FK_Book_Category] FOREIGN KEY ([Category_Id]) REFERENCES [dbo].[Categories] ([Category_Id])
);

-- [Activity]
CREATE TABLE [dbo].[Activity] (
    [Activity_Id] INT IDENTITY(1,1) NOT NULL,
    [Activity_Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_Activity] PRIMARY KEY CLUSTERED ([Activity_Id] ASC)
);

-- [RulesRegulations]
CREATE TABLE [dbo].[RulesRegulations] (
    [Rule_Id] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedDateTime] DATETIME NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDelete] BIT NULL DEFAULT 0,
    CONSTRAINT [PK_RulesRegulations] PRIMARY KEY CLUSTERED ([Rule_Id] ASC)
);

-- [Permissions]
CREATE TABLE [dbo].[Permissions] (
    [PermissionId] INT IDENTITY(1,1) NOT NULL,
    [PermissionName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [CreatedDateTime] DATETIME NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC)
);

-- [RolePermissions]
CREATE TABLE [dbo].[RolePermissions] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [RoleId] INT NOT NULL,
    [PermissionId] INT NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Role_Id]),
    CONSTRAINT [FK_RolePermissions_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([PermissionId])
);

-- [RoleHierarchy]
CREATE TABLE [dbo].[RoleHierarchy] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ParentRoleId] INT NOT NULL,
    [ChildRoleId] INT NOT NULL,
    CONSTRAINT [PK_RoleHierarchy] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RoleHierarchy_ParentRole] FOREIGN KEY ([ParentRoleId]) REFERENCES [dbo].[Roles] ([Role_Id]),
    CONSTRAINT [FK_RoleHierarchy_ChildRole] FOREIGN KEY ([ChildRoleId]) REFERENCES [dbo].[Roles] ([Role_Id])
);
GO

-- ===============================================================================
-- 3. SEED INITIAL MASTER DATA
-- ===============================================================================

-- Seed Roles
SET IDENTITY_INSERT [dbo].[Roles] ON;
INSERT INTO [dbo].[Roles] ([Role_Id], [Role_Name], [Description]) VALUES 
(1, N'Super Admin', N'System Administrator'),
(2, N'Faculty Admin', N'Faculty Administrator'),
(3, N'Student', N'Student User Account'),
(4, N'Tutor', N'Teacher / Instructor');
SET IDENTITY_INSERT [dbo].[Roles] OFF;

-- Seed Faculties
SET IDENTITY_INSERT [dbo].[Faculty] ON;
INSERT INTO [dbo].[Faculty] ([Faculty_Id], [Faculty_Name]) VALUES 
(1, N'Faculty of Information Technology'),
(2, N'Faculty of Engineering'),
(3, N'Faculty of Science'),
(4, N'Faculty of Computer Systems and Technologies');
SET IDENTITY_INSERT [dbo].[Faculty] OFF;

-- Seed Majors
SET IDENTITY_INSERT [dbo].[Major] ON;
INSERT INTO [dbo].[Major] ([Major_Id], [Major_Name], [Faculty_Id]) VALUES 
(1, N'Computer Science', 1),
(2, N'Computer Technology', 1),
(3, N'Information Technology', 1),
(4, N'Civil Engineering', 2),
(5, N'Electronic Engineering', 2),
(6, N'Electrical Power Engineering', 2),
(7, N'Mechanical Engineering', 2),
(8, N'Information Technology Engineering', 2);
SET IDENTITY_INSERT [dbo].[Major] OFF;

-- Seed Semesters
SET IDENTITY_INSERT [dbo].[Semesters] ON;
INSERT INTO [dbo].[Semesters] ([Semester_Id], [Semester_Name]) VALUES 
(1, N'First Year First Semester'),
(2, N'First Year Second Semester'),
(3, N'Second Year First Semester'),
(4, N'Second Year Second Semester'),
(5, N'Third Year First Semester'),
(6, N'Third Year Second Semester'),
(7, N'Fourth Year First Semester'),
(8, N'Fourth Year Second Semester'),
(9, N'Fifth Year Final Semester');
SET IDENTITY_INSERT [dbo].[Semesters] OFF;

-- Seed Super Admin User (Password: Admin@123)
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([User_Id], [Username], [PasswordHash], [FullName], [Email], [Role_id], [Faculty_Id]) VALUES 
(1, N'admin', N'$2a$11$q9d/4X6U.vPzM7o7JtWn5.E.k3FzWj.kZ4XmQ8G.q/m.z7X.', N'System Administrator', N'admin@smartcampus.edu.mm', 1, 1);
SET IDENTITY_INSERT [dbo].[Users] OFF;

GO

PRINT N'Database creation and seeding completed successfully.';
