# ViewGrid 1.0.51.6 Community Preview

This release prepares ViewGrid for public GitHub preview.

## Highlights

- Core DLL and sample/TestApp separation
- TestApp startup language selector with Designer.cs controls
- Built-in localization entry point through `ViewGridLocalization.Use(...)`
- Product/Developer menu separation
- Audix-oriented media views: Poster, Gallery, MediaTile and FilmStrip
- Media playback state preparation for audio/video cards
- Dark/Light theme accessibility hardening
- GitHub repository hygiene: MIT license, issue templates, PR template and release notes

## Repository layout

- `src/ViewGrid` — clean ViewGrid core library
- `samples/ViewGrid.TestApp` — showcase, developer samples and language selector
- `docs` — API notes, migration notes and feature history
- `.github` — workflow and contribution templates

## Preview note

This is a Community Preview. The public API can still receive cleanup based on real user feedback.


## 1.0.51.6 - Documentation Capture Mode

- TestApp içine Documentation Capture Mode eklendi.
- Example Center ekran görüntüleri otomatik PNG olarak üretilebilir.
- viewgrid-screenshot-manifest.json, viewgrid-screenshots.md ve viewgrid-docx-insert-map.json çıktıları eklendi.
- DOCX içine ekran görüntüsü yerleştirmek için tools/docs/insert_screenshots_into_docx.py yardımcı scripti eklendi.
