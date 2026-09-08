## ADDED Requirements

### Requirement: Unified Aero control baseline
The application SHALL load the WPF Windows 7-era Aero theme before TermForge-specific theme dictionaries, and SHALL present standard and custom shell controls through one semantic color, typography, metric, and state system.

#### Scenario: Application starts with the Aero resource set
- **WHEN** the main application window is created
- **THEN** all required Aero and TermForge resource dictionaries resolve without error and standard controls use the Aero baseline before product overrides

#### Scenario: A standard control has not been custom templated
- **WHEN** a standard WPF control such as a menu item, text box, scroll bar, or tree item is displayed without a TermForge-specific template
- **THEN** it uses the explicitly loaded Aero baseline instead of inheriting an unrelated modern operating-system theme

### Requirement: Win7-style main window chrome
The main window SHALL provide a custom Win7 Aero-style caption and frame while preserving the expected primary-window behaviors supplied through WindowChrome or equivalent system integration.

#### Scenario: User manipulates the title bar
- **WHEN** the user drags the title bar, double-clicks it, or opens its context menu
- **THEN** the window moves, toggles maximize/restore, or shows the system menu using standard Windows behavior

#### Scenario: User uses caption commands
- **WHEN** the user invokes minimize, maximize/restore, or close from the caption buttons or corresponding system shortcuts
- **THEN** the same window command executes and the visual state reflects the resulting window state

#### Scenario: Main window is maximized
- **WHEN** the main window enters the maximized state on any monitor
- **THEN** it occupies that monitor's working area without covering the taskbar and remains recoverable through standard window commands

#### Scenario: User resizes or snaps the window
- **WHEN** the user drags any resize border or uses a supported Windows Snap gesture
- **THEN** the window resizes or snaps with native-feeling feedback and no transparent-window compatibility regression

### Requirement: MMC shell hierarchy
The main window SHALL retain the menu, command toolbar, navigation pane, MDI workspace, optional properties and diagnostics panes, and status bar, while visually organizing them as a Windows 7 MMC/Explorer-style workbench.

#### Scenario: User scans the default workbench
- **WHEN** the default main window is shown
- **THEN** the terminal workspace is the visual focus, auxiliary panes are lighter and subordinate, and glass/high-glow effects are limited to window framing regions

#### Scenario: User opens the toolbar
- **WHEN** the command toolbar is visible
- **THEN** it exposes only high-frequency immediate commands and groups window arrangement variants behind one accessible arrangement entry

#### Scenario: User reads auxiliary pane titles
- **WHEN** navigation, properties, or diagnostics pane titles are displayed in the Chinese interface
- **THEN** each title uses concise Chinese text without redundant English or implementation terminology

#### Scenario: Status bar is displayed
- **WHEN** the application is idle or a terminal document is active
- **THEN** the status bar contains relevant dynamic state and document/session indicators without fixed promotional branding

### Requirement: Win7 typography and spacing
Shell UI SHALL use Segoe UI with Microsoft YaHei UI fallback for simplified Chinese, a normal UI size equivalent to 9pt at 96 DPI, and a consistent 5/7/11 DIP spacing rhythm for labels, related controls, and groups.

#### Scenario: Chinese and Latin labels are rendered
- **WHEN** a shell surface contains Chinese and Latin UI text
- **THEN** the text uses the designated UI font stack, remains legible, and does not fall below the defined auxiliary-text minimum

#### Scenario: Common command controls are compared
- **WHEN** buttons, labels, inputs, and separators appear across two shell panes
- **THEN** they use the shared metric resources instead of unrelated local sizes and margins

### Requirement: Terminal content remains visually isolated
The Aero shell SHALL frame the terminal document without applying transparency, glass, blur, rounded clipping, or decorative effects to the terminal character viewport.

#### Scenario: TerminalControl is hosted in an MDI document
- **WHEN** the terminal renderer change has connected TerminalControl as the primary document content
- **THEN** the viewport remains an opaque high-contrast terminal surface while its surrounding frame follows the Aero shell theme

