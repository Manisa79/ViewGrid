# ViewGrid v28.2 - Card Status Indicators

Card/Tile/Dashboard/Kanban/Poster çizim yolu artık Details hücre renderer'ına bağımlı kalmadan durum rengini kendi içinde çizebilir.

## Yeni özellikler

- `CardStatusIndicators`: kart durum göstergelerini aç/kapat.
- `CardStatusTopBar`: kart üst barını durum rengine bağlar.
- `CardStatusDot`: kart başlığı solunda renkli durum noktası çizer.
- `CardStatusAspectName`: durum değerinin okunacağı kolon. Boş bırakılırsa `Status`, `Durum`, `State`, `TicketStatus` gibi kolonlar otomatik bulunur.
- `CardStatusColorGetter`: uygulama tarafının doğrudan renk vermesi için runtime delegate.
- `CardStatusValueGetter`: durum değeri kolondan değil modelden özel okunacaksa runtime delegate.

## AOI Support Desk senaryosu

Details görünümde hücre renderer/status icon çalışırken CardView/Dashboard ayrı çizim kullandığı için icon kayboluyordu. v28.2 ile kart renderer aynı status bilgisini okuyup nokta ve üst bar olarak kendisi çiziyor.

Varsayılan durum renkleri:

- Yeni/Bekleyen: sarı
- Bakılıyor/Açık/In Progress: mavi
- Tamamlandı/Çözüldü/OK: yeşil
- Kapalı/İptal/Closed/Cancel: gri
