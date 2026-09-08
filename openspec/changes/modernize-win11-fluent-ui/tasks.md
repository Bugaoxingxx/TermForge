## 1. Product documentation (direction now, SDK wording after TFM lands)

- [x] 1.1 Update `README.md` so the visual-design link and stack description name the Fluent / .NET 10 direction, and say the tree is still `net8.0` until the upgrade slice lands.
- [x] 1.2 Add a later-baseline note to `docs/PRD-Phase1-ConPTY.md` without rewriting Phase 1 FR/AC or the historical ".NET 8 this round" sentence.
- [x] 1.3 Point `docs/ARCHITECTURE.md` at the Fluent migration design and the new runtime baseline.
- [x] 1.4 Note in `docs/design/LOGO_DESIGN_SYSTEM.md` that the product chrome is moving to Fluent while the mark may keep Aero heritage.
- [x] 1.5 After the TFM slice merges, change README / `global.json` commentary from "still .NET 8" to "requires .NET 10 SDK".
- [x] 1.6 Archive `openspec/changes/refine-win7-aero-ui` once this change is the active UI baseline.

## 2. .NET 10 framework upgrade (visual no-op)

- [x] 2.1 Install or confirm a .NET 10 SDK on the build machine.
- [x] 2.2 Pin `global.json` to a 10.0.x SDK with `rollForward: latestFeature`.
- [x] 2.3 Change all seven project `TargetFramework` values from `net8.0` / `net8.0-windows` to `net10.0` / `net10.0-windows`.
- [x] 2.4 Restore, build, and run the unit suite; fix only framework-breakage, not visuals.
- [x] 2.5 Run the existing UI automation suite with `ThemeMode` still `None` and confirm no intentional chrome change.

## 3. Fluent shell and backdrop

- [x] 3.1 Enable `ThemeMode=System` (suppress `WPF0001` if set from code) and verify light/dark follow Windows.
- [x] 3.2 Replace the opaque Aero `MainWindowChrome` caption with Fluent or system chrome plus a system backdrop on Windows 11 22621+.
- [x] 3.3 Preserve drag, double-click, system menu, resize, Snap, and taskbar-safe maximize; add maximize margin compensation if the caption clips.
- [x] 3.4 Detect OS version and fall back to Acrylic or a solid color when Mica is unavailable; never leave a black frame.
- [x] 3.5 Keep the terminal viewport opaque and free of backdrop, blur, and rounded clipping.

## 4. Tokens, icons, and high contrast

- [x] 4.1 Rebuild `Colors.xaml` as light/dark semantic tokens bound to accent / `SystemColors`.
- [x] 4.2 Replace toolbar and caption glyphs with Segoe Fluent Icons where the shell is user-visible.
- [x] 4.3 Audit `Controls.xaml` against Fluent defaults and remove Aero-only overrides that cause mixed chrome.
- [x] 4.4 Keep `AeroDockingTheme.xaml` as the MDI brand skin; do not put Fluent materials on terminal cells.
- [x] 4.5 Extend `HighContrast.xaml` so glow/shadow are off and success/danger are not color-only.

## 5. Tests and validation

- [x] 5.1 Make `AutomationId` unique for navigation, diagnostics, and properties panes (see review `6fe0b84`).
- [x] 5.2 Update FlaUI page objects for the new caption and pane tree.
- [x] 5.3 Extend theme smoke tests for Fluent keys and light / dark / high-contrast.
- [x] 5.4 Record Win11 22621+, Win10 fallback, and high-contrast screenshots in a Fluent validation note.
