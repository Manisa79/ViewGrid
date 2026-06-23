# ViewGrid v37-v40 Pro Experience Suite

Bu paket v36 Build/Theme/Media Pro üzerine dört yeni fazı tek akışta toplar.

## Faz 37 — Enterprise Layout

- `EnableEnterpriseLayout`
- `ApplyV37EnterpriseLayoutPack()`
- `SaveEnterpriseLayoutToJson(name)`
- `LoadEnterpriseLayoutFromJson(json)`
- Kolon genişliği, görünürlük, display index ve ViewMode snapshot desteği

## Faz 38 — Performance Pro

- `ApplyV38PerformanceProfile(...)`
- `Balanced`, `LargeData`, `MediaLibrary`, `VirtualMillionRows`, `LowMemory`
- Büyük veri filtre tarama limitleri
- Medya lazy-load/cache profili
- `BeginPerformanceBatch()` ile toplu işlem redraw koruması

## Faz 39 — Interaction Pro

- `ApplyV39InteractionProfile(...)`
- Command Palette + Search Everywhere + modern search panel
- Power user, touch friendly, Audix media ve factory operator profilleri
- `ShortcutActions` ile örnek kısayol listesi

## Faz 40 — Visual Analytics

- `ApplyV40AnalyticsProfile(...)`
- KPI Dashboard, HeatMap, Timeline, MiniChart ve FactoryOverview presetleri
- `AnalyticsMetrics`
- `CreateAnalyticsSummaryText()`

## Example Center

Yeni örnek:

- `ViewGridV37ToV40ProExperienceSampleForm`

SampleHub üstünde **Hızlı Erişim / Nerede Bulurum?** bölümüne eklendi.
Example Center Pro içine de `30 v37-v40 Pro Experience` senaryosu eklendi.

## Audix için öneri

Audix albüm ekranında:

```csharp
viewgrid.ApplyV38PerformanceProfile(ViewGridV38PerformancePreset.MediaLibrary);
viewgrid.ApplyV39InteractionProfile(ViewGridV39InteractionPreset.AudixMedia);
viewgrid.SetViewMode(ViewGridMode.Poster);
```

Büyük albüm arşivlerinde:

```csharp
using (viewgrid.BeginPerformanceBatch())
{
    viewgrid.SetObjects(albumRows);
    viewgrid.SetViewMode(ViewGridMode.Gallery);
}
```
