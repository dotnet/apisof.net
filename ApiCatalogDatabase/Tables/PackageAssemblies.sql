CREATE TABLE [dbo].[PackageAssemblies]
(
	[PackageVersionId] INT NOT NULL,
	[FrameworkId] INT NOT NULL,
	[AssemblyId] INT NOT NULL,
)

GO

CREATE INDEX [IX_PackageAssemblies_AssemblyId] ON [dbo].[PackageAssemblies] ([AssemblyId])
