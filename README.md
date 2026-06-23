# ViewGrid

**ViewGrid** is a modern WinForms grid/list/card control for .NET Windows desktop apps. It started as an ObjectListView-style replacement and grew into a themed, virtualized, multi-view data presentation control.

> Current release: **1.0.52.2 Community Preview**

ViewGrid is still in Community Preview. The API is usable, but feedback from real projects may still shape naming, defaults and module boundaries.

> Note: In this preview, some internal project, namespace, file and class names still use the original `Gridly` / `GridlyView` naming for compatibility. These names may be gradually migrated in a future release.

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
│  └─ Gridly/                 # Core DLL, legacy internal project name
├─ samples/
│  └─ Gridly.TestApp/         # Showcase, examples and developer center
├─ docs/                      # API notes, migration docs and feature history
├─ release/                   # Release notes
├─ assets/                    # Logo/assets
├─ tools/                     # Quality/stress helper tools
└─ .github/                   # CI and issue templates
