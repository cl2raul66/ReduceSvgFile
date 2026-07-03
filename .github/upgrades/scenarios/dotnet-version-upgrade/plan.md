# .NET 10 Upgrade Plan — ReduceSvgFile

## Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: Single project (ReduceSvgFile.csproj), already on .NET 8 (modern framework), SDK-style, no incompatible packages. Straightforward TFM bump from net8.0 to net10.0.

---

## Tasks

### 01-upgrade-to-dotnet-10: Upgrade ReduceSvgFile to .NET 10

The single project in the solution is already on .NET 8 using SDK-style csproj format and has no external NuGet dependencies. This task updates the target framework from `net8.0` to `net10.0`, verifies compatibility, and ensures all code continues to work without breaking changes.

**Scope**: ReduceSvgFile.csproj (console/utility application)

**Context**: 
- 923 lines of code across 2 files
- No incompatible packages
- No API breaking changes detected
- No complex dependencies or external integrations

**Known risks**: None identified

**Research starting points**:
- Verify the project builds and runs successfully after TFM change
- Check for any behavioral changes in the .NET 10 runtime
- Run existing tests/validation (if any)

**Done when**:
- [ ] ReduceSvgFile.csproj target framework changed to `net10.0`
- [ ] Solution builds without errors or warnings
- [ ] Application runs as expected
- [ ] All functionality is preserved

### 02-final-validation: Validate upgraded solution

Verify the complete upgrade is successful with a full solution build and any integration tests.

**Scope**: Entire solution

**Done when**:
- [ ] Solution builds cleanly (no errors, no warnings)
- [ ] All tests pass (if any test projects exist)
- [ ] Application is ready for deployment
