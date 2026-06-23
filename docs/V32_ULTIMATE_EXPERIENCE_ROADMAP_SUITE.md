# ViewGrid v32.0 Ultimate Experience Roadmap Suite

Bu sürüm v31 sonrasında konuşulan Faz 38-48 akışını tek paket altında toparlar.

## Eklenen fazlar

- **Faz 38 UX Intelligence**: kullanıcı görünüm/kolon/filtre alışkanlıkları için `EnableUxIntelligence`, `RememberLastViewMode`, `SaveExperienceSnapshot`.
- **Faz 39 Factory Intelligence**: makine durum overlay hazırlığı, `FactoryStatusAspectName`, `ResolveFactoryStatus`, HeatMap/KPI senaryosu.
- **Faz 40 Timeline Engine**: ticket, makine ve işlem geçmişi için `ViewGridTimelineEvent` ve Timeline örneği.
- **Faz 41 Document Explorer**: PDF/CAD/video/audio/image/folder önizleme sınıflandırması için `ViewGridDocumentPreviewKind`, Gallery örneği.
- **Faz 42 Virtualization Pro**: büyük veri hedefleri için `EnableVirtualizationPro`, `VirtualizationProTargetRows`.
- **Faz 43 Search Everywhere**: tüm görünür kolonlarda arama için `SearchEverywhere`.
- **Faz 44 Command Palette**: host uygulamada komut merkezi için `EnableCommandPalette`.
- **Faz 45 Layout Studio**: kullanıcı layout tasarlama/kaydetme akışı için `EnableLayoutStudio`.
- **Faz 46 Dashboard Builder**: `DashboardWidgets`, `ViewGridDashboardWidgetDefinition`, KPI/HeatMap/Timeline/Gallery/Kanban widget tipleri.
- **Faz 47 AI Layer**: host uygulama önerileri için `ViewGridAiInsight`, `AddAiInsight`.
- **Faz 48 ViewGrid Ecosystem**: ViewGridKanban/ViewGridTimeline/ViewGridDashboard gibi ortak çekirdek yaklaşımı için birleşik property seti.

## Example Center

Example Center artık yeni özellikleri şu başlıklarda gösterir:

- v31 Audix Media Experience
- v32 Faz Merkezi / Nerede Bulurum
- v32 Factory Intelligence
- v32 Timeline Engine
- v32 Document Explorer
- v32 Virtualization Pro
- v32 Search Everywhere + Command Palette
- v32 Layout Studio
- v32 Dashboard Builder
- v32 AI Layer + Ecosystem

## Audix için önerilen kullanım

```csharp
viewgrid.SetViewMode(ViewGridMode.Poster);
viewgrid.TilePosterMode = true;
viewgrid.ShowMediaOverlayButton = true;
viewgrid.ShowMediaQualityBadge = true;
viewgrid.MediaQualityBadgeAspectName = "Format";
viewgrid.EnableMediaLazyLoading = true;
viewgrid.EnableMediaImageCache = true;
```

Albüm kapağı yine kolon `ImageGetter` üzerinden verilir; v31 medya ayarları görselin sunum davranışını yönetir.
