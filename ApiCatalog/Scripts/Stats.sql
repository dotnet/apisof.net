SELECT
	[Apis]                = (SELECT COUNT(*) FROM [Apis]),
	[Declarations]        = (SELECT COUNT(*) FROM [Declarations]),
	[Assemblies]          = (SELECT COUNT(*) FROM [Assemblies]),
	[Packages]            = (SELECT COUNT(*) FROM [Packages]),
	[PackageVersions]     = (SELECT COUNT(*) FROM [PackageVersions]),
	[PackageAssemblies]   = (SELECT COUNT(*) FROM [PackageAssemblies]),
	[Frameworks]          = (SELECT COUNT(*) FROM [Frameworks]),
	[PlatformAssemblies]  = (SELECT COUNT(*) FROM [PlatformAssemblies])