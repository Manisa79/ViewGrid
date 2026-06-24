# Taylan.Pano v28.15.1 - Clean Architecture Build Fix

- `PanoColumnProfile` duplicate type conflict resolved.
- Data profiling model was renamed to `PanoDataColumnProfile`.
- Existing column layout/profile API remains `PanoColumnProfile` for compatibility.
- Ultimate data profiling APIs now return `PanoDataColumnProfile`.
- No feature removal: Query, Expression, Event Bus, Change Tracking, Layout Package, Smart Suggestions and Data Profiling remain available.
