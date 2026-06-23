# ViewGrid v1.0.51.4 - TestApp Language Form Polish

## Amaç

GitHub öncesi TestApp dil seçimi penceresi daha düzgün, designer dostu ve tekrar erişilebilir hale getirildi.

## Değişiklikler

- `StartupLanguageForm` iki parçaya ayrıldı:
  - `StartupLanguageForm.cs`: sadece davranış/kayıt/yükleme mantığı
  - `StartupLanguageForm.Designer.cs`: tüm WinForms kontrolleri ve yerleşim
- Checkbox görünmeme/kırpılma problemi düzeltildi.
- Dil penceresi yüksekliği artırıldı ve buton alanı sabitlendi.
- TestApp açıldıktan sonra sol menüden **Dil Değiştir** seçeneği ile dil tekrar değiştirilebilir.
- Yeni açılan ViewGrid örnekleri ve built-in menüler seçilen dili kullanır.

## Not

Dil seçimi `settings/viewgrid-testapp-language.json` içinde saklanır.
