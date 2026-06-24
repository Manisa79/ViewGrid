# Taylan.Pano v27.5 - Menu & Icon Customization

Bu sürüm Pano menülerini host projeye göre tamamen yönetilebilir hale getirir.

## Eklenenler

- `MenuOptions` designer property grubu
- `MenuIcons` designer property grubu
- Header/body/merged menü gruplarını ayrı ayrı aç/kapatma
- Item bazlı görünürlük: `VisibleMenuItems`, `HiddenMenuItems`
- Runtime API:
  - `SetMenuGroupVisible(...)`
  - `SetMenuItemVisible(...)`
  - `SetCustomMenuIconFolder(...)`
  - `SetCustomMenuIconImageList(...)`
- Built-in menüyü tamamen kapatma veya kullanıcı `ContextMenuStrip` ile merge etme
- Özel ikonlar için klasör, ImageList, dark/light ImageList ve SVG resolver desteği
- Example Center Pro içinde `Menu & Icon Customization` senaryosu

## Örnek

```csharp
grid.MenuOptions.HeaderGroups = PanoMenuGroups.Filter | PanoMenuGroups.Sort | PanoMenuGroups.ColumnChooser;
grid.MenuOptions.BodyGroups = PanoMenuGroups.Clipboard | PanoMenuGroups.ViewMode;
grid.SetMenuItemVisible(PanoMenuItemKeys.AdvancedFilter, false);
grid.SetCustomMenuIconFolder(@"C:\MyApp\PanoIcons");
```

## İkon key isimleri

- `filter`
- `clear_filter`
- `sort_asc`
- `sort_desc`
- `columns`
- `layout`
- `theme`
- `view`
- `copy`
- `print`
- `export`
- `group`
- `advanced_filter`
- `state`
- `scenario`

PNG/ICO/JPG/BMP klasörden otomatik yüklenir. SVG için `CustomMenuIconResolver` ile raster image döndürülmelidir.
