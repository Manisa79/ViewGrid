using Taylan.Pano.Theming;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    [Category("Pano - V35 Theme Studio")]
    [DefaultValue(false)]
    [Description("Theme Studio preset ve canlı önizleme akışını aktif eder.")]
    public bool EnableThemeStudio { get; set; }

    [Category("Pano - V35 Theme Studio")]
    [DefaultValue(PanoThemeStudioPreset.FactoryOsDark)]
    [Description("Pano Theme Studio içinde seçili olan hazır tema paleti.")]
    public PanoThemeStudioPreset ThemeStudioPreset { get; set; } = PanoThemeStudioPreset.FactoryOsDark;

    [Category("Pano - V35 Theme Studio")]
    [DefaultValue(true)]
    [Description("Theme Studio preset uygulanırken v33 erişilebilirlik normalizasyonunu zorunlu tutar.")]
    public bool ThemeStudioEnforceAccessibility { get; set; } = true;

    public void ApplyThemeStudioPreset(PanoThemeStudioPreset preset)
    {
        EnableThemeStudio = true;
        ThemeStudioPreset = preset;
        var theme = PanoThemeStudio.Create(preset);
        if (ThemeStudioEnforceAccessibility)
            theme = PanoThemeAccessibility.Normalize(theme);
        ApplyTheme(theme);
    }

    public IReadOnlyList<PanoThemeStudioPalette> GetThemeStudioPalettes()
    {
        return PanoThemeStudio.BuiltInPalettes();
    }

    public string ExportCurrentThemeStudioPalette(string name = "Current")
    {
        return PanoThemeStudio.ExportPaletteText(_theme, name);
    }

    public void ApplyV35ThemeStudioPack(PanoThemeStudioPreset preset = PanoThemeStudioPreset.FactoryOsDark)
    {
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        AutoEnsureReadableTextColors = true;
        ApplyThemeStudioPreset(preset);
    }
}
