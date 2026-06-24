# V29.2 Pano checkbox event migration fix

Bu paket, OLV/ListView tarafındaki `ItemCheck` kullanımının `FastPanoControl` üzerinde doğru event modeliyle karşılanması için migration örneğini günceller.

## Net karar

`FastPanoControl` içinde `ItemCheck` yoktur ve eklenmemelidir. Pano tarafında checkbox değişimi için önerilen yol:

- `CellValueChanged`: checkbox/toggle değeri değiştiğinde kullanılır.
- `CellClick`: sadece tıklama koordinasyonu gerekiyorsa kullanılır.
- `ItemChecked` / `ObjectChecked`: GLV compatibility event'i olarak satır checked state değişimini bildirir.

## Eklenen güvenli yardımcılar

`PanoCellClickEventArgs` içine:

```csharp
bool IsCheckBoxColumn { get; }
```

`PanoCellEditEventArgs` içine:

```csharp
bool? NewValueAsBoolean { get; }
```

Böylece uygulama tarafında kolon türü ve yeni değer güvenli şekilde kontrol edilir.

## MasterData/AOI gibi projelerde önerilen kullanım

```csharp
glvPanel.CellValueChanged += GlvPanel_CellValueChanged;

private void GlvPanel_CellValueChanged(object? sender, PanoCellEditEventArgs e)
{
    if (!e.IsCheckBoxColumn) return;

    bool isChecked = e.NewValueAsBoolean == true;

    if (isChecked)
    {
        // Tek seçim mantığı: diğer model satırlarının checked alanını false yap.
        // Sonra grid.RefreshObjects(...) ile görseli yenile.
    }
}
```

## Kontrol edilen benzer durumlar

- Kaynakta `FastPanoControl.ItemCheck` veya `glvPanel.ItemCheck` benzeri hatalı kullanım bulunmadı.
- Kalan `.ItemCheck +=` kullanımları `CheckedListBox` tabanlı popup/chooser listelerine aittir; bunlar Pano event'i değildir ve doğrudur.
- `(MethodInvoker)delegate` kullanımı kaynakta bulunmadı. Ambiguous riskli yerlerde uygulama tarafında `System.Windows.Forms.MethodInvoker` veya alias kullanılmalıdır.
