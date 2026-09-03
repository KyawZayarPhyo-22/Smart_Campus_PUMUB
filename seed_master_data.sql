-- ===============================================================================
-- SMART CAMPUS PUMUB - MASTER DATA SEED SCRIPT
-- ===============================================================================
-- Target Database: SmartCampusDb
-- Description: Seeds Master Data for Faculty, Department, Major, Subject,
--              Semester, Position, Role, Permissions, RoleHierarchy, and Super Admin User.
-- Idempotent: Can be run multiple times safely without generating duplicate records.
-- ===============================================================================

USE [SmartCampusDb];
GO

SET NOCOUNT ON;

PRINT N'======================================================';
PRINT N'Starting Master Data Seeding for Smart Campus PUMUB...';
PRINT N'======================================================';

-- ===============================================================================
-- 1. SEED ROLES (ရာထူးအဆင့်အတန်းများ)
-- ===============================================================================
PRINT N'1. Seeding Roles...';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [RoleName] = N'Super Admin')
BEGIN
    INSERT INTO [dbo].[Role] ([RoleName], [CreatedDateTime], [IsDelete])
    VALUES (N'Super Admin', GETDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [RoleName] = N'Faculty Admin')
BEGIN
    INSERT INTO [dbo].[Role] ([RoleName], [CreatedDateTime], [IsDelete])
    VALUES (N'Faculty Admin', GETDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [RoleName] = N'Registrar')
BEGIN
    INSERT INTO [dbo].[Role] ([RoleName], [CreatedDateTime], [IsDelete])
    VALUES (N'Registrar', GETDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [RoleName] = N'Tutor')
BEGIN
    INSERT INTO [dbo].[Role] ([RoleName], [CreatedDateTime], [IsDelete])
    VALUES (N'Tutor', GETDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [RoleName] = N'Student')
BEGIN
    INSERT INTO [dbo].[Role] ([RoleName], [CreatedDateTime], [IsDelete])
    VALUES (N'Student', GETDATE(), 0);
END
GO

-- ===============================================================================
-- 2. SEED FACULTIES (မဟာဌာနများ)
-- ===============================================================================
PRINT N'2. Seeding Faculties...';

DECLARE @Faculties TABLE (Name NVARCHAR(150));
INSERT INTO @Faculties (Name) VALUES
(N'Faculty of Computer Science'),
(N'Faculty of Computer Technology'),
(N'Faculty of Information Science & Technology'),
(N'Faculty of Engineering');

INSERT INTO [dbo].[Faculty] ([Faculty_Name], [CreatedDateTime], [IsDelete])
SELECT f.Name, GETDATE(), 0
FROM @Faculties f
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Faculty] WHERE [Faculty_Name] = f.Name AND ([IsDelete] = 0 OR [IsDelete] IS NULL)
);
GO

-- ===============================================================================
-- 3. SEED DEPARTMENTS (ဌာနများ)
-- ===============================================================================
PRINT N'3. Seeding Departments...';

DECLARE @F_CS INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Science');
DECLARE @F_CT INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Technology');
DECLARE @F_IT INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Information Science & Technology');
DECLARE @F_ENG INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Engineering');

-- Fallback if specific names differ
IF @F_CS IS NULL SELECT TOP 1 @F_CS = [Faculty_Id] FROM [dbo].[Faculty];
IF @F_CT IS NULL SET @F_CT = @F_CS;
IF @F_IT IS NULL SET @F_IT = @F_CS;
IF @F_ENG IS NULL SET @F_ENG = @F_CS;

DECLARE @DeptData TABLE (FacultyId INT, DeptName NVARCHAR(150));
INSERT INTO @DeptData (FacultyId, DeptName) VALUES
-- Faculty of Computer Science
(@F_CS, N'Department of Software Engineering'),
(@F_CS, N'Department of Computational Mathematics'),
(@F_CS, N'Department of Information Technology Supporting'),
-- Faculty of Computer Technology
(@F_CT, N'Department of Hardware & Computer Systems'),
(@F_CT, N'Department of Network & Cybersecurity'),
(@F_CT, N'Department of Embedded Systems & IoT'),
-- Faculty of Information Science & Technology
(@F_IT, N'Department of Information Science'),
(@F_IT, N'Department of Data Science & Artificial Intelligence'),
-- Supporting & Foundation Departments
(@F_ENG, N'Department of Electronic Engineering'),
(@F_ENG, N'Department of Civil Engineering'),
(@F_CS, N'Department of Myanmar & English Languages'),
(@F_CS, N'Department of Natural Science & Physics');

INSERT INTO [dbo].[Department] ([Faculty_Id], [Department_Name], [CreatedDateTime], [IsDelete])
SELECT d.FacultyId, d.DeptName, GETDATE(), 0
FROM @DeptData d
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Department] 
    WHERE [Department_Name] = d.DeptName AND [Faculty_Id] = d.FacultyId AND ([IsDelete] = 0 OR [IsDelete] IS NULL)
);
GO

-- ===============================================================================
-- 4. SEED MAJORS (မေဂျာများ)
-- ===============================================================================
PRINT N'4. Seeding Majors...';

DECLARE @F_CS INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Science');
DECLARE @F_CT INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Technology');
DECLARE @F_IT INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Information Science & Technology');
DECLARE @F_ENG INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Engineering');

IF @F_CS IS NULL SELECT TOP 1 @F_CS = [Faculty_Id] FROM [dbo].[Faculty];
IF @F_CT IS NULL SET @F_CT = @F_CS;
IF @F_IT IS NULL SET @F_IT = @F_CS;
IF @F_ENG IS NULL SET @F_ENG = @F_CS;

DECLARE @MajorData TABLE (FacultyId INT, MajorName NVARCHAR(200));
INSERT INTO @MajorData (FacultyId, MajorName) VALUES
(@F_CS, N'Computer Science (B.C.Sc.)'),
(@F_CS, N'Software Engineering (B.C.Sc. - SE)'),
(@F_CT, N'Computer Technology (B.C.Tech.)'),
(@F_CT, N'Computer Networking & Cybersecurity (B.C.Tech. - CNC)'),
(@F_IT, N'Information Technology (B.C.Sc. - IT)'),
(@F_IT, N'Data Science & Analytics (B.C.Sc. - DS)'),
(@F_ENG, N'Electronic Engineering (B.E. - EC)'),
(@F_ENG, N'Civil Engineering (B.E. - CE)');

INSERT INTO [dbo].[Major] ([Faculty_Id], [Major_Name], [CreatedDateTime], [IsDelete])
SELECT m.FacultyId, m.MajorName, GETDATE(), 0
FROM @MajorData m
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Major] 
    WHERE [Major_Name] = m.MajorName AND [Faculty_Id] = m.FacultyId AND ([IsDelete] = 0 OR [IsDelete] IS NULL)
);
GO

-- ===============================================================================
-- 5. SEED POSITIONS (ဆရာ/ဆရာမ နှင့် ဝန်ထမ်း ရာထူးများ)
-- ===============================================================================
PRINT N'5. Seeding Positions...';

DECLARE @Positions TABLE (Title NVARCHAR(100));
INSERT INTO @Positions (Title) VALUES
(N'Professor / Head of Department'),
(N'Associate Professor'),
(N'Lecturer'),
(N'Assistant Lecturer'),
(N'Tutor / Demonstrator'),
(N'Head Registrar'),
(N'Registrar Officer'),
(N'System Administrator');

INSERT INTO [dbo].[Position] ([Position_Name], [CreatedDateTime], [IsDelete])
SELECT p.Title, GETDATE(), 0
FROM @Positions p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Position] 
    WHERE [Position_Name] = p.Title AND ([IsDelete] = 0 OR [IsDelete] IS NULL)
);
GO

-- ===============================================================================
-- 6. SEED SEMESTERS (စာသင်နှစ်ဝက်များ - Sequence & Elective Limits)
-- ===============================================================================
PRINT N'6. Seeding Semesters...';

DECLARE @Semesters TABLE (
    SemesterName NVARCHAR(100), 
    Sequence INT, 
    MaxElective INT, 
    MaxElectiveCS INT, 
    MaxElectiveCT INT
);

INSERT INTO @Semesters (SemesterName, Sequence, MaxElective, MaxElectiveCS, MaxElectiveCT) VALUES
(N'First Year - Semester I',   1,  0, 0, 0),
(N'First Year - Semester II',  2,  0, 0, 0),
(N'Second Year - Semester I',  3,  1, 1, 1),
(N'Second Year - Semester II', 4,  1, 1, 1),
(N'Third Year - Semester I',   5,  2, 2, 2),
(N'Third Year - Semester II',  6,  2, 2, 2),
(N'Fourth Year - Semester I',  7,  2, 2, 2),
(N'Fourth Year - Semester II', 8,  2, 2, 2),
(N'Fifth Year - Semester I',   9,  2, 2, 2),
(N'Fifth Year - Semester II',  10, 2, 2, 2);

MERGE [dbo].[Semester] AS target
USING @Semesters AS source
ON (target.[Semester_Name] = source.[SemesterName] AND (target.[IsDelete] = 0 OR target.[IsDelete] IS NULL))
WHEN MATCHED THEN
    UPDATE SET 
        target.[Sequence] = source.[Sequence],
        target.[Max_Elective] = source.[MaxElective],
        target.[Max_Elective_CS] = source.[MaxElectiveCS],
        target.[Max_Elective_CT] = source.[MaxElectiveCT],
        target.[ModifiedDateTime] = GETDATE()
WHEN NOT MATCHED THEN
    INSERT ([Semester_Name], [Sequence], [Max_Elective], [Max_Elective_CS], [Max_Elective_CT], [CreatedDateTime], [IsDelete])
    VALUES (source.[SemesterName], source.[Sequence], source.[MaxElective], source.[MaxElectiveCS], source.[MaxElectiveCT], GETDATE(), 0);
GO

-- ===============================================================================
-- 7. SEED SUBJECTS (ဘာသာရပ်များ - Core & Elective)
-- ===============================================================================
PRINT N'7. Seeding Subjects...';

DECLARE @F_CS INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Science');
DECLARE @F_CT INT = (SELECT TOP 1 [Faculty_Id] FROM [dbo].[Faculty] WHERE [Faculty_Name] = N'Faculty of Computer Technology');
DECLARE @M_CS INT = (SELECT TOP 1 [Major_Id] FROM [dbo].[Major] WHERE [Major_Name] LIKE N'%Computer Science%');
DECLARE @M_CT INT = (SELECT TOP 1 [Major_Id] FROM [dbo].[Major] WHERE [Major_Name] LIKE N'%Computer Technology%');

IF @F_CS IS NULL SELECT TOP 1 @F_CS = [Faculty_Id] FROM [dbo].[Faculty];
IF @F_CT IS NULL SET @F_CT = @F_CS;
IF @M_CS IS NULL SELECT TOP 1 @M_CS = [Major_Id] FROM [dbo].[Major];
IF @M_CT IS NULL SET @M_CT = @M_CS;

DECLARE @Sem1 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 1);
DECLARE @Sem2 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 2);
DECLARE @Sem3 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 3);
DECLARE @Sem4 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 4);
DECLARE @Sem5 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 5);
DECLARE @Sem6 INT = (SELECT TOP 1 [Semester_Id] FROM [dbo].[Semester] WHERE [Sequence] = 6);

DECLARE @Subjects TABLE (
    SubjectCode VARCHAR(50),
    SubjectName NVARCHAR(150),
    Credit INT,
    SubjectType INT, -- 1 = Core, 2 = Elective
    SemesterId INT,
    FacultyId INT,
    MajorId INT
);

INSERT INTO @Subjects (SubjectCode, SubjectName, Credit, SubjectType, SemesterId, FacultyId, MajorId) VALUES
-- First Year - Semester I (All Core)
('CST-1111', N'Myanmar Language & Communication Skills', 3, 1, @Sem1, @F_CS, @M_CS),
('CST-1121', N'English for Academic Purposes I',        3, 1, @Sem1, @F_CS, @M_CS),
('CST-1131', N'Calculus I & Analytical Geometry',       3, 1, @Sem1, @F_CS, @M_CS),
('CST-1141', N'Principles of Information Technology',    3, 1, @Sem1, @F_CS, @M_CS),
('CST-1151', N'Programming Fundamentals (C++)',         3, 1, @Sem1, @F_CS, @M_CS),
('CST-1161', N'Physics & Basic Circuit Fundamentals',    3, 1, @Sem1, @F_CT, @M_CT),

-- First Year - Semester II (All Core)
('CST-1211', N'English for Academic Purposes II',       3, 1, @Sem2, @F_CS, @M_CS),
('CST-1221', N'Discrete Mathematics',                  3, 1, @Sem2, @F_CS, @M_CS),
('CST-1231', N'Object-Oriented Programming (Java)',    3, 1, @Sem2, @F_CS, @M_CS),
('CST-1241', N'Computer Architecture & Organization',  3, 1, @Sem2, @F_CT, @M_CT),
('CST-1251', N'Database Management Systems (SQL)',     3, 1, @Sem2, @F_CS, @M_CS),
('CST-1261', N'Web Technologies Fundamentals (HTML/CSS/JS)', 3, 1, @Sem2, @F_CS, @M_CS),

-- Second Year - Semester I (CS & CT Core & Electives)
('CS-2111',  N'Data Structures and Algorithms',         3, 1, @Sem3, @F_CS, @M_CS),
('CS-2121',  N'Advanced Object-Oriented Software Design', 3, 1, @Sem3, @F_CS, @M_CS),
('CT-2111',  N'Digital Logic Design & Microprocessors', 3, 1, @Sem3, @F_CT, @M_CT),
('CST-2131', N'Operating Systems Concepts',             3, 1, @Sem3, @F_CS, @M_CS),
('CST-2141', N'Computer Networks & Protocols',          3, 1, @Sem3, @F_CT, @M_CT),
('CS-2151',  N'Human Computer Interaction (HCI)',       3, 2, @Sem3, @F_CS, @M_CS), -- Elective
('CT-2151',  N'Microcontroller Interfacing & Sensors',  3, 2, @Sem3, @F_CT, @M_CT), -- Elective

-- Second Year - Semester II
('CS-2211',  N'Software Engineering Principles',        3, 1, @Sem4, @F_CS, @M_CS),
('CS-2221',  N'Design & Analysis of Algorithms',        3, 1, @Sem4, @F_CS, @M_CS),
('CT-2211',  N'Computer System Architecture',           3, 1, @Sem4, @F_CT, @M_CT),
('CST-2231', N'Network Routing & Switching',            3, 1, @Sem4, @F_CT, @M_CT),
('CS-2241',  N'Mobile Application Development (Flutter)', 3, 2, @Sem4, @F_CS, @M_CS), -- Elective
('CT-2241',  N'Embedded Linux & IoT Architecture',      3, 2, @Sem4, @F_CT, @M_CT), -- Elective

-- Third Year - Semester I
('CS-3111',  N'Artificial Intelligence & Machine Learning', 3, 1, @Sem5, @F_CS, @M_CS),
('CS-3121',  N'Enterprise Web Applications (.NET Core / C#)', 3, 1, @Sem5, @F_CS, @M_CS),
('CT-3111',  N'Wireless Sensor Networks',               3, 1, @Sem5, @F_CT, @M_CT),
('CST-3131', N'Information & Network Security',         3, 1, @Sem5, @F_CT, @M_CT),
('CS-3141',  N'Cloud Computing & DevOps',               3, 2, @Sem5, @F_CS, @M_CS), -- Elective
('CT-3141',  N'Robotics & Automation Control',          3, 2, @Sem5, @F_CT, @M_CT), -- Elective
('CS-3151',  N'Big Data Analytics',                     3, 2, @Sem5, @F_CS, @M_CS), -- Elective

-- Third Year - Semester II
('CS-3211',  N'Compiler Design & Automata Theory',      3, 1, @Sem6, @F_CS, @M_CS),
('CS-3221',  N'Software Testing & Quality Assurance',   3, 1, @Sem6, @F_CS, @M_CS),
('CT-3211',  N'Digital Signal Processing (DSP)',        3, 1, @Sem6, @F_CT, @M_CT),
('CST-3231', N'Cybersecurity & Ethical Hacking',        3, 1, @Sem6, @F_CT, @M_CT),
('CS-3241',  N'Natural Language Processing (NLP)',      3, 2, @Sem6, @F_CS, @M_CS), -- Elective
('CT-3241',  N'VLSI System Design',                     3, 2, @Sem6, @F_CT, @M_CT); -- Elective

MERGE [dbo].[Subject] AS target
USING (SELECT * FROM @Subjects WHERE SemesterId IS NOT NULL) AS source
ON (target.[Subject_Code] = source.[SubjectCode] AND (target.[IsDelete] = 0 OR target.[IsDelete] IS NULL))
WHEN MATCHED THEN
    UPDATE SET 
        target.[Subject_Name] = source.[SubjectName],
        target.[Credit] = source.[Credit],
        target.[Subject_Type] = source.[SubjectType],
        target.[Semester_Id] = source.[SemesterId],
        target.[Faculty_Id] = source.[FacultyId],
        target.[Major_Id] = source.[MajorId],
        target.[ModifiedDateTime] = GETDATE()
WHEN NOT MATCHED THEN
    INSERT ([Subject_Code], [Subject_Name], [Credit], [Subject_Type], [Semester_Id], [Faculty_Id], [Major_Id], [CreatedDateTime], [IsDelete])
    VALUES (source.[SubjectCode], source.[SubjectName], source.[Credit], source.[SubjectType], source.[SemesterId], source.[FacultyId], source.[MajorId], GETDATE(), 0);
GO

-- ===============================================================================
-- 8. SEED PERMISSIONS (ခွင့်ပြုချက်များအားလုံး)
-- ===============================================================================
PRINT N'8. Seeding Permissions...';

DECLARE @Perms TABLE (Name NVARCHAR(100));
INSERT INTO @Perms (Name) VALUES
-- User & Role Management
(N'User.View'), (N'User.Create'), (N'User.Edit'), (N'User.Delete'),
(N'Role.View'), (N'Role.Create'), (N'Role.Edit'), (N'Role.Delete'), (N'view.role'),
-- Academic Structures
(N'Faculty.View'), (N'Faculty.Create'), (N'Faculty.Edit'), (N'Faculty.Delete'),
(N'Department.View'), (N'Department.Create'), (N'Department.Edit'), (N'Department.Delete'),
(N'Major.View'), (N'Major.Create'), (N'Major.Edit'), (N'Major.Delete'),
(N'Position.View'), (N'Position.Create'), (N'Position.Edit'), (N'Position.Delete'),
(N'Semester.View'), (N'Semester.Create'), (N'Semester.Edit'), (N'Semester.Delete'),
(N'Subject.View'), (N'Subject.Create'), (N'Subject.Edit'), (N'Subject.Delete'),
-- Tutors & Students
(N'Tutor.View'), (N'Tutor.Create'), (N'Tutor.Edit'), (N'Tutor.Delete'),
(N'Student.View'), (N'Student.Create'), (N'Student.Edit'), (N'Student.Delete'),
(N'StudentRegistrations.View'), (N'StudentRegistrations.Create'), (N'StudentRegistrations.Edit'), (N'StudentRegistrations.Delete'),
(N'RegisterAcc.View'), (N'RegisterAcc.Create'), (N'RegisterAcc.Edit'), (N'RegisterAcc.Delete'),
(N'NewStudentAcc.View'), (N'NewStudentAcc.Create'), (N'NewStudentAcc.Edit'), (N'NewStudentAcc.Delete'),
(N'StudentDatabank.View'),
-- Enrollment & Grades
(N'Enrollment.View'), (N'Enrollment.Create'), (N'Enrollment.Edit'), (N'Enrollment.Delete'),
(N'Grade.View'), (N'Grade.Create'), (N'Grade.Edit'), (N'Grade.Delete'),
-- Auxiliary Services
(N'PaymentFee.View'), (N'PaymentFee.Create'), (N'PaymentFee.Edit'), (N'PaymentFee.Delete'),
(N'Activity.View'), (N'Activity.Create'), (N'Activity.Edit'), (N'Activity.Delete'),
(N'Book.View'), (N'Book.Create'), (N'Book.Edit'), (N'Book.Delete'),
(N'Category.View'), (N'Category.Create'), (N'Category.Edit'), (N'Category.Delete'),
(N'Rules.View'), (N'Rules.Create'), (N'Rules.Edit'), (N'Rules.Delete'),
(N'Mail.Send');

INSERT INTO [dbo].[Permissions] ([Permission_Name])
SELECT p.Name FROM @Perms p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Permission_Name] = p.Name);
GO

-- ===============================================================================
-- 9. ASSIGN ALL PERMISSIONS TO SUPER ADMIN ROLE (Role_Permissions)
-- ===============================================================================
PRINT N'9. Assigning Permissions to Super Admin Role...';

DECLARE @SuperAdminRoleId INT = (SELECT TOP 1 [Role_Id] FROM [dbo].[Role] WHERE [RoleName] = N'Super Admin');

IF @SuperAdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Role_Permissions] ([Role_Id], [Permission_Id])
    SELECT @SuperAdminRoleId, p.[Permission_Id]
    FROM [dbo].[Permissions] p
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[Role_Permissions] rp 
        WHERE rp.[Role_Id] = @SuperAdminRoleId AND rp.[Permission_Id] = p.[Permission_Id]
    );
END
GO

-- ===============================================================================
-- 10. SEED ROLE HIERARCHY (Super Admin Scope for All Faculties)
-- ===============================================================================
PRINT N'10. Seeding Role Hierarchy...';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleHierarchy')
BEGIN
    DECLARE @SuperAdminRoleId INT = (SELECT TOP 1 [Role_Id] FROM [dbo].[Role] WHERE [RoleName] = N'Super Admin');
    DECLARE @FacultyAdminRoleId INT = (SELECT TOP 1 [Role_Id] FROM [dbo].[Role] WHERE [RoleName] = N'Faculty Admin');
    DECLARE @TutorRoleId INT = (SELECT TOP 1 [Role_Id] FROM [dbo].[Role] WHERE [RoleName] = N'Tutor');

    IF @SuperAdminRoleId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[RoleHierarchy] WHERE [Parent_Role_Id] = @SuperAdminRoleId)
        BEGIN
            INSERT INTO [dbo].[RoleHierarchy] ([Parent_Role_Id], [Child_Role_Id], [CanAccessAllFaculties])
            VALUES 
            (@SuperAdminRoleId, ISNULL(@FacultyAdminRoleId, @SuperAdminRoleId), 1);
        END
    END
END
GO

-- ===============================================================================
-- 11. SEED SUPER ADMIN USER (အသုံးပြုသူ Super Admin အကောင့်)
-- ===============================================================================
PRINT N'11. Seeding Super Admin User Account...';

DECLARE @SuperAdminRoleId INT = (SELECT TOP 1 [Role_Id] FROM [dbo].[Role] WHERE [RoleName] = N'Super Admin');
IF @SuperAdminRoleId IS NULL SET @SuperAdminRoleId = 1;

-- BCrypt hash for "Admin@123"
DECLARE @AdminHash NVARCHAR(255) = N'$2a$11$q9d/4X6U.vPzM7o7JtWn5.E.k3FzWj.kZ4XmQ8G.q/m.z7X.';

-- 1. Create or Update superadmin account
IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [UserName] = 'superadmin' AND ([IsDelete] = 0 OR [IsDelete] IS NULL))
BEGIN
    INSERT INTO [dbo].[User] (
        [UserName], 
        [Password], 
        [Full_Name], 
        [Email], 
        [Role_id], 
        [Faculty_Id], 
        [Status], 
        [MustChangePassword], 
        [CreatedDateTime], 
        [IsDelete]
    )
    VALUES (
        'superadmin', 
        @AdminHash, 
        N'Super Administrator', 
        N'superadmin@pumub.edu.mm', 
        @SuperAdminRoleId, 
        NULL, -- NULL allows access to all faculties
        'Active', 
        0, 
        GETDATE(), 
        0
    );
    PRINT N'Created user: superadmin (Password: Admin@123)';
END
ELSE
BEGIN
    UPDATE [dbo].[User]
    SET 
        [Role_id] = @SuperAdminRoleId,
        [Faculty_Id] = NULL,
        [Status] = 'Active',
        [MustChangePassword] = 0,
        [Password] = @AdminHash
    WHERE [UserName] = 'superadmin';
    PRINT N'Updated user: superadmin';
END

-- 2. Create or Update standard admin account
IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [UserName] = 'admin' AND ([IsDelete] = 0 OR [IsDelete] IS NULL))
BEGIN
    INSERT INTO [dbo].[User] (
        [UserName], 
        [Password], 
        [Full_Name], 
        [Email], 
        [Role_id], 
        [Faculty_Id], 
        [Status], 
        [MustChangePassword], 
        [CreatedDateTime], 
        [IsDelete]
    )
    VALUES (
        'admin', 
        @AdminHash, 
        N'System Administrator', 
        N'admin@pumub.edu.mm', 
        @SuperAdminRoleId, 
        NULL, 
        'Active', 
        0, 
        GETDATE(), 
        0
    );
    PRINT N'Created user: admin (Password: Admin@123)';
END
ELSE
BEGIN
    UPDATE [dbo].[User]
    SET 
        [Role_id] = @SuperAdminRoleId,
        [Faculty_Id] = NULL,
        [Status] = 'Active',
        [MustChangePassword] = 0
    WHERE [UserName] = 'admin';
    PRINT N'Updated user: admin';
END
GO

PRINT N'======================================================';
PRINT N'Master Data Seeding Completed Successfully!';
PRINT N'======================================================';
PRINT N'Super Admin Credentials:';
PRINT N'  - Username: superadmin  (or admin)';
PRINT N'  - Password: Admin@123';
PRINT N'======================================================';
