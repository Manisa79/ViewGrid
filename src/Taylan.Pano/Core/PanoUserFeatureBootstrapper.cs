namespace Taylan.Pano.Core;

/// <summary>
/// Host projelerde aynı v27 ayarlarını tekrar tekrar yazmamak için hızlı başlangıç yardımcıları.
/// MasterData, AOI Support Desk ve üretim araçlarında drop-in kullanım hedeflenmiştir.
/// </summary>
public static class PanoUserFeatureBootstrapper
{
    public static PanoControl UseMasterDataDefaults(this PanoControl grid, string stateFilePath, string stateKeyAspectName = "Id", PanoScenario scenario = PanoScenario.DataTable)
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

    public static PanoControl UseBomPositionDefaults(this PanoControl grid, string stateFilePath, string stateKeyAspectName = "MaterialCode")
        => grid.UseMasterDataDefaults(stateFilePath, stateKeyAspectName, PanoScenario.BomPositions);

    public static PanoControl UseTicketBoardDefaults(this PanoControl grid, string stateFilePath, string stateKeyAspectName = "Id")
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        grid.StateKeyAspectName = stateKeyAspectName;
        grid.AutoStateFilePath = stateFilePath ?? string.Empty;
        grid.AutoLoadStateOnCreate = true;
        grid.AutoSaveStateOnDispose = true;
        grid.ShowStateMenuItems = true;
        grid.ShowScenarioMenuItems = true;
        grid.ActiveScenario = PanoScenario.TicketBoard;
        grid.ApplyActiveScenario();
        return grid;
    }
}
