# Core / Examples Separation Report

Goal: keep `Taylan.Pano.dll` clean for product projects and move showcase/developer screens to `samples/Taylan.Pano.TestApp`.

## Current layout

- Core library: `src/Taylan.Pano`
- Sample application: `samples/Taylan.Pano.TestApp`
- Feature snippets and examples: `samples/Taylan.Pano.TestApp/Snippets`
- Documentation: `docs`

## Core policy

The core project must not contain:

- Example Center forms
- Developer Center forms
- TestApp startup forms
- Demo-only datasets
- Sample forms

All such files belong under `samples/Taylan.Pano.TestApp`.

## Static check performed

No `ExampleCenter`, `SampleForm`, `TestApp` or `FeatureSamples` references were found under `src/Taylan.Pano` during package preparation.
