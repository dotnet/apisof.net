CREATE TABLE [dbo].[PackageVersions]
(
	[PackageVersionId] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
	[PackageId] INT NOT NULL,
	[Version] NVARCHAR(255) NOT NULL
)
