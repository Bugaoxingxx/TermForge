## ADDED Requirements

### Requirement: Shared MDI Aero presentation
All MDI child windows SHALL consume shared Docking theme resources for their frame, title bar, typography, caption glyphs, and interaction states instead of defining independent local Aero resources.

#### Scenario: Multiple child windows are displayed
- **WHEN** three or more terminal documents are visible in the MDI workspace
- **THEN** every child window uses the same frame metrics and control templates while retaining its own title, content, commands, and AutomationId values

### Requirement: Distinct active and inactive windows
The MDI presentation SHALL distinguish the active child from inactive children through at least border, title treatment, and elevation, without relying on color alone or applying strong effects to every window.

#### Scenario: User activates another child window
- **WHEN** focus and active-document state move from child A to child B
- **THEN** child B receives the active title, border, and elevation treatment and child A receives the quieter inactive treatment

#### Scenario: Colors are viewed with reduced saturation
- **WHEN** the active and inactive windows are viewed without reliable color discrimination
- **THEN** differences in border contrast, title emphasis, or elevation still identify the active window

### Requirement: Accurate MDI caption controls
MDI minimize, maximize/restore, and close controls SHALL use pixel-aligned vector or bitmap glyphs, SHALL provide normal, hover, pressed, focus, and disabled states, and SHALL preserve the existing document commands and window-state behavior.

#### Scenario: User operates a child caption button with the pointer
- **WHEN** the user hovers, presses, and releases a child caption button
- **THEN** each interaction state is visible and exactly one corresponding document command is executed

#### Scenario: Child window changes maximized state
- **WHEN** the child is maximized or restored by its caption button or title-bar double-click
- **THEN** the maximize glyph changes to the appropriate restore or maximize glyph without depending on a font-specific Unicode character

#### Scenario: User navigates caption controls by keyboard
- **WHEN** a child caption command receives keyboard focus
- **THEN** a visible focus indication appears and the command can be invoked without a pointer

### Requirement: Win7-style minimized document tray
Minimized MDI documents SHALL appear in a compact, shared tray whose items resemble Windows 7 task buttons rather than independent floating cards, while preserving restore and close access.

#### Scenario: A child document is minimized
- **WHEN** the user minimizes an MDI child document
- **THEN** the child leaves the canvas, a compact tray item appears with its title and state, and the tray does not add a separate drop shadow to every item

#### Scenario: User restores a tray item
- **WHEN** the user invokes restore from a minimized document item
- **THEN** the document returns to its recoverable bounds, becomes active, and the tray item is removed

### Requirement: MDI effects remain bounded
Decorative effects in the MDI workspace SHALL be limited so that terminal rendering and multi-window interaction do not incur per-cell or per-list-item effect costs.

#### Scenario: Several terminals produce output
- **WHEN** multiple visible terminal documents update concurrently
- **THEN** Aero effects remain confined to window-level visuals and are not applied to terminal cells, output lines, or repeated navigation items

