# V29.4 DetailCard Header Toggle

ViewGrid v29.4 ile `DetailCard` görünümünde kolon başlıklarının gösterilip gizlenebilmesi için yeni bir ayar eklendi.

## Yeni özellik

```csharp
viewgrid.ShowDetailCardColumnHeaders = true;  // Varsayılan: Başlık + değer
viewgrid.ShowDetailCardColumnHeaders = false; // Sadece değerler
```

## Ne işe yarar?

Başlıklar açıkken DetailCard her görünür kolonu etiket/değer olarak gösterir:

```text
Makine : ASMPT SX1 - 01
Hat    : TEST-LINE-01
Durum  : Online
```

Başlıklar kapalıyken aynı kart daha sade görünür:

```text
ASMPT SX1 - 01
TEST-LINE-01
Online
```

## Tasarım zamanı

Özellik `ViewGrid - DetailCard` kategorisinde tasarım zamanında da görünür. Böylece kullanıcı kod yazmadan DetailCard başlıklarını açıp kapatabilir.

## Örnek merkezi

`ViewModeShowcaseSampleForm` içine `DetailCard başlıkları` toggle butonu eklendi. Kullanıcı DetailCard modundayken başlıklı ve başlıksız görünümü anlık karşılaştırabilir.
