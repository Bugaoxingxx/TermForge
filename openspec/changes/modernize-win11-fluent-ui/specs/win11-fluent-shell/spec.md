## ADDED Requirements

### Requirement: Fluent theme follows the system
The application SHALL enable the WPF Fluent theme in `System` mode so light, dark, and accent colors follow the user's Windows settings.

#### Scenario: User runs Windows in dark mode
- **WHEN** the system color mode is dark and the application starts
- **THEN** the main window chrome and untemplated standard controls use the dark Fluent baseline instead of a hard-coded Aero light palette

#### Scenario: User changes accent color
- **WHEN** the Windows accent color changes while the application is running or on the next launch
- **THEN** semantic highlight and focus tokens update from the system accent rather than remaining a fixed HEX value

### Requirement: Native window chrome and system backdrop
The main window SHALL use a Fluent or system caption with a system-drawn backdrop on Windows 11 22621+, and SHALL NOT keep the opaque painted Aero glass caption as the primary chrome.

#### Scenario: Window opens on Windows 11 22H2 or newer
- **WHEN** the main window is shown on Windows 11 build 22621 or newer
- **THEN** the window uses a system backdrop (Mica or an explicitly chosen equivalent) and rounded corners, and the old opaque `AeroGlassBrush` caption is no longer the window frame

#### Scenario: User uses standard window gestures
- **WHEN** the user drags the title bar, double-clicks it, opens the system menu, resizes from a border, snaps, or maximizes
- **THEN** native Windows behavior is preserved, Snap Layouts remain available if the OS provides them, and the maximized window stays inside the monitor working area

#### Scenario: Backdrop is unavailable
- **WHEN** the host is Windows 10 or DWM backdrop application fails
- **THEN** the window falls back to Acrylic or a solid theme color without a black frame and remains fully operable

### Requirement: Terminal viewport stays isolated
Fluent materials, blur, rounded clipping, and decorative shadows SHALL NOT be applied to the terminal character viewport.

#### Scenario: TerminalControl is hosted in an MDI document
- **WHEN** a terminal document is visible
- **THEN** the viewport remains an opaque high-contrast surface (the existing `#0C0F14` contract) while only the surrounding shell uses Fluent materials

### Requirement: High contrast remains a first-class path
The application SHALL keep a high-contrast resource path that maps shell and MDI tokens to `SystemColors` and disables decorative glow and drop shadows.

#### Scenario: User enables high contrast
- **WHEN** Windows high contrast is on or becomes on
- **THEN** shell text, borders, focus, and selection use system colors, glow and shadow effects are disabled, and success versus danger states are not communicated by color alone

### Requirement: MMC workbench structure is unchanged
The Fluent shell SHALL keep the menu, command toolbar, navigation pane, MDI workspace, optional properties and diagnostics panes, and status bar.

#### Scenario: User scans the default workbench
- **WHEN** the default main window is shown
- **THEN** the three-pane MMC layout and MDI document commands remain available, and only the visual language of the outer chrome has changed
