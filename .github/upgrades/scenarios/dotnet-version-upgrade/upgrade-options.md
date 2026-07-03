# Upgrade Options — ReduceSvgFile

Assessment: 1 project (ReduceSvgFile), .NET 8 → .NET 10 (modern-to-modern), SDK-style, no incompatible packages, minimal complexity

## Strategy

### Upgrade Strategy
Single project with no external dependencies. Straightforward modern .NET version bump.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all projects simultaneously in a single atomic pass. Fastest approach, no multi-targeting overhead. |
| Top-Down | Upgrade applications first with multi-targeting libraries (not applicable here — no library/app separation). |
