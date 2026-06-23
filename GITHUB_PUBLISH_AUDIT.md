# ViewGrid GitHub Publish Audit

Version audited: `1.0.52.2`  
Package type: source-first Community Preview

## Completed cleanup

- Removed `.git/`, `.vs/`, `.agents/`, `bin/`, `obj/`, `artifacts/`, `TestResults/` and generated DLL/EXE/PDB files from the public source package.
- Removed `*.csproj.user` and local Visual Studio state.
- Aligned main public-facing version references to `1.0.52.2`.
- Updated `README.md`, `docs/GITHUB_PUBLISHING_GUIDE.md`, `RELEASE_CHECKLIST.md`, issue templates and `ViewGridVersionInfo`.
- Added GitHub-ready repository hygiene files: `AGENTS.md`, `.editorconfig`, `.gitattributes`, `SECURITY.md`, `SUPPORT.md`, `CODE_OF_CONDUCT.md`.
- Hardened NuGet metadata by adding MIT package license metadata and replacing the hard-coded placeholder repository URL with optional `ViewGridRepositoryUrl`.

## Static checks performed in this environment

- File tree scanned for generated binary outputs.
- File tree scanned for stale placeholder repository metadata.
- File tree scanned for old active release markers; historical changelog/release-note references are retained only as history.
- Core/examples separation policy reviewed at path level.
- Release scripts and CI files reviewed for GitHub use.

## Build limitation

This environment does not have `dotnet` installed, so a real `dotnet restore`, `dotnet build`, WinForms designer load and TestApp smoke run could not be executed here.

Before publishing the repository publicly, run on Windows:

```powershell
dotnet restore ViewGrid.sln
dotnet build ViewGrid.sln -c Release
./tools/QualityChecks/ViewGrid.QualityChecks.ps1
dotnet pack src/ViewGrid/ViewGrid.csproj -c Release -o artifacts/nuget /p:ViewGridRepositoryUrl=https://github.com/<owner>/ViewGrid
```

## Remaining owner-specific value

Set the final GitHub owner when packing or publishing NuGet:

```powershell
/p:ViewGridRepositoryUrl=https://github.com/<owner>/ViewGrid
```

GitHub source publishing itself is ready after the Windows build verification succeeds.

## Python static quality mirror

The repository's PowerShell quality check was mirrored with a Python static scan in this environment:

- public type declarations scanned under `src/ViewGrid`: 216
- duplicate public type names found: 5
- known accidental syntax markers found: 0

This does not replace the Windows `.NET` build.


## v1.0.52.2 documentation screenshot capture update

- Added `DocumentationMissingScreenshotsForm.cs` for the 31 DOCX sections listed under missing screenshot tracking.
- Added `Eksik DOCX Görselleri` selection button to `DocumentationCaptureForm`.
- Updated capture manifest/map output with `TargetHeading` for inline DOCX insertion.
- Updated `tools/docs/insert_screenshots_into_docx.py` to support inline placement and legacy gallery mode.

## Compile Fix - 1.0.52.2 missing screenshot capture

- Added `ViewGrid.Localization` import coverage for `DocumentationMissingScreenshotsForm` and TestApp global usings.
- Fixes CS0103/CS0246 style build errors where `ViewGridLanguage` was not visible in the missing DOCX screenshot scenario form.


## Documentation package added

- Added Turkish guide: `docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_TR.docx`
- Added English guide: `docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_EN.docx`
- Added documentation index: `docs/USER_GUIDE_INDEX.md`
- Added screenshot placement audit: `docs/ViewGrid_Full_Image_Audit_v1.0.52.2.md`

The documentation is suitable for GitHub source publishing, with the remaining mandatory gate being a real Windows/.NET restore/build/TestApp smoke check.
