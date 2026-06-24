# Taylan.Pano ObjectListView Migration Status - v1.0.26.88

Bu sürümde Pano, BrightIdeasSoftware/ObjectListView bağımlılığı olmadan eski OLV/GLV kullanım kalıplarını karşılamaya odaklanır.

## Bu sürümde eklenenler

- `CaptureSelectionSnapshot()` / `RestoreSelectionSnapshot()`
- `UpdateObjectsPreserveSelection(rows, keyAspectName)`
- `FindObjectByAspect()` / `FindObjectsByAspect()`
- `SelectObjectByAspect()` / `SelectObjectsByAspectValues()`
- `RevealObjectByAspect()`
- `ToggleColumn()` / `ShowAllColumns()` / `HideAllColumnsExcept()`
- `GetCellText()` / `GetAspectValue()`
- Örnek merkezi: **OLV ekstra uyumluluk yardımcıları**

## Daha önce karşılanan ana OLV davranışları

- `AspectName`, `AspectGetter`, `AspectPutter`, `AspectToStringConverter`, `AspectToStringFormat`
- `SetObjects`, `AddObject(s)`, `RemoveObject(s)`, `ClearObjects`, `RefreshObject(s)`, `BuildList`
- `SelectedObject(s)`, `CheckedObject(s)`, `CheckBoxes`, `CheckedAspectName`
- `ModelFilter`, `AdditionalFilter`, `UseFiltering`, `ShowGroups`
- `PrimarySortColumn`, `SecondarySortColumn`, `Sort`, `ClearSort`
- `EmptyListMsg`, `AllColumns`, column visibility, context menu, filter popup
- Header checkbox, cell checkbox, hyperlink/button/progress/rating/tag cells
- Export, print preview, layout save/load, keyboard access, large virtual list samples

## Bilerek eklenmeyenler

- Doğrudan BrightIdeasSoftware tiplerine compile-time bağımlılık yok.
- OLV renderer sınıfları birebir taşınmadı; Pano kendi theme-aware paint motorunu kullanır.
- Eski OLV grouping renderer/hot item renderer sınıfları yerine Pano theme/format event sistemi kullanılmalı.
