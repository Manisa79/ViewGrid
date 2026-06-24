# Taylan.Pano v28.6 - Card Layout Designer + Advanced Excel Filter + Row Height Stability

## Added

- Card Layout Designer foundation for Card/Tile/Dashboard views.
- `PanoCardLayoutDefinition` and `PanoCardLayoutField` models.
- `ShowCardLayoutDesigner()` API for runtime/layout tooling.
- Column-level card layout properties: `VisibleInCard`, `CardOrder`, `CardRole`, `CardShowCaption`, `CardMaxLines`.
- Advanced Excel-style filter shortcuts inside the filter popup/window:
  - Select Only This
  - Exclude Selected
  - Invert Selection
  - Top 10
  - Above Average
- Details row height stability fix:
  - `AutoRowHeightForMultilineCells` default is now `false`.
  - `LockDetailsRowHeightOnSelection` prevents accidental row-height expansion on click/selection.
  - `DetailsRowHeight` gives Details/List mode a stable row height independent from Card/Tile/Dashboard height.

## Why

Card/Tile/Dashboard rendering needs a reusable layout layer instead of one-off ticket-specific drawing. The new layout definition can be reused for support tickets, stock cards, production cards, files, orders, or machine dashboards.

The Details view row-height change prevents the issue where rows look normal at first, then expand after clicking a row because multiline/card height coercion leaks into Details selection refresh.
