# LoginWindow

**Type:** WPF Window
**Location:** `EngineeringPerformance.App/Views/LoginWindow.xaml`
**Last Updated:** 2026-04-15

## Purpose

Authentication entry point for the Engineering Performance application. Provides user login with Windows authentication detection, credential input, error feedback, and database settings access.

## Layout Structure

The window uses a two-column split layout:

```
┌─────────────────────────────────────────────┐
│  Left Panel (280px)  │  Right Panel (*)     │
│  Dark branding       │  Light form area     │
│                      │                      │
│  [Logo]              │  Authorization       │
│  App Title           │  Windows user info   │
│  Subtitle            │  Username field      │
│                      │  Password field      │
│                      │  Error message       │
│                      │  [Login] [Settings]  │
└─────────────────────────────────────────────┘
```

### Left Panel — Branding Area

- **Width:** Fixed at `280px`
- **Background:** `#0D1B2A` (dark navy, matches splash screen)
- **Corner radius:** `12,0,0,12` (rounded left edges only)
- **Content:** Centered `StackPanel` with `30px` margin
  - Logo image: `180x180`, uniform stretch, sourced from `/Assets/logo.png`
  - App title: "Engineering Performance", `18px`, bold, white
  - Subtitle: "Система управления эффективностью", `12px`, `#8899AA`, `8px` top margin

### Right Panel — Login Form

- **Width:** Remaining space (`*`)
- **Background:** `#F7F8FA` (light gray)
- **Corner radius:** `0,12,12,0` (rounded right edges only)
- **Content:** `ScrollViewer` with `40,32` padding for scrollable form on small windows

Form elements (top to bottom):

1. **Header:** "Авторизация" — `24px`, bold, `#0D1B2A`
2. **Description:** "Введите данные для входа в систему" — `13px`, `#6B7280`, `28px` bottom margin
3. **Windows username display** (conditional) — shown when `HasWindowsUsername` is true
4. **Username field** — `dxe:TextEdit`, `40px` height, `14px` font
5. **Password field** — `dxe:PasswordBoxEdit` / `dxe:TextEdit` toggle, `40px` height, with visibility button
6. **Error message** — styled `Border` with red background, shown when `HasErrorMessage` is true
7. **Action buttons** — Login button + settings gear icon

## Design Tokens

### Colors

| Token | Value | Usage |
|-------|-------|-------|
| `--brand-dark` | `#0D1B2A` | Left panel background, login button, header text |
| `--brand-hover` | `#1B2838` | Login button hover state |
| `--brand-pressed` | `#0A1520` | Login button pressed state |
| `--surface-light` | `#F7F8FA` | Right panel background |
| `--border-default` | `#D1D5DB` | Input field borders |
| `--text-primary` | `#374151` | Form labels, Windows username |
| `--text-secondary` | `#6B7280` | Description text, field labels |
| `--text-muted` | `#8899AA` | Subtitle text |
| `--error-bg` | `#FEE2E2` | Error message background |
| `--error-border` | `#FECACA` | Error message border |
| `--error-text` | `#DC2626` | Error message text |
| `--disabled-bg` | `#4A5568` | Disabled button background |
| `--disabled-text` | `#A0AEC0` | Disabled button text |
| `--btn-hover` | `#E0E0E0` | Flat icon button hover |
| `--btn-pressed` | `#C0C0C0` | Flat icon button pressed |

### Typography

| Element | Size | Weight | Color |
|---------|------|--------|-------|
| Window title | `24px` | Bold | `#0D1B2A` |
| App title (left panel) | `18px` | Bold | White |
| Form labels | `13px` | SemiBold | `#374151` |
| Input fields | `14px` | Normal | Default |
| Description text | `13px` | Normal | `#6B7280` |
| Subtitle | `12px` | Normal | `#8899AA` |
| Error message | `13px` | Normal | `#DC2626` |

### Spacing

- Form padding: `40px` horizontal, `32px` vertical
- Field bottom margin: `16px`
- Label-to-field gap: `6px`
- Header to form gap: `28px`
- Error message padding: `12px`

## Responsive Behavior

| Property | Value | Notes |
|----------|-------|-------|
| Default size | `700x480` | Width x Height |
| MinWidth | `600` | Prevents form from becoming too narrow |
| MinHeight | `400` | Ensures all fields remain visible |
| ResizeMode | `CanResize` | User can resize freely |
| Left panel | Fixed `280px` | Does not shrink on resize |
| Right panel | `*` (star) | Expands/shrinks with window |
| ScrollViewer | `VerticalScrollBarVisibility="Auto"` | Enables scrolling when content exceeds available height |

## Custom Styles

### LoginButtonStyle

Dark-themed primary action button with full state coverage:

```xml
<Style x:Key="LoginButtonStyle" TargetType="Button">
  <!-- Default: #0D1B2A bg, white text, 6px corner radius -->
  <!-- Hover: #1B2838 bg -->
  <!-- Pressed: #0A1520 bg -->
  <!-- Disabled: #4A5568 bg, #A0AEC0 text -->
</Style>
```

### FlatIconButton

Minimal icon button for settings gear and password toggle:

```xml
<Style x:Key="FlatIconButton" TargetType="Button">
  <!-- Default: Transparent bg, no border -->
  <!-- Hover: #E0E0E0 bg -->
  <!-- Pressed: #C0C0C0 bg -->
</Style>
```

## Visual Hierarchy

1. **Primary:** Left panel branding draws attention first (dark contrast)
2. **Secondary:** "Авторизация" header establishes context
3. **Tertiary:** Form fields guide user through input sequence
4. **Quaternary:** Login button as the primary call-to-action (dark, prominent)
5. **Supporting:** Settings gear icon (subtle, secondary action)

## Dependencies

- DevExpress WPF: `dxe:TextEdit`, `dxe:PasswordBoxEdit`
- DevExpress MVVM: `dxmvvm:BooleanToVisibilityConverter`, `dxmvvm:BooleanNegationConverter`, `dxmvvm:DispatcherService`
- Custom: `InverseBooleanToVisibilityConverter` (password visibility toggle)
- Assets: `/Assets/logo.png`, `/Assets/password-hidden.png`, `/Assets/settings.png`

## Bindings

| Property | Binding | Purpose |
|----------|---------|---------|
| `HasWindowsUsername` | `BooleanToVisibilityConverter` | Show Windows auth info |
| `WindowsUsername` | Direct | Display detected Windows user |
| `Username` | TwoWay, PropertyChanged | Username input |
| `Password` | TwoWay, PropertyChanged | Password input |
| `PasswordVisible` | `BooleanToVisibilityConverter` | Toggle password field type |
| `HasErrorMessage` | `BooleanToVisibilityConverter` | Show/hide error box |
| `ErrorMessage` | Direct | Error text content |
| `IsBusy` | `BooleanNegationConverter` | Disable login during operation |
| `LoginCommand` | Command | Submit credentials |
| `TogglePasswordVisibilityCommand` | Command | Toggle password visibility |
| `OpenDbSettingsCommand` | Command | Open database settings |
