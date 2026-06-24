# V29.3 DetailCard Mode

Pano v29.3 ile `PanoViewMode.DetailCard` eklendi.

## Amaç

Dashboard/CardView okunabilirliğini koruyup, klasik griddeki tüm görünür kolonları tek kayıt kartı içinde satır satır göstermek.

Bu görünüm özellikle şu ekranlar için uygundur:

- Ticket detay listeleri
- Makine / line haritaları
- BOM / CAD kontrol sonuçları
- Log ve rapor kayıtları
- Operatör / teknisyen mesaj geçmişleri

## Kullanım

```csharp
glvPanel.SetViewMode(PanoViewMode.DetailCard);
```

veya:

```csharp
glvPanel.ViewMode = PanoViewMode.DetailCard;
```

## Davranış

- Her kayıt tam satır genişliğinde tek kart olarak çizilir.
- Kart içinde tüm görünür kolonlar `Kolon Başlığı : Değer` şeklinde alt alta gösterilir.
- Header gizlenir; kart görünümü gibi çalışır.
- Sağ tık görünüm menüsüne `DetailCard` seçeneği eklendi.
- `ViewModeShowcaseSampleForm` içine örnek kullanım eklendi.
- Checkbox, badge, status accent, card action ve card visual info altyapısıyla uyumlu bırakıldı.

## Not

DetailCard, `Poster` yerine geçmez. Poster görsel katalog içindir; DetailCard ise veri satırını okunabilir detay kartına dönüştürmek için tasarlanmıştır.
