# Third-party notice: GraphViz

`eng/graphviz` contains a redistribution of [GraphViz](https://graphviz.org/) **6.0.2**,
unmodified apart from the removal of import libraries (`.lib`, `.exp`), which are only used
when linking against GraphViz and never when running it.

NetUpgradePlanner invokes `dot.exe` at runtime to lay out dependency graphs, so these binaries
are copied into the application output and ship with it.

| | |
|---|---|
| Version | 6.0.2 |
| Source | `https://gitlab.com/api/v4/projects/4207231/packages/generic/graphviz-releases/6.0.2/windows_10_msbuild_Release_graphviz-6.0.2-win32.zip` |
| License | [Eclipse Public License 1.0](https://graphviz.org/license/) |
| Upstream | https://gitlab.com/graphviz/graphviz |

GraphViz is licensed under the Eclipse Public License, Version 1.0, which permits
redistribution. The full licence text is available at https://www.eclipse.org/legal/epl-v10.html
and at https://graphviz.org/license/.

## Why these binaries are committed

They used to be downloaded during the build. That outbound call to `gitlab.com` violates 1ES
Network Isolation (CFSClean2) and prevents `apis-of-dotnet-planner-build` from running on a
network-isolated pool.

Publishing them to an internal package feed was the obvious alternative, but Microsoft package
feed security policy permits only one internal feed in `NuGet.config`, and we do not have
publish rights on the existing one. Vendoring keeps the build hermetic without a second feed.

To move to a newer GraphViz release, run `eng/Update-GraphViz.ps1 -Version <x.y.z>` from a
developer machine and commit the result.
