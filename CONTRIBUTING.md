# Contributing to ViewGrid

Thanks for trying ViewGrid. This repository is published as a Community Preview, so bug reports, screenshots and real-world usage feedback are very valuable.

## Before opening an issue

Please include:

- ViewGrid version
- .NET SDK / Visual Studio version
- Windows version
- Theme mode: Light, Dark, System or High Contrast
- View mode: List, Dashboard, DetailCard, Poster, Gallery, MediaTile, FilmStrip, etc.
- A small code sample or screenshot when possible

## Local build

```bash
dotnet restore ViewGrid.sln
dotnet build ViewGrid.sln -c Release
```

The core library lives under `src/ViewGrid`. All demo and showcase screens must stay under `samples/ViewGrid.TestApp`.

## Pull request rule

Please do not add sample/demo forms to `src/ViewGrid`. The DLL should stay clean and product-ready. Put all experiments under `samples/ViewGrid.TestApp`.
