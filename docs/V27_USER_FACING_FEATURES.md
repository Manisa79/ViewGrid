# ViewGrid v27.2 User Facing Features

v27.0 çekirdekte başlayan State Engine, Range Virtual Provider ve görsel profil altyapısı v27.2 ile kullanıcıya açıldı.

## Eklenen kullanıcıya dönük parçalar

- `ActiveScenario`: ViewGrid'nin MasterData, BOM, ticket, timeline, yoğun veri gibi hazır görünüm senaryolarını designer veya runtime menüden seçtirir.
- `ShowStateMenuItems`: Başlık/gövde sağ tık menüsüne State / Preset menüsünü ekler.
- `ShowScenarioMenuItems`: Başlık/gövde sağ tık menüsüne Görünüm Senaryosu menüsünü ekler.
- `AutoStateFilePath`: Varsayılan state dosya yolu.
- `AutoLoadStateOnCreate`: Kontrol açılırken state yükler.
- `AutoSaveStateOnDispose`: Kontrol kapanırken state kaydeder.
- `SaveStateToDefaultPath()` / `LoadStateFromDefaultPath()`.
- `SaveStatePreset(name)` / `LoadStatePreset(name)` / `GetStatePresetNames()`.
- `ResetRuntimeState(...)`: filtre, grup ve kolon görünümünü hızlı sıfırlar.

## MasterData için önerilen başlangıç

```csharp
grid.ActiveScenario = ViewGridScenario.BomPositions;
grid.StateKeyAspectName = "MaterialCode";
grid.ShowStateMenuItems = true;
grid.ShowScenarioMenuItems = true;
grid.AutoStateFilePath = Path.Combine(Application.StartupPath, "Layouts", "bom-grid.json");
grid.AutoLoadStateOnCreate = true;
grid.AutoSaveStateOnDispose = true;
grid.ApplyActiveScenario();
```

## AOI Support Desk için önerilen başlangıç

```csharp
ticketGrid.ActiveScenario = ViewGridScenario.TicketBoard;
ticketGrid.StateKeyAspectName = "Id";
ticketGrid.ShowStateMenuItems = true;
ticketGrid.ShowScenarioMenuItems = true;
ticketGrid.ApplyActiveScenario();
```

## Not

Bu sürümde özellikler runtime ve designer tarafına bağlandı. Kanban / Timeline / MasterDetail gibi modların özel çizim motoru halen mevcut ViewGrid render altyapısı üzerinden güvenli şekilde çalışır; ilerleyen sürümde ayrı layout motoruna ayrılabilir.

## Tek satır başlangıç yardımcıları

```csharp
grid.UseBomPositionDefaults(
    Path.Combine(Application.StartupPath, "Layouts", "bom-grid.json"),
    stateKeyAspectName: "MaterialCode");

ticketGrid.UseTicketBoardDefaults(
    Path.Combine(Application.StartupPath, "Layouts", "tickets.json"));
```
