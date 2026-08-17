-- 1. Add new columns to the existing Student table
ALTER TABLE [dbo].[Student]
ADD 
    [Student_Name] [nvarchar](150) NULL,
    [Enrollment_No] [varchar](50) NULL,
    [Email] [nvarchar](150) NULL,
    [Phone] [varchar](20) NULL,
    [Faculty_Id] [int] NULL;
GO

-- 2. Add foreign key for Faculty_Id in Student table
ALTER TABLE [dbo].[Student]
ADD CONSTRAINT [FK_Student_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty]([Faculty_Id]);
GO

-- 3. Create Subject_Prerequisite table
CREATE TABLE [dbo].[Subject_Prerequisite](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Subject_Id] [int] NOT NULL,               
    [Prerequisite_Subject_Id] [int] NOT NULL,  
    [CreatedDateTime] [datetime] NULL DEFAULT (getdate()),
    [CreatedBy] [nvarchar](100) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SubjPrereq_Subject FOREIGN KEY (Subject_Id) REFERENCES [dbo].[Subject]([Subject_Id]),
    CONSTRAINT FK_SubjPrereq_Prereq  FOREIGN KEY (Prerequisite_Subject_Id) REFERENCES [dbo].[Subject]([Subject_Id]),
    CONSTRAINT UQ_Subject_Prereq UNIQUE (Subject_Id, Prerequisite_Subject_Id),
    CONSTRAINT CK_No_Self_Prereq CHECK (Subject_Id <> Prerequisite_Subject_Id)
);
GO

-- 4. Create Student_Subject_Enrollment table
CREATE TABLE [dbo].[Student_Subject_Enrollment](
    [Enrollment_Id] [int] IDENTITY(1,1) NOT NULL,
    [Student_Id] [int] NOT NULL,
    [Subject_Id] [int] NOT NULL,
    [Semester_Id] [int] NOT NULL,        
    [Enrollment_Date] [datetime] NOT NULL DEFAULT (getdate()),
    [Status] [tinyint] NOT NULL DEFAULT 1,  
    [CreatedDateTime] [datetime] NULL DEFAULT (getdate()),
    [CreatedBy] [nvarchar](100) NULL,
    [ModifiedDateTime] [datetime] NULL,
    [ModifiedBy] [nvarchar](100) NULL,
    [IsDelete] [bit] NULL DEFAULT 0,
    PRIMARY KEY CLUSTERED ([Enrollment_Id] ASC),
    CONSTRAINT FK_Enroll_Student FOREIGN KEY (Student_Id) REFERENCES [dbo].[Student]([Student_Id]),
    CONSTRAINT FK_Enroll_Subject FOREIGN KEY (Subject_Id) REFERENCES [dbo].[Subject]([Subject_Id]),
    CONSTRAINT FK_Enroll_Semester FOREIGN KEY (Semester_Id) REFERENCES [dbo].[Semester]([Semester_Id]),
    CONSTRAINT UQ_Student_Subject_Semester UNIQUE (Student_Id, Subject_Id, Semester_Id)
);
GO

-- 5. Create Student_Subject_Result table
CREATE TABLE [dbo].[Student_Subject_Result](
    [Result_Id] [int] IDENTITY(1,1) NOT NULL,
    [Enrollment_Id] [int] NOT NULL,
    [Student_Id] [int] NOT NULL,
    [Subject_Id] [int] NOT NULL,
    [Semester_Id] [int] NOT NULL,
    [Marks_Obtained] [decimal](5,2) NULL,
    [Max_Marks] [decimal](5,2) NULL,
    [Grade] [varchar](5) NULL,
    [Is_Pass] [bit] NOT NULL DEFAULT 0,
    [Result_Date] [datetime] NULL,
    [CreatedDateTime] [datetime] NULL DEFAULT (getdate()),
    [CreatedBy] [nvarchar](100) NULL,
    [ModifiedDateTime] [datetime] NULL,
    [ModifiedBy] [nvarchar](100) NULL,
    PRIMARY KEY CLUSTERED ([Result_Id] ASC),
    CONSTRAINT FK_Result_Enrollment FOREIGN KEY (Enrollment_Id) REFERENCES [dbo].[Student_Subject_Enrollment]([Enrollment_Id]),
    CONSTRAINT FK_Result_Student FOREIGN KEY (Student_Id) REFERENCES [dbo].[Student]([Student_Id]),
    CONSTRAINT FK_Result_Subject FOREIGN KEY (Subject_Id) REFERENCES [dbo].[Subject]([Subject_Id]),
    CONSTRAINT FK_Result_Semester FOREIGN KEY (Semester_Id) REFERENCES [dbo].[Semester]([Semester_Id]),
    CONSTRAINT UQ_Result_Enrollment UNIQUE (Enrollment_Id)
);
GO
