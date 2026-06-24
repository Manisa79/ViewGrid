# Taylan.Pano v27.6 - Design-Time Theme Sync

Visual Studio WinForms designer yüzeyi çoğunlukla açık tema karakterinde çalışır. v27.6 ile PanoControl, runtime tema tercihini bozmadan tasarım zamanında temiz ve okunabilir açık tema önizlemesi verir.

## Yeni özellikler

- `EnableDesignTimeThemeSync`
- `DesignTimeFollowParentTheme`
- `DesignTimeThemeSyncMenus`
- SmartTag üzerinden designer tema seçenekleri
- Runtime temadan bağımsız designer güvenli tema uygulama

## Önerilen varsayılan

```csharp
panoView1.EnableDesignTimeThemeSync = true;
panoView1.DesignTimeThemePreview = PanoDesignTimeThemePreview.Auto;
panoView1.DesignTimeFollowParentTheme = false;
```

Bu ayarda designer açık tema gibi görünür, runtime ise `ThemePreset`, `FollowWindowsTheme` ve uygulama temasına göre çalışmaya devam eder.
