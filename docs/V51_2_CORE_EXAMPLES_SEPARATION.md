# Taylan.Pano v51.2 - Core / Examples Separation

This update prepares Pano for a cleaner GitHub/community preview structure.

## What changed

- Pano core remains in `src/Taylan.Pano` as a reusable WinForms control library.
- Example/demo screens remain in `samples/Taylan.Pano.TestApp` only.
- The old detached `samples/Pano.FeatureSamples` folder was moved under `samples/Taylan.Pano.TestApp/Snippets/FeatureSamples`.
- Detached productization sample notes were moved under `samples/Taylan.Pano.TestApp/Snippets/ProductizationSamples`.
- Legacy snippet files are excluded from TestApp compilation because they are copy/paste recipes and contain repeated demo model names.
- Core descriptions no longer reference Example Center cleanup or developer menus.

## Expected repository layout

```text
Pano
├─ src
│  └─ Pano                 # production DLL
├─ samples
│  └─ Taylan.Pano.TestApp         # example/developer app only
├─ docs
└─ assets
```

## Rule

Anything that is only for demonstration belongs in `Taylan.Pano.TestApp`, not in `Taylan.Pano.dll`.

`Taylan.Pano.dll` may still contain reusable runtime UI such as filter dialogs, command palette, column chooser and theme helpers because those are product features, not examples.
