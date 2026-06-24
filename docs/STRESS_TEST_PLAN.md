# Taylan.Pano Stress Test Plan

## Senaryolar

- 10K satır: normal kullanım
- 100K satır: hızlı filtre/sort
- 1M satır: virtual scroll
- 1B satır: fake virtual provider
- 1T satır: fake virtual provider metadata testi

## Başarı kriterleri

- UI ilk açılışta donmamalı.
- Satır üretimi lazy olmalı.
- Memory kullanımında satır sayısına lineer büyüme olmamalı.
- Details/Card/Dashboard geçişlerinde eski yükseklik cache'i kalmamalı.

## 1 trilyon satır gerçeği

1 trilyon kayıt ancak server-side paging, virtual provider ve index tabanlı veri modeliyle simüle edilir. Pano tarafında hedef, bu sayıyı belleğe almak değil; görünen aralığı hızlı istemek ve çizmek olmalıdır.
