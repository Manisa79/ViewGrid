# ViewGrid v51.2 - Core / Examples Separation

This update prepares ViewGrid for a cleaner GitHub/community preview structure.

## What changed

- ViewGrid core remains in `src/ViewGrid` as a reusable WinForms control library.
- Example/demo screens remain in `samples/ViewGrid.TestApp` only.
- The old detached `samples/ViewGrid.FeatureSamples` folder was moved under `samples/ViewGrid.TestApp/Snippets/FeatureSamples`.
- Detached productization sample notes were moved under `samples/ViewGrid.TestApp/Snippets/ProductizationSamples`.
- Legacy snippet files are excluded from TestApp compilation because they are copy/paste recipes and contain repeated demo model names.
- Core descriptions no longer reference Example Center cleanup or developer menus.

## Expected repository layout

```text
ViewGrid
├─ src
│  └─ ViewGrid                 # production DLL
├─ samples
│  └─ ViewGrid.TestApp         # example/developer app only
├─ docs
└─ assets
```

## Rule

Anything that is only for demonstration belongs in `ViewGrid.TestApp`, not in `ViewGrid.dll`.

`ViewGrid.dll` may still contain reusable runtime UI such as filter dialogs, command palette, column chooser and theme helpers because those are product features, not examples.
