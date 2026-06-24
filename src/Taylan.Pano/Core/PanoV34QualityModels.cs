namespace Taylan.Pano.Core;

/// <summary>
/// v34 Build Quality / Stability tarama sonuç seviyesi.
/// </summary>
public enum PanoQualitySeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Pano public API ve runtime davranışlarını hızlı kontrol etmek için hafif kalite maddesi.
/// </summary>
public sealed class PanoQualityCheckItem
{
    public PanoQualitySeverity Severity { get; set; } = PanoQualitySeverity.Info;
    public string Area { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Severity} | {Area} | {Code} | {Message}";
    }
}

public sealed class PanoQualityReport
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<PanoQualityCheckItem> Items { get; } = new();
    public int ErrorCount => Items.Count(x => x.Severity == PanoQualitySeverity.Error);
    public int WarningCount => Items.Count(x => x.Severity == PanoQualitySeverity.Warning);
    public bool IsClean => ErrorCount == 0;

    public void Add(PanoQualitySeverity severity, string area, string code, string message, string suggestion = "")
    {
        Items.Add(new PanoQualityCheckItem
        {
            Severity = severity,
            Area = area,
            Code = code,
            Message = message,
            Suggestion = suggestion
        });
    }
}
