# ViewGrid v28.8 - Cell Overflow Scroll & Reader UX

Bu sürüm uzun açıklama/not/log kolonları için satır yüksekliğini büyütmeden hücre içinde okunabilirlik sağlayan genel altyapıyı ekler.

## Yeni özellikler

- Details görünümünde hücre bazlı dikey scroll.
- Satır yüksekliği sabit kalır; uzun metin satırı büyütmez.
- Mouse hücre üzerindeyken tekerlek önce hücre içeriğini kaydırır.
- İnce mini scrollbar ve overflow fade ipuçları.
- Kolon bazlı aç/kapat.
- Kolon bazlı görünür maksimum satır sayısı.
- Çift tıklama ile uzun metin için reader popup.
- Mesaj, açıklama, teknisyen notu, hata detayı, log ve JSON gibi kolonlara uygundur.

## Kullanım

```csharp
var noteColumn = new ViewGridColumn("Teknisyen Notu", "TechnicianNote", 260)
{
    WordWrap = true,
    AllowCellScroll = true,
    CellScrollMaxVisibleLines = 4,
    ShowCellScrollBar = true,
    CellOverflowFade = true,
    CellOverflowDetailsOnDoubleClick = true
};

grid.EnableCellOverflowScroll = true;
grid.ShowCellOverflowScrollBars = true;
grid.EnableCellOverflowDetailsPopup = true;
```

## Tasarım zamanı özellikleri

ViewGridControl:
- EnableCellOverflowScroll
- ShowCellOverflowScrollBars
- EnableCellOverflowDetailsPopup

ViewGridColumn:
- AllowCellScroll
- ShowCellScrollBar
- CellScrollMaxVisibleLines
- CellOverflowFade
- CellOverflowDetailsOnDoubleClick

## Not

Bu özellik özellikle Details view için tasarlandı. Card/Dashboard tarafında taşan metin için CardMaxLines, card action veya detay popup ile birlikte kullanılabilir.
