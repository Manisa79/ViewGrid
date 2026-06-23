# ViewGrid

**ViewGrid** is a modern WinForms grid/list/card control for .NET Windows desktop apps. It started as an ObjectListView-style replacement and grew into a themed, virtualized, multi-view data presentation control.

> Current release: **1.0.52.2 Community Preview**

ViewGrid is still in Community Preview. The API is usable, but feedback from real projects may still shape naming, defaults and module boundaries.

> Note: In this preview, some internal project, namespace, file and class names still use the original `ViewGrid` / `ViewGridControl` naming for compatibility. These names may be gradually migrated in a future release.

## Highlights

- Classic grid/list view
- Dashboard and DetailCard views
- Poster, Gallery, MediaTile and FilmStrip media views
- Audix-style album cover and media playback state support
- Dark/Light/System theme support
- Theme accessibility and contrast hardening
- Column filtering, sorting, icons and badges
- Layout/profile helpers
- Example/TestApp separated from the core DLL
- Language selector in TestApp

## Repository layout

```text
ViewGrid/
├─ src/
│  └─ ViewGrid/                 # Core DLL, legacy internal project name
├─ samples/
│  └─ ViewGrid.TestApp/         # Showcase, examples and developer center
├─ docs/                      # API notes, migration docs and feature history
├─ release/                   # Release notes
├─ assets/                    # Logo/assets
├─ tools/                     # Quality/stress helper tools
└─ .github/                   # CI and issue templates
Getting started

Current preview API:

using ViewGrid.Core;

var viewGrid = new ViewGridControl();
viewGrid.Dock = DockStyle.Fill;
viewGrid.ViewMode = ViewGridMode.Details;
viewGrid.SetObjects(items);
Controls.Add(viewGrid);
Media view example
viewGrid.ApplyAudix51MediaPilotDefaults();
viewGrid.ViewMode = ViewGridMode.Poster;
viewGrid.SetObjects(trackList);

A media item can expose fields such as title, artist, album, duration and cover image. The TestApp includes Poster, Gallery, MediaTile, FilmStrip and playback-state examples.

Localization

TestApp includes a startup language selection screen. Host apps can apply localization globally:

ViewGridLocalization.Use("tr-TR");
Documentation

The full user/developer guide is available in both languages:

Turkish guide
English guide
Documentation index
Screenshot placement audit
Publish readiness report

These documents use inline screenshots that were reviewed against their target headings.

Build
dotnet restore ViewGrid.sln
dotnet build ViewGrid.sln -c Release

Requirements:

Windows
.NET SDK with Windows Forms support
Visual Studio 2022 or newer recommended
Samples

All samples live in samples/ViewGrid.TestApp. The core DLL intentionally does not contain Example Center or Developer Center forms.

Run the TestApp to explore:

Basic list/grid views
Media views
Theme Lab
Audix pilot sample
Developer samples
Performance/stress helpers
Status

This is a Community Preview. Recommended release label:

ViewGrid 1.0.52.2 Community Preview
Tag: v1.0.52.2-preview
Contributing

Please open issues for bugs, theme/accessibility problems, performance reports and feature suggestions. See CONTRIBUTING.md.

License

MIT License. See LICENSE.

Documentation Capture Mode

ViewGrid v1.0.52.2 includes a documentation screenshot workflow inside samples/ViewGrid.TestApp.

Open:

Documentation / Documentation Capture Mode

Then select the Example Center screens to capture. The tool generates:

docs/screenshots/*.png
docs/screenshots/viewgrid-screenshot-manifest.json
docs/screenshots/viewgrid-screenshots.md
docs/screenshots/viewgrid-docx-insert-map.json

To append generated screenshots into the Word guide:

python tools/docs/insert_screenshots_into_docx.py \
  --docx docs/ViewGrid_Professional_Developer_User_Guide.docx \
  --screenshots docs/screenshots \
  --output docs/ViewGrid_Professional_Developer_User_Guide_with_Screenshots.docx
GitHub publish readiness

This repository is prepared as a source-first Community Preview package:

generated Visual Studio and build outputs are excluded from the release source package,
the core library currently remains under src/ViewGrid,
showcase and developer examples currently remain under samples/ViewGrid.TestApp,
release metadata is aligned to 1.0.52.2,
CI builds on Windows because WinForms requires Windows targeting.

Before creating a public GitHub release, run the local release checklist in RELEASE_CHECKLIST.md.

For NuGet packaging, pass your final repository URL as an MSBuild property:

dotnet pack src/ViewGrid/ViewGrid.csproj -c Release -o artifacts/nuget /p:ViewGridRepositoryUrl=https://github.com/Manisa79/ViewGrid
Naming roadmap

The public repository and product name are now ViewGrid.

For compatibility, this preview still exposes the original API names such as:

ViewGridControl
ViewGridMode
ViewGridLocalization
ViewGrid.Core

A future release may introduce ViewGrid-native API names while keeping compatibility aliases for existing preview users.
