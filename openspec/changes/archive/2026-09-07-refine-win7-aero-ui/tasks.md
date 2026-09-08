## 1. Baseline and Regression Safety

- [x] 1.1 Capture current main-window and three-document MDI screenshots and record the tested Windows version, DPI, resolution, and active-window state.
- [x] 1.2 Add or extend UI automation coverage for main-window minimize, maximize/restore, close, title-bar double-click, drag, resize borders, and retained AutomationId values before replacing the window chrome.
- [x] 1.3 Add coverage for `Alt+Space`, title-bar system menu access, maximized working-area bounds, and the existing Ctrl+N/Ctrl+Tab/Ctrl+F4 navigation behavior.
- [x] 1.4 Inventory hard-coded shell/MDI colors, sizes, local effects, Unicode glyph icons, and duplicate resource definitions, and map each one to its target semantic theme resource.

## 2. Aero Theme Foundation

- [x] 2.1 Explicitly merge the WPF `PresentationFramework.Aero` theme before TermForge resources and verify that the application starts without missing-resource or theme-assembly errors.
- [x] 2.2 Split the application theme into ordered color, typography/metric, standard-control, icon, and main-window-chrome resource dictionaries while preserving existing resource keys during migration.
- [x] 2.3 Define shared Segoe UI/Microsoft YaHei UI font resources, 12-DIP normal text, the auxiliary-text minimum, 23–24-DIP command height, and 5/7/11-DIP spacing tokens.
- [x] 2.4 Add semantic resources and templates for normal, hover, pressed, keyboard-focus, disabled, checked, inactive-selection, and danger-command states.
- [x] 2.5 Enable layout rounding and device-pixel snapping at the shell boundary and add a startup smoke test that resolves every critical theme resource.

## 3. Main Window Chrome

- [x] 3.1 Implement the non-transparent `WindowChrome` configuration and Win7 Aero-style main-window template with caption, frame, application icon, title, and caption command regions.
- [x] 3.2 Implement pixel-aligned minimize, maximize/restore, and close glyphs and bind them to standard window commands with complete pointer, keyboard, disabled, and window-state visuals.
- [x] 3.3 Preserve title-bar drag, double-click maximize/restore, right-click and `Alt+Space` system menu behavior, resize borders, Windows Snap, and taskbar-safe maximized bounds.
- [x] 3.4 Add a deterministic XAML fallback that remains visible and operable when legacy DWM Aero glass is unavailable.
- [x] 3.5 Run the main-window behavior automation suite and correct any hit-test, DPI, clipping, or state regressions before proceeding to shell restyling.

## 4. MMC Shell and Iconography

- [x] 4.1 Restyle the menu bar and menu items against the shared Aero baseline while retaining access keys, shortcut text, command bindings, and keyboard navigation.
- [x] 4.2 Reduce the toolbar to high-frequency commands, combine layer/tile variants behind one accessible arrangement entry, and apply subtle Win7 normal/hover/pressed/focus treatments.
- [x] 4.3 Replace navigation, toolbar, status, and caption Emoji/Unicode symbols with shared pixel-aligned `DrawingImage`, `Path`, or 16-DIP-aware transparent image resources.
- [x] 4.4 Produce and wire a multi-size ICO containing 16, 24, 32, 48, and 256px application images derived from the approved TermForge mark.
- [x] 4.5 Restyle the navigation, properties, diagnostics, splitters, and status bar as lightweight MMC panes; remove card-like panel shadows/rounding and fixed status-bar branding.
- [x] 4.6 Normalize visible shell labels to concise Chinese text and verify that long document names, paths, and localized values do not clip at the supported minimum window size.

## 5. MDI Aero Windows

- [x] 5.1 Create and merge `AgentTerminal.Docking` Aero theme resources for child frames, active/inactive titles, typography, caption buttons, focus, borders, and bounded elevation.
- [x] 5.2 Refactor `MdiChildWindow` to consume shared resources and remove duplicate local gradients, effects, and font-dependent caption glyphs without changing bindings, commands, geometry, or AutomationId values.
- [x] 5.3 Implement active/inactive treatments that remain distinguishable through border contrast, title emphasis, and elevation when color saturation is reduced.
- [x] 5.4 Restyle the minimized-document tray and items as compact Win7 task buttons, preserving restore, close, title visibility, activation, and recoverable bounds.
- [x] 5.5 Apply the selected low-noise blue-gray MDI workspace background and verify that Aero effects remain at window level rather than terminal cells or repeated collection items.
- [x] 5.6 Run MDI drag, resize, activate, minimize, maximize, restore, close, cascade, horizontal-tile, vertical-tile, and session-isolation tests after the template migration.

## 6. Compatibility, Terminal Integration, and Documentation

- [x] 6.1 Implement a system-color high-contrast resource path and verify readable text, focus, selection, borders, inputs, and non-color-only session state communication.
- [x] 6.2 After `add-terminal-rendering` connects `TerminalControl` as the primary MDI document view, apply only the shared outer frame and verify that the terminal viewport remains opaque, unclipped, effect-free, and correctly focused.
- [x] 6.3 Execute the visual acceptance matrix at 100%, 125%, 150%, and 200% DPI, at 1024×768 and a common widescreen size, including active/inactive MDI windows and high-contrast mode.
- [x] 6.4 Record screenshots and results in a Win7 Aero UI validation document, including any intentional differences from native Windows 7 rendering.
- [x] 6.5 Update the existing Win7 Aero design document to reflect the implemented resource architecture and phase status, and replace unverifiable “100% pixel-perfect across systems” claims with measurable compatibility guarantees.
- [x] 6.6 Run the full unit and UI test suites, build the application, and validate this OpenSpec change strictly before marking implementation complete.
