# Taylan.Pano v28.9 - Card Checkbox Layout Polish

Card/Tile/Dashboard/Kanban görünümlerinde checkbox artık kart görsel eklentileriyle çakışmadan çizilebilir.

## Yeni özellikler

- `TileCheckBoxPosition`: TopLeft, TopRight, BottomLeft, BottomRight.
- `TileCheckBoxReserveTextArea`: Checkbox için metin/status-dot alanını otomatik ayırır.
- `TileCheckBoxDrawOnTop`: Badge, accent bar, action ve status göstergelerinden sonra checkbox'ı en üst katmanda çizer.
- `TileCheckBoxShowBackground`: Tema uyumlu mini arka plan ve border ile görünürlüğü artırır.
- `TileCheckBoxHitPadding`: Küçük checkbox'larda tıklama alanını büyütür.
- `TileCheckBoxVisibilityMode`: Always, Hover, Selected, HoverOrSelected, CheckedOrHoverOrSelected.

## Önerilen kullanım

```csharp
grid.TileCheckBoxes = true;
grid.TileCheckBoxPosition = PanoTileCheckBoxPosition.TopLeft;
grid.TileCheckBoxReserveTextArea = true;
grid.TileCheckBoxDrawOnTop = true;
grid.TileCheckBoxShowBackground = true;
grid.TileCheckBoxHitPadding = 6;
```

Kartta çok badge/ikon varsa ve sade görünüm istenirse:

```csharp
grid.TileCheckBoxVisibilityMode = PanoTileCheckBoxVisibilityMode.CheckedOrHoverOrSelected;
```

Bu modda checkbox işaretli kartlarda kalıcı görünür; diğer kartlarda hover veya selection sırasında görünür.
