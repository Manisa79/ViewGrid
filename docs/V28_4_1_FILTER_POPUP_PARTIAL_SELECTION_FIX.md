# ViewGrid v28.4.1 - Filter Popup Partial Selection Fix

- Popup filtre listesindeki tekil seçimlerde `Tümünü seç` artık doğru şekilde `Indeterminate` durumuna geçer.
- `Apply` işlemi artık `CheckBox.Checked` yerine `CheckState.Checked` kontrol eder.
- WinForms'ta `Indeterminate` durumunda `Checked == true` dönebildiği için kısmi seçimlerin yanlışlıkla "tümünü seç" gibi algılanması düzeltildi.
- Sonuç: tek değer seçip `Uygula` dendiğinde filtre artık doğru şekilde uygulanır.
