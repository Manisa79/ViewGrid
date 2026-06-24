# AGENTS.md

## Project overview

T.T.Pano is a Windows Forms data presentation control for .NET Windows desktop applications. The core package lives in `src/Taylan.Pano`; demo, showcase and developer-facing screens live in `samples/Taylan.Pano.TestApp`.

Current source package version: `1.0.52.2`.

## Repository rules

- Keep the core DLL clean. Do not add sample forms, demo datasets, Example Center code or TestApp startup flows under `src/Taylan.Pano`.
- Put all demos, experiments, screenshots and user-facing example workflows under `samples/Taylan.Pano.TestApp` or `docs`.
- Follow the Designer First standard for persistent WinForms UI: controls belong in `.Designer.cs`; behavior, data loading and event logic belong in the form `.cs`.
- Do not commit generated build outputs: `.vs`, `bin`, `obj`, `artifacts`, `TestResults`, `*.dll`, `*.exe`, `*.pdb`, `*.nupkg`, `*.snupkg`, `*.csproj.user`.
- Keep version metadata aligned across `src/Taylan.Pano/Taylan.Pano.csproj`, `samples/Taylan.Pano.TestApp/Taylan.Pano.TestApp.csproj`, `src/Taylan.Pano/Core/PanoVersionInfo.cs`, `README.md`, `CHANGELOG.md`, release notes and issue templates.
- Preserve Turkish and English user-facing text intentionally. New visible strings should be localizable when they appear in reusable UI.

## Build and validation commands

Run these on Windows with the .NET SDK that supports `net10.0-windows` and WinForms:

```powershell
dotnet restore Taylan.Pano.sln
dotnet build Taylan.Pano.sln -c Release
./tools/QualityChecks/Taylan.Pano.QualityChecks.ps1
dotnet pack src/Taylan.Pano/Taylan.Pano.csproj -c Release -o artifacts/nuget /p:PanoRepositoryUrl=https://github.com/<owner>/Pano
```

The Linux/headless container may not have the .NET Windows desktop SDK. If `dotnet` is unavailable, perform static checks only and state that a real Windows build still needs to be run.

## Coding standards

- Prefer nullable-safe C# and keep `<Nullable>enable</Nullable>` assumptions intact.
- Avoid adding new production dependencies without a clear reason.
- Use `BeginUpdate` / `EndUpdate` style batching for large UI refreshes when available.
- Test Light, Dark and System theme paths when touching rendering, dialogs, icons, badges or filter popups.
- Consider Virtual Mode or cache strategies for large-data or media-heavy scenarios.
- Keep profile, state, filtering, export and localization behavior consistent across view modes.

## Release workflow

1. Clean generated outputs.
2. Run restore/build/quality checks on Windows.
3. Run TestApp smoke checks for Details, Card/Dashboard, media views, filtering, profile import/export, theme switching and screenshot capture.
4. Update `RELEASE_NOTES_v1_0_52_2.md` or create the next release notes file.
5. Create a release tag such as `v1.0.52.2-preview`.
6. Publish only source-ready files and generated artifacts from `artifacts/`, not local IDE state.
