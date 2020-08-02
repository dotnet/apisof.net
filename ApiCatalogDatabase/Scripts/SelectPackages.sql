SELECT	p.Name,
		pv.Version
FROM	Packages p
			JOIN PackageVersions pv on pv.PackageId = p.PackageId
ORDER	BY p.Name, pv.Version