## ADDED Requirements

### Requirement: DPI-correct desktop rendering
The desktop shell SHALL remain aligned, readable, and fully operable at 100%, 125%, 150%, and 200% display scaling, using device-independent metrics and appropriately sized image resources.

#### Scenario: Application opens at a supported DPI
- **WHEN** the main window opens at 100%, 125%, 150%, or 200% scaling
- **THEN** borders and glyphs are not visibly blurred by half-pixel placement, text is not clipped, and caption controls remain fully visible and clickable

#### Scenario: Window moves between monitors
- **WHEN** the main window moves between monitors with different supported scaling values
- **THEN** the shell relayouts at the destination scale without losing resize, caption, or document interaction regions

### Requirement: Production-quality icon resources
The application SHALL use a multi-size ICO for its application identity and SHALL use dedicated 16-DIP-aware visual resources for toolbar, tree, status, and caption commands instead of Emoji or font-dependent Unicode symbols.

#### Scenario: Windows requests an application icon
- **WHEN** the application icon is shown in the title bar, taskbar, Alt+Tab, or a large shell surface
- **THEN** Windows can select an appropriate embedded icon size without displaying a JPEG background or scaling a single low-resolution image upward

#### Scenario: Command icons render under another font configuration
- **WHEN** the system font, locale, or fallback font changes
- **THEN** command and caption glyph shapes remain consistent because they do not depend on Emoji or arbitrary font glyphs

### Requirement: Complete interaction states
Interactive shell and MDI controls SHALL provide distinguishable normal, hover, pressed, keyboard-focus, disabled, checked where applicable, and inactive-selection states.

#### Scenario: User navigates without a mouse
- **WHEN** the user moves focus through menus, toolbar commands, pane controls, tree items, and window commands using the keyboard
- **THEN** the focused control is visibly identifiable and the focus order remains logical

#### Scenario: A command is unavailable
- **WHEN** a shell or document command cannot currently execute
- **THEN** its disabled state remains readable and is communicated by more than global opacity alone

### Requirement: High-contrast fallback
The UI SHALL provide a usable high-contrast presentation based on system colors, and SHALL not rely solely on Aero gradients, red/green meaning, transparency, or shadows to communicate state.

#### Scenario: High-contrast mode is active
- **WHEN** the application window is opened in Windows high-contrast mode
- **THEN** text, inputs, selection, focus, borders, and essential status indicators use system-compatible colors with sufficient separation

#### Scenario: Session state is communicated
- **WHEN** a session is running, stopped, failed, or selected
- **THEN** text or iconography accompanies any color distinction so the state remains understandable without color

### Requirement: Cross-version visual fallback
The Win7-inspired shell SHALL have a deterministic XAML-rendered fallback and SHALL not require legacy DWM glass support to remain usable on supported Windows versions.

#### Scenario: Legacy Aero glass is unavailable
- **WHEN** the operating system does not expose Windows 7 DWM glass composition
- **THEN** the custom frame renders its defined opaque or translucent-looking XAML treatment without a black frame, invisible controls, or loss of window behavior

### Requirement: Repeatable visual acceptance matrix
The change SHALL be accepted against documented screenshots and behavioral checks covering supported DPI values, minimum/common window sizes, active/inactive MDI states, and high-contrast mode.

#### Scenario: UI change is prepared for completion
- **WHEN** implementation tasks are reported complete
- **THEN** the validation record contains results for the defined DPI and window-size matrix plus window chrome, keyboard focus, icon, and active-window checks

