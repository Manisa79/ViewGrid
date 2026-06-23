# ViewGrid 1.0.52.2 Community Preview

ViewGrid 1.0.52.2 is a source-first Community Preview for WinForms developers.

## Highlights

- Core library and TestApp/examples are separated.
- Details, Card, Dashboard, DetailCard, Poster, Gallery, MediaTile and FilmStrip workflows are documented.
- Theme, localization, filtering, export, profile and media scenarios are documented.
- Turkish and English user/developer guides are included under `docs/`.
- Screenshot placement has been reviewed against document headings.

## Documentation

- `docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_TR.docx`
- `docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_EN.docx`
- `docs/USER_GUIDE_INDEX.md`
- `docs/ViewGrid_Full_Image_Audit_v1.0.52.2.md`

## Validation required before public release

This package was prepared in a Linux/headless environment where Windows Forms build execution was not available. Before pushing publicly, run on Windows:

```powershell
dotnet restore ViewGrid.sln
dotnet build ViewGrid.sln -c Release
./tools/QualityChecks/ViewGrid.QualityChecks.ps1
```

For NuGet packaging:

```powershell
dotnet pack src/ViewGrid/ViewGrid.csproj -c Release -o artifacts/nuget /p:ViewGridRepositoryUrl=https://github.com/<owner>/ViewGrid
```
