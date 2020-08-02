CREATE TABLE [dbo].[Declarations]
(
	[ApiId] INT NOT NULL,
	[AssemblyId] INT NOT NULL,
	[Syntax] NVARCHAR(MAX)
)

GO

CREATE INDEX [IX_Declarations_ApiId] ON [dbo].[Declarations] ([ApiId])
