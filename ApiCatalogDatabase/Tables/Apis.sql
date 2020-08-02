CREATE TABLE [dbo].[Apis]
(
	[ApiId] INT IDENTITY (1,1) PRIMARY KEY,
	[Kind] INT NOT NULL,
	[ApiGuid] UNIQUEIDENTIFIER NOT NULL,
	[ParentApiId] INT,
	[Name] NVARCHAR(255) NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_Apis_ApiGuid] ON [dbo].[Apis] ([ApiGuid])

GO

CREATE INDEX [IX_Apis_ParentApiId] ON [dbo].[Apis] ([ParentApiId])
