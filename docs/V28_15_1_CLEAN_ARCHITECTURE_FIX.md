# ViewGrid v28.15.1 - Clean Architecture Build Fix

- `ViewGridColumnProfile` duplicate type conflict resolved.
- Data profiling model was renamed to `ViewGridDataColumnProfile`.
- Existing column layout/profile API remains `ViewGridColumnProfile` for compatibility.
- Ultimate data profiling APIs now return `ViewGridDataColumnProfile`.
- No feature removal: Query, Expression, Event Bus, Change Tracking, Layout Package, Smart Suggestions and Data Profiling remain available.
