# Taylan.Pano v30.0.1 - Size Compatibility Fix

Bu sürümde `PanoDialogChrome.ConfigureStandardDialog` için geriye uyumlu overload'lar eklendi.

Düzeltilen hata:

```text
3 bağımsız değişkeni: 'int' öğesinden 'System.Drawing.Size' öğesine dönüştürülemiyor
```

Sebep:
- Yeni dialog chrome API'sinde üçüncü parametre `System.Drawing.Size` bekliyordu.
- Bazı eski örnek/proje kodlarında genişlik/yükseklik `int` olarak gönderilebiliyordu.

Eklenen uyumluluk overload'ları:

```csharp
ConfigureStandardDialog(Form form, PanoTheme theme, int minimumSize, bool sizeable = true, PanoDialogIconKind iconKind = PanoDialogIconKind.Grid)
ConfigureStandardDialog(Form form, PanoTheme theme, int minimumWidth, int minimumHeight, bool sizeable = true, PanoDialogIconKind iconKind = PanoDialogIconKind.Grid)
```
