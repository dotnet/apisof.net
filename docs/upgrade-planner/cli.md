# .NET Upgrade Planner CLI

## Installation

You can get it via a .NET Global Tool:

```text
dotnet tool install -g apisofdotnet --prerelease
```

To update, you can run this:

```text
dotnet tool update apisofdotnet -g --prerelease
```

If the app has trouble reading the catalog, it might be because you have an old
version of the catalog that is no longer compatible. You can force a catalog
update by running this command:

```text
apisofdotnet update-catalog --force
```

## Usage

To check the API usages of an application, you can use this command:

```text
apisofdotnet check-apis D:\app\path -t net8.0-windows -o P:\results.csv
```
