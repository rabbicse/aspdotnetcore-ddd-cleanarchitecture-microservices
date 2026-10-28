# ADR 0001 — Target .NET 10 and manage packages centrally

**Status:** accepted

**Context.** The service targeted .NET 8, with the framework and package versions repeated in seven project files.
.NET 10 is the current long-term-support release, and drift between projects (two EF Core versions, say) is a common
source of runtime errors.

**Decision.** Target `net10.0` in `Directory.Build.props`, pin the SDK in `global.json`, and keep every version in
`Directory.Packages.props`. Projects reference packages without versions.

**Consequences.** One place to upgrade and to see what the service depends on; Dependabot can raise a single PR per
package. A project cannot silently use a different version. Cost: contributors must edit the props file, not the csproj.
