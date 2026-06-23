# ViewGrid v28.11 - Dialog Title Theme & Icons

ViewGrid owned helper windows now use a shared dialog chrome and theme-aware title icons.

## Added

- Dark/light native title bar synchronization for ViewGrid dialogs.
- Theme-aware generated icons, no external PNG/ICO dependency.
- Dialog icon kinds: Grid, Filter, Column, Search, Export, Designer, Command, Info, Warning, Success, Error.
- Applied to filter window, column chooser, command palette, modern search panel, card layout designer, advanced filter builder, column designer and toast notifications.
- Existing windows still use `ViewGridDialogChrome.ConfigureStandardDialog`; a new optional `iconKind` parameter controls the title icon.

## Usage

```csharp
ViewGridDialogChrome.ConfigureStandardDialog(
    form,
    theme,
    new Size(480, 320),
    sizeable: true,
    iconKind: ViewGridDialogIconKind.Filter);
```

The icon follows the active theme accent color and remains readable in dark and light modes.
