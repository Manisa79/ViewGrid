# ViewGrid Release Checklist

- [ ] `bin` ve `obj` klasörleri temizlendi.
- [ ] `build/Build-ViewGrid.ps1 -Configuration Release -Pack` başarılı.
- [ ] TestApp açıldı.
- [ ] Details satır yüksekliği seçimden sonra değişmiyor.
- [ ] Poster -> Card/Dashboard geçişinde yükseklik cache'i kalmıyor.
- [ ] Header filter popup ve menu filter popup aynı görünüyor.
- [ ] Popup filtre resize çalışıyor.
- [ ] PDF export örneği görünüyor.
- [ ] Profil import/export/migration test edildi.
- [ ] Changelog güncellendi.
- [ ] Release zip oluşturuldu.

- [ ] `.git`, `.vs`, `bin`, `obj`, `artifacts`, `*.user`, `*.dll`, `*.exe`, `*.pdb` dosyaları public source zip içinde yok.
- [ ] README, changelog, issue templates, `ViewGridVersionInfo` ve `.csproj` sürümü `1.0.52.2` ile tutarlı.
- [ ] NuGet publish gerekiyorsa `/p:ViewGridRepositoryUrl=https://github.com/<owner>/ViewGrid` değeri verildi.


## Documentation screenshot completion

- [x] Turkish and English DOCX guides are included under `docs/`.
- [x] Screenshots were reviewed against document headings.
- [x] Misleading or weak screenshot placements were removed.
- [x] Documentation index and screenshot audit are included.

## Documentation finalization for GitHub

- [x] Turkish user/developer guide added under `docs/`.
- [x] English user/developer guide added under `docs/`.
- [x] Screenshot placement audit added under `docs/`.
- [x] Misleading screenshot placements were removed from the final guides.
- [ ] Final Windows build and TestApp smoke test completed before public release.
