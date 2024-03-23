# API Catalog

## Indexing

The general design of the API catalog is that it's indexed once every day, from
scratch. Since re-indexing isn't incremental it's easy to absorb schema changes.

The general process works as follows:

1. Some frameworks are snapshotted as zip files and are downloaded from blob
   storage. That is true for .NET Framework and .NET Standard.

2. .NET Core is generally indexed by indexing target framework pack files, which
   are themselves NuGet packages. Indexing resolves the newest version of those
   packages.

3. The output of the indexing is a bunch of XML files that are then translated into
   a [binary catalog][schema].

4. The binary catalog is uploaded to blob storage and can be accessed publicly.

5. The website serves out of the [binary blob][schema].

[schema]: schema.md