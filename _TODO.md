# Goal

We want to expose a programmatic interface to .NET Upgrade Planner

Current situation:

- We have both `apisofdotnet` (CLI) and `NetUpgradePlanner` (UI)
- There is no code sharing
- The CLI is outdated as the UI has an improved algorithm
- We should extract the analysis portion of API Catalog into a library (e.g. `Terrajobst.NetUpgradePlanner`)
- We should update the CLI to use this library
- We should publish both `Terrajobst.ApiCatalog` as well as `Terrajobst.NetUpgradePlanner` to NuGet
- We should provide code samples that shows how to use `apisofdotnet` as well as `Terrajobst.NetUpgradePlanner`
