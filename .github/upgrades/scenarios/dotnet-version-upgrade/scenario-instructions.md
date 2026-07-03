# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: .NET 10 (LTS)
- **Rationale**: .NET 10 provides LTS support through November 2028, ensuring long-term stability and security updates for the ReduceSvgFile project.

## Upgrade Options
**Source**: .github/upgrades/dotnet-version-upgrade/upgrade-options.md

### Strategy
- **Upgrade Strategy**: All-at-Once

## Strategy
**Selected**: All-At-Once
**Rationale**: Single project with no external package dependencies or complex APIs. Straightforward TFM bump from .NET 8 to .NET 10 with no compatibility issues.

### Execution Constraints
- Atomic operation: entire solution upgraded together
- Single project simplifies validation — no multi-project dependency ordering needed
- Target framework updated in one step, packages validated, full solution build required for validation
- No phasing, no multi-targeting overhead
