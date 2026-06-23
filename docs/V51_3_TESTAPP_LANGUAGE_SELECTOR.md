# ViewGrid v1.0.51.3 - TestApp Language Selector

## Amaç

GitHub / Community Preview öncesinde `ViewGrid.TestApp` başlangıcına dil seçimi eklendi.
Bu seçim ViewGrid'nin dahili menüleri, filtre pencereleri, kolon seçici, layout menüleri ve built-in dialog metinleri için tek merkezden uygulanır.

## Eklenenler

- `ViewGrid.Localization.ViewGridLocalization` facade sınıfı
- `ViewGridLocalization.SupportedLanguages`
- `ViewGridLocalization.Use(ViewGridLanguage language)`
- `ViewGridLocalization.DisplayName(...)`
- `ViewGridLocalization.FromName(...)`
- `samples/ViewGrid.TestApp/StartupLanguageForm`
- TestApp başlangıcında dil seçim ekranı
- Dil seçiminin `settings/viewgrid-testapp-language.json` içinde saklanması

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
using ViewGrid.Localization;

ViewGridLocalization.Use(ViewGridLanguage.Turkish);
```

veya tek bir ViewGridControl üzerinden:

```csharp
viewgrid.Language = ViewGridLanguage.English;
```

Not: `ViewGridControl.Language` mevcut API ile uyumlu kalması için korunmuştur. Arka planda global ViewGrid localization dilini değiştirir.

## Kapsam

Bu seçim ViewGrid çekirdeğinin kendi ürettiği metinleri etkiler:

- Sağ tık / header menüleri
- Filtre penceresi metinleri
- Kolon seçici
- Layout ve grouping menü metinleri
- Built-in dialog metinleri

Uygulamanın kendi menüleri ve özel formları için aynı dili kullanmak istenirse uygulama tarafında `ViewGridLocalization.T("Key")` veya kendi resource sistemi kullanılmalıdır.
