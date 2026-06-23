# ViewGrid v27.7 - Card / Large View Filter UX

Büyük kart, dashboard, kanban, poster ve geniş kart görünümlerinde filtreye header olmadan kolay erişim sağlar.

## Yeni özellikler

- `ShowQuickFilterBar`
- `ShowFloatingFilterButton`
- `ShowActiveFilterChips`
- `ShowFilterPresetsBar`
- `CardFilterUxOnlyInCardViews`
- `CardFilterUxPlacement`
- `CardFilterBarHeight`
- `QuickFilterPlaceholderText`
- `FloatingFilterButtonText`

## Önerilen kullanım

```csharp
grid.SetViewMode(ViewGridMode.DashboardCard);
grid.ShowQuickFilterBar = true;
grid.ShowFloatingFilterButton = true;
grid.ShowActiveFilterChips = true;
grid.FilterMenuMode = ViewGridFilterMenuMode.Both;
```

Bu yapı AOI Support Desk ticket kartları, MasterData BOM/program kartları ve makine/hat seçim ekranlarında filtreye tek tık erişim sağlar.
