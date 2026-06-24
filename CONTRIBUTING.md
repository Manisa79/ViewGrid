# Contributing to Pano

Thanks for trying Taylan.Pano. This repository is published as a Community Preview, so bug reports, screenshots and real-world usage feedback are very valuable.

## Before opening an issue

Please include:

- Pano version
- .NET SDK / Visual Studio version
- Windows version
- Theme mode: Light, Dark, System or High Contrast
- View mode: List, Dashboard, DetailCard, Poster, Gallery, MediaTile, FilmStrip, etc.
- A small code sample or screenshot when possible

## Local build

```bash
dotnet restore Taylan.Pano.sln
dotnet build Taylan.Pano.sln -c Release
```

The core library lives under `src/Taylan.Pano`. All demo and showcase screens must stay under `samples/Taylan.Pano.TestApp`.

## Pull request rule

Please do not add sample/demo forms to `src/Taylan.Pano`. The DLL should stay clean and product-ready. Put all experiments under `samples/Taylan.Pano.TestApp`.
