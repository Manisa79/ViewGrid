using System.Reflection;

namespace Taylan.Pano.Core;

/// <summary>
/// Product/version helper for host apps, test apps and diagnostics screens.
/// </summary>
public static class PanoVersionInfo
{
    public const string ProjectDisplayName = "Pano";
    public const string ControlName = "PanoControl";
    public const string PackageId = "Pano.WinForms";
    public const string SuggestedProjectName = ProjectDisplayName;
    public const string Version = "1.0.52.2";
    public const string BuildName = "1.0.52.2-community-preview-media-scenarios-compile-cleanup";

    public static string AssemblyVersion
        => typeof(PanoVersionInfo).Assembly.GetName().Version?.ToString() ?? Version;

    public static string InformationalVersion
        => typeof(PanoVersionInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? BuildName;

    public static string DisplayText => $"{ProjectDisplayName} {InformationalVersion}";
}
