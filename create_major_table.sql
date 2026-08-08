IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Major')
BEGIN
    CREATE TABLE [dbo].[Major] (
        [Major_Id]          INT            IDENTITY (1, 1) NOT NULL,
        [Major_Name]        NVARCHAR (200) NOT NULL,
        [Faculty_Id]        INT            NOT NULL,
        [CreatedDateTime]   DATETIME       DEFAULT (GETDATE()) NULL,
        [CreatedBy]         NVARCHAR (MAX) NULL,
        [ModifiedDateTime]  DATETIME       NULL,
        [ModifiedBy]        NVARCHAR (MAX) NULL,
        [IsDelete]          BIT            DEFAULT ((0)) NULL,
        CONSTRAINT [PK_Major] PRIMARY KEY CLUSTERED ([Major_Id] ASC),
        CONSTRAINT [FK_Major_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id])
    );
END
GO
