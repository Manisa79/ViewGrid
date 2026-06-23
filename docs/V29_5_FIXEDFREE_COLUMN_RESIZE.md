# ViewGrid v29.5 - FixedFree Column Resize Behavior

## Amaç

Details görünümünde sağ tarafta kullanılabilir boş alan varken bir kolon elle genişletildiğinde yatay scrollbar'ın erken açılması engellendi.

ViewGrid artık ObjectListView'in `FixedFree` hissine daha yakın davranır:

- Kullanıcı bir kolonu genişlettiğinde önce `FillFreeSpace` kolonlarından boşluk alınır.
- Görünür alan tamamen dolana kadar yatay scrollbar açılmaz.
- `FillFreeSpace` kolonlarının minimum genişlikleri korunur.
- Boşluk gerçekten bittiyse yatay scrollbar normal şekilde devreye girer.

## Yeni özellik

```csharp
public bool AbsorbColumnResizeOverflowFromFreeSpace { get; set; } = true;
```

Designer kategorisi:

```text
ViewGrid - Column Manager
```

## Kullanım

Varsayılan olarak açıktır:

```csharp
glv.AbsorbColumnResizeOverflowFromFreeSpace = true;
```

Eski davranış istenirse kapatılabilir:

```csharp
glv.AbsorbColumnResizeOverflowFromFreeSpace = false;
```

## Not

Bu davranış yalnızca klasik Details/List grid düzenlerinde uygulanır. Tile, Card, Dashboard, Poster ve DetailCard gibi kart tabanlı görünümlerde yatay kolon genişliği mantığı kullanılmaz.
