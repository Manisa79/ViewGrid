# Taylan.Pano v1.0.51.3 - TestApp Language Selector

## Amaç

GitHub / Community Preview öncesinde `Taylan.Pano.TestApp` başlangıcına dil seçimi eklendi.
Bu seçim Pano'nin dahili menüleri, filtre pencereleri, kolon seçici, layout menüleri ve built-in dialog metinleri için tek merkezden uygulanır.

## Eklenenler

- `Taylan.Pano.Localization.PanoLocalization` facade sınıfı
- `PanoLocalization.SupportedLanguages`
- `PanoLocalization.Use(PanoLanguage language)`
- `PanoLocalization.DisplayName(...)`
- `PanoLocalization.FromName(...)`
- `samples/Taylan.Pano.TestApp/StartupLanguageForm`
- TestApp başlangıcında dil seçim ekranı
- Dil seçiminin `settings/pano-testapp-language.json` içinde saklanması

## Desteklenen diller

- Auto / System
- Türkçe
- English
- Deutsch
- Français
- Español
- Italiano
- Русский
- العربية
- 中文
- 日本語

## Uygulamalarda kullanım

```csharp
using Taylan.Pano.Localization;

PanoLocalization.Use(PanoLanguage.Turkish);
```

veya tek bir PanoControl üzerinden:

```csharp
pano.Language = PanoLanguage.English;
```

Not: `PanoControl.Language` mevcut API ile uyumlu kalması için korunmuştur. Arka planda global Pano localization dilini değiştirir.

## Kapsam

Bu seçim Pano çekirdeğinin kendi ürettiği metinleri etkiler:

- Sağ tık / header menüleri
- Filtre penceresi metinleri
- Kolon seçici
- Layout ve grouping menü metinleri
- Built-in dialog metinleri

Uygulamanın kendi menüleri ve özel formları için aynı dili kullanmak istenirse uygulama tarafında `PanoLocalization.T("Key")` veya kendi resource sistemi kullanılmalıdır.
