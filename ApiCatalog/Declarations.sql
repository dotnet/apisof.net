CREATE TABLE [dbo].[Declarations]
(
	[DeclarationId] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
	[ApiId] INT NOT NULL,
	[AssemblyId] INT NOT NULL,
	[Syntax] NVARCHAR(MAX)
)
