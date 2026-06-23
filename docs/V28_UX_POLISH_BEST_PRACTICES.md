# ViewGrid v28 UX Polish + Best Practice Pack

Bu sürümde kart/poster/dashboard görünümlerinde global filtre butonu kart alanından üst hızlı filtre barına taşındı. Böylece filtre aksiyonu veri kartı aksiyonu gibi görünmez.

## Eklenenler

- `ApplyV28UxPolish(...)`
- `UseTopBarFilterOnly()`
- `UseHybridCardFilterUx()`
- `EnableV28CardFilterPolish`
- `MoveFilterButtonToTopBar`
- `ShowCardInlineFilterButton` uyumluluk alias'ı
- `QuickActionBarMode`
- MasterData / SupportDesk / PosterGallery / LargeData UX presetleri

## Tasarım kararı

Filtre global bir grid davranışıdır; kart üstünde ya da kart sağında durduğunda item aksiyonu gibi algılanır. v28 varsayılanı üst bardır.

## İlham alınan popüler grid davranışları

DevExpress tarafında filter/search panel ve kolon filtre dropdownları; Telerik tarafında hızlı grouping/filtering/virtualized grid davranışı; Syncfusion tarafında drag-drop, context menu, export/summary gibi enterprise grid beklentileri incelendi. ViewGrid tarafında bunlar hafif ve WinForms designer dostu property'ler olarak toparlandı.
