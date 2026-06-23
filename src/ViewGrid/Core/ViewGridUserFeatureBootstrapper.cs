namespace ViewGrid.Core;

/// <summary>
/// Host projelerde aynı v27 ayarlarını tekrar tekrar yazmamak için hızlı başlangıç yardımcıları.
/// MasterData, AOI Support Desk ve üretim araçlarında drop-in kullanım hedeflenmiştir.
/// </summary>
public static class ViewGridUserFeatureBootstrapper
{
    public static ViewGridControl UseMasterDataDefaults(this ViewGridControl grid, string stateFilePath, string stateKeyAspectName = "Id", ViewGridScenario scenario = ViewGridScenario.DataTable)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        grid.StateKeyAspectName = stateKeyAspectName;
        grid.AutoStateFilePath = stateFilePath ?? string.Empty;
        grid.AutoLoadStateOnCreate = true;
        grid.AutoSaveStateOnDispose = true;
        grid.ShowStateMenuItems = true;
        grid.ShowScenarioMenuItems = true;
        grid.ActiveScenario = scenario;
        grid.ApplyActiveScenario();
        return grid;
    }

    public static ViewGridControl UseBomPositionDefaults(this ViewGridControl grid, string stateFilePath, string stateKeyAspectName = "MaterialCode")
        => grid.UseMasterDataDefaults(stateFilePath, stateKeyAspectName, ViewGridScenario.BomPositions);

    public static ViewGridControl UseTicketBoardDefaults(this ViewGridControl grid, string stateFilePath, string stateKeyAspectName = "Id")
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        grid.StateKeyAspectName = stateKeyAspectName;
        grid.AutoStateFilePath = stateFilePath ?? string.Empty;
        grid.AutoLoadStateOnCreate = true;
        grid.AutoSaveStateOnDispose = true;
        grid.ShowStateMenuItems = true;
        grid.ShowScenarioMenuItems = true;
        grid.ActiveScenario = ViewGridScenario.TicketBoard;
        grid.ApplyActiveScenario();
        return grid;
    }
}
