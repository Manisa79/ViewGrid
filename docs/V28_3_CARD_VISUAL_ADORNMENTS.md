# Taylan.Pano v28.3 - Card Visual Adornments

Bu sürümde Card/Tile/Dashboard/Kanban/Timeline görünümleri için genel amaçlı görsel eklenti altyapısı eklendi.
Amaç AOI Support Desk gibi tek bir projeye özel durum noktası çizmek değil; stok, üretim, sipariş, dosya listesi, mesaj/ticket ekranı gibi tüm kart senaryolarında aynı Pano renderer altyapısını kullanabilmek.

## Yeni API

- `CardVisualAdornments`: Kart görsel eklentilerini aç/kapat.
- `CardVisualInfoGetter`: Satır bazlı `PanoCardVisualInfo` döndürür.
- `CardDefaultAccentMode`: Varsayılan accent çizimi. `Auto`, `TopBar`, `LeftBar`, `BottomBar`, `Outline`, `Glow`, `None`.
- `CardAutoBadgesFromBadgeColumns`: `PanoColumnKind.Badge` kolonlarını kart üzerinde otomatik rozet olarak gösterir.
- `CardBadgeSize`: Kart rozet boyutu.
- `CardBadgeMaxCount`: Kart üzerinde çizilecek maksimum rozet sayısı.

## Örnek

```csharp
pano.CardVisualInfoGetter = row =>
{
    TicketRow ticket = (TicketRow)row;

    PanoCardVisualInfo info = new PanoCardVisualInfo
    {
        AccentColor = ticket.StatusColor,
        DotColor = ticket.StatusColor,
        AccentMode = PanoCardAccentMode.TopBar
    };

    if (ticket.UnreadMessageCount > 0)
    {
        info.Badges.Add(new PanoCardBadge
        {
            Text = ticket.UnreadMessageCount.ToString(),
            Glyph = PanoCardGlyph.Message,
            BackColor = Color.Orange,
            Placement = PanoCardBadgePlacement.TopRight
        });
    }

    if (ticket.IsCritical)
    {
        info.Badges.Add(new PanoCardBadge
        {
            Glyph = PanoCardGlyph.Warning,
            BackColor = Color.Firebrick,
            Placement = PanoCardBadgePlacement.TopLeft
        });
    }

    return info;
};
```

## Tasarım Notu

Details görünümündeki hücre renderer artık tek kaynak değildir. Kart/dash görünümlerinde status dot, accent bar, ikon badge ve otomatik badge kolonları doğrudan kart renderer içinde çizilir.
