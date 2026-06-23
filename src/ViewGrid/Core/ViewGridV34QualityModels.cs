namespace ViewGrid.Core;

/// <summary>
/// v34 Build Quality / Stability tarama sonuç seviyesi.
/// </summary>
public enum ViewGridQualitySeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// ViewGrid public API ve runtime davranışlarını hızlı kontrol etmek için hafif kalite maddesi.
/// </summary>
public sealed class ViewGridQualityCheckItem
{
    public ViewGridQualitySeverity Severity { get; set; } = ViewGridQualitySeverity.Info;
    public string Area { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Severity} | {Area} | {Code} | {Message}";
    }
}

public sealed class ViewGridQualityReport
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<ViewGridQualityCheckItem> Items { get; } = new();
    public int ErrorCount => Items.Count(x => x.Severity == ViewGridQualitySeverity.Error);
    public int WarningCount => Items.Count(x => x.Severity == ViewGridQualitySeverity.Warning);
    public bool IsClean => ErrorCount == 0;

    public void Add(ViewGridQualitySeverity severity, string area, string code, string message, string suggestion = "")
    {
        Items.Add(new ViewGridQualityCheckItem
        {
            Severity = severity,
            Area = area,
            Code = code,
            Message = message,
            Suggestion = suggestion
        });
    }
}
