# ViewGrid v28.3 - Card Visual Adornments

Bu sürümde Card/Tile/Dashboard/Kanban/Timeline görünümleri için genel amaçlı görsel eklenti altyapısı eklendi.
Amaç AOI Support Desk gibi tek bir projeye özel durum noktası çizmek değil; stok, üretim, sipariş, dosya listesi, mesaj/ticket ekranı gibi tüm kart senaryolarında aynı ViewGrid renderer altyapısını kullanabilmek.

## Yeni API

- `CardVisualAdornments`: Kart görsel eklentilerini aç/kapat.
- `CardVisualInfoGetter`: Satır bazlı `ViewGridCardVisualInfo` döndürür.
- `CardDefaultAccentMode`: Varsayılan accent çizimi. `Auto`, `TopBar`, `LeftBar`, `BottomBar`, `Outline`, `Glow`, `None`.
- `CardAutoBadgesFromBadgeColumns`: `ViewGridColumnKind.Badge` kolonlarını kart üzerinde otomatik rozet olarak gösterir.
- `CardBadgeSize`: Kart rozet boyutu.
- `CardBadgeMaxCount`: Kart üzerinde çizilecek maksimum rozet sayısı.

## Örnek

```csharp
viewgrid.CardVisualInfoGetter = row =>
{
    TicketRow ticket = (TicketRow)row;

    ViewGridCardVisualInfo info = new ViewGridCardVisualInfo
    {
        AccentColor = ticket.StatusColor,
        DotColor = ticket.StatusColor,
        AccentMode = ViewGridCardAccentMode.TopBar
    };

    if (ticket.UnreadMessageCount > 0)
    {
        info.Badges.Add(new ViewGridCardBadge
        {
            Text = ticket.UnreadMessageCount.ToString(),
            Glyph = ViewGridCardGlyph.Message,
            BackColor = Color.Orange,
            Placement = ViewGridCardBadgePlacement.TopRight
        });
    }

    if (ticket.IsCritical)
    {
        info.Badges.Add(new ViewGridCardBadge
        {
            Glyph = ViewGridCardGlyph.Warning,
            BackColor = Color.Firebrick,
            Placement = ViewGridCardBadgePlacement.TopLeft
        });
    }

    return info;
};
```

## Tasarım Notu

Details görünümündeki hücre renderer artık tek kaynak değildir. Kart/dash görünümlerinde status dot, accent bar, ikon badge ve otomatik badge kolonları doğrudan kart renderer içinde çizilir.
