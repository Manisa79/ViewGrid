# ViewGrid v27.8 - Card Layout + Tree UX

## Card / Poster filter layout fix

v27.7 ile gelen Quick Filter Bar, Poster/Card/Dashboard gibi header dışı büyük görünümlerde üstte çiziliyordu. v27.8 ile kart çizim alanı artık bu barın yüksekliğini hesaba katar.

Yeni ayarlar:

```csharp
grid.ShowQuickFilterBar = true;
grid.CardViewReserveFilterArea = true;
grid.CardFilterContentSpacing = 6;
```

Davranış:

- Poster/Card/Dashboard/Kanban kartları filtre barının altında başlar.
- Hit-test, checkbox overlay ve `GetCellBounds` aynı offset ile çalışır.
- Scroll görünür satır hesabı filtre alanını düşer; üstte yarım kart kalmaz.
- İstenirse eski overlay davranışı için `CardViewReserveFilterArea = false` yapılabilir.

## TreeViewGridControl UX Pack

TreeViewGridControl için üretim/MasterData kullanımına uygun hızlı ağaç işlemleri eklendi.

Yeni ayarlar ve API:

```csharp
treeGrid.EnableTreeContextMenu = true;
treeGrid.TreeDoubleClickTogglesNode = true;
treeGrid.TreeSearchBehavior = TreeViewGridSearchBehavior.ExpandAncestorsAndDescendants;
treeGrid.TreeDefaultExpandLevel = 2;

treeGrid.ApplyTreeSearch("R12");
treeGrid.ExpandDescendants(selectedModel, 3);
treeGrid.CollapseDescendants(selectedModel);
string path = treeGrid.GetNodePath(selectedModel);
```

Sağ tık menüsünde:

- Seçili düğümü genişlet/daralt
- Alt dalları genişlet/daralt
- Tümünü genişlet/daralt
- Varsayılan seviyeye kadar aç
- Düğüm yolunu kopyala

## Example Center

Örnek merkezinde `v27.7 / v27.8 Card + Tree UX` ve `TreeView + TreeGrid` bölümü güncellendi.
