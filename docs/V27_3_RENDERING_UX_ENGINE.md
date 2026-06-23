# ViewGrid v27.3 Rendering & UX Engine

Bu sürümde v27.2 kullanıcıya açılan state/preset altyapısının üzerine görsel hücre renderer davranışları eklendi.

## Eklenenler

- Semantic Badge Renderer: Open, Waiting, Done, Fail, OK, Offline, SAP/BOM gibi metinleri anlamlı renklerle gösterir.
- Chip Tag Renderer: Tag kolonlarında etiketleri ayrı pill/chip görünümleriyle çizer.
- Hyperlink UX: Link hücreleri daha belirgin ve tema uyumlu hale getirildi.
- Modern ProgressBar ile aynı görsel dilde çalışacak pill radius ayarları eklendi.
- Example Center artık başlangıç ekranı olarak tek merkezden yönetilir.
- v27.3 Renderer Showcase örneği eklendi.

## Yeni Propertyler

- `EnableV273RenderingUx`
- `BadgeUseSemanticStatusColors`
- `TagsUseChipRenderer`
- `CellPillCornerRadius`

## Örnek Merkezi

Uygulama artık `SampleHubForm` ile başlar. Tüm eski örnekler, yeni v27 örnekleri ve açıklamaları sol menülü tek merkezden açılır.
