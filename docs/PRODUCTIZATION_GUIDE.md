# Taylan.Pano Productization Guide

## Release hedefleri

1. Clean build geçmeli.
2. TestApp açılmalı.
3. Örnekler menüsünde PDF, filtre, profil, CardView, Dashboard, stress senaryoları görünmeli.
4. `tools/QualityChecks` duplicate public type ve bilinen syntax hatalarını yakalamalı.
5. NuGet paketi `artifacts/nuget` altına çıkmalı.

## Stress test yaklaşımı

Gerçekten 1 trilyon satır belleğe yüklenmez. Doğru yöntem sanal veri kaynağıdır:

- 10K / 100K: normal liste testi
- 1M: virtual provider testi
- 1B / 1T: index tabanlı fake provider testi

Amaç satırları RAM'e almak değil; scroll/render/filter pipeline'ının index üzerinden çalışmasını doğrulamaktır.

## Tema test matrisi

| Tema | DPI | Font Scale | Beklenen |
|---|---:|---:|---|
| Light | 100% | 100% | normal |
| Dark | 100% | 100% | koyu dialog/popup |
| Auto | 125% | 125% | taşma yok |
| Dark | 150% | 150% | icon/header düzgün |

## Migration safety

Legacy profil migration işlemi dosyayı dönüştürmeden önce `.bak` oluşturmalıdır.
