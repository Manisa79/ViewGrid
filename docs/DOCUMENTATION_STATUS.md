# Documentation Status

The repository contains Markdown documentation under `docs/`.

A separate Word guide was supplied during package preparation with version `1.0.51`. Because the source package version is `1.0.52.2`, do not publish that `.docx` as the canonical release guide until its title, version references and screenshots are refreshed.

Recommended canonical public docs for this GitHub source release:

- `README.md`
- `docs/API_REFERENCE.md`
- `docs/GITHUB_PUBLISHING_GUIDE.md`
- `docs/PRODUCTIZATION_GUIDE.md`
- `docs/CORE_EXAMPLES_SEPARATION_REPORT.md`
- `RELEASE_NOTES_v1_0_52_2.md`

## v1.0.52.2 screenshot completion flow

The TestApp now contains a dedicated `Missing Documentation` capture category for the 31 screenshots tracked in the Word guide under `70. Görsel Durumu ve Eksik Takip`.

Recommended completion flow:

1. Run `samples/Taylan.Pano.TestApp`.
2. Open `Documentation Capture Mode`.
3. Click `Eksik DOCX Görselleri`.
4. Generate PNG files into `docs/screenshots`.
5. Run `tools/docs/insert_screenshots_into_docx.py` to place screenshots inline using `TargetHeading`.

See `docs/V52_2_MISSING_SCREENSHOT_CAPTURE.md`.
