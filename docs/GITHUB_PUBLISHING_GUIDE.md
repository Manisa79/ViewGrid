# GitHub Publishing Guide

Recommended public release name:

```text
ViewGrid 1.0.52.2 Community Preview
Tag: v1.0.52.2-preview
```

## 1. Final local checks

```bash
dotnet restore ViewGrid.sln
dotnet build ViewGrid.sln -c Release
```

Optional package check:

```bash
dotnet pack src/ViewGrid/ViewGrid.csproj -c Release
```

## 2. Create repository

Create a new GitHub repository named `ViewGrid`.

## 3. Push source

```bash
git init
git add .
git commit -m "Initial ViewGrid community preview"
git branch -M main
git remote add origin https://github.com/<owner>/ViewGrid.git
git push -u origin main
```

## 4. Create release

Use:

```text
Tag: v1.0.52.2-preview
Title: ViewGrid 1.0.52.2 Community Preview
```

Release notes are prepared in:

```text
release/RELEASE_NOTES_v1.0.52.2-preview.md
```

## 5. Important positioning

Do not present this as a final LTS release yet. Use Community Preview / Early Access wording until more real-world feedback is collected.

## 6. Source package hygiene

The public source package should not contain local IDE state or generated outputs.

Do not commit:

- `.git/` from exported archives,
- `.vs/`,
- `bin/`,
- `obj/`,
- `artifacts/`,
- generated `*.dll`, `*.exe`, `*.pdb`, `*.nupkg` files,
- `*.csproj.user` files or local settings JSON.

## 7. Recommended release command

```powershell
./build/Build-ViewGrid.ps1 -Configuration Release -Pack
./release/Make-ReleaseZip.ps1 -Version 1.0.52.2 -Configuration Release -SkipBuild
```
