CREATE TABLE [dbo].[PlatformAssemblies]
(
	[DeclarationId] INT NOT NULL,
	[AssemblyId] INT NOT NULL,
)

GO

CREATE INDEX [IX_PlatformAssemblies_AssemblyId] ON [dbo].[PlatformAssemblies] ([AssemblyId])
