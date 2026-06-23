# Core / Examples Separation Report

Goal: keep `ViewGrid.dll` clean for product projects and move showcase/developer screens to `samples/ViewGrid.TestApp`.

## Current layout

- Core library: `src/ViewGrid`
- Sample application: `samples/ViewGrid.TestApp`
- Feature snippets and examples: `samples/ViewGrid.TestApp/Snippets`
- Documentation: `docs`

## Core policy

The core project must not contain:

- Example Center forms
- Developer Center forms
- TestApp startup forms
- Demo-only datasets
- Sample forms

All such files belong under `samples/ViewGrid.TestApp`.

## Static check performed

No `ExampleCenter`, `SampleForm`, `TestApp` or `FeatureSamples` references were found under `src/ViewGrid` during package preparation.
