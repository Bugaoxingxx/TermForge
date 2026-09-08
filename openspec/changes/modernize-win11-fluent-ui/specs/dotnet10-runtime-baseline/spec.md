## ADDED Requirements

### Requirement: Solution targets .NET 10 LTS
All TermForge projects SHALL target `net10.0` or `net10.0-windows` as appropriate, and `global.json` SHALL pin a .NET 10 SDK with `rollForward` no looser than `latestFeature`.

#### Scenario: Solution restores and builds on .NET 10 SDK
- **WHEN** a machine with a .NET 10 SDK restores and builds the solution
- **THEN** every project compiles against `net10.0` or `net10.0-windows` and no project remains on `net8.0` or `net8.0-windows`

#### Scenario: SDK pin rejects .NET 8-only toolchains
- **WHEN** `dotnet --version` is resolved through the repository `global.json`
- **THEN** the selected SDK is a 10.0.x release and is not locked to 8.0.x

### Requirement: Framework upgrade does not change product behavior
The first implementation slice SHALL upgrade the runtime baseline without changing window chrome, theme mode, commands, session lifecycle, or terminal rendering behavior.

#### Scenario: Theme mode stays None after the framework-only upgrade
- **WHEN** the application starts after only TFM and `global.json` have changed
- **THEN** `ThemeMode` remains `None` (or equivalent Aero2 baseline) and the visible Aero shell is unchanged

#### Scenario: Existing automated tests still pass
- **WHEN** the unit test project and the UI automation project run after the framework-only upgrade
- **THEN** the previously passing unit and UI suites complete without new failures attributable to the TFM change

### Requirement: Product docs state the runtime baseline
README, architecture, and Phase 1 PRD SHALL describe the decided .NET 10 baseline, and SHALL distinguish Phase 1 historical .NET 8 scope from the new baseline.

#### Scenario: A new contributor reads README
- **WHEN** they open `README.md`
- **THEN** the documented stack and SDK prerequisite name .NET 10 LTS as the product baseline once the upgrade slice has landed, and they are not told the project is permanently on .NET 8

#### Scenario: Phase 1 PRD keeps historical scope
- **WHEN** a reader opens `docs/PRD-Phase1-ConPTY.md`
- **THEN** the original Phase 1 sentence about reusing .NET 8 remains as historical scope, and a later-baseline note points to this change and .NET 10
