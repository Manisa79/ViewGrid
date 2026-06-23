using System.ComponentModel;
using System.Text.Json;
using ViewGrid.Layout;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - Profiles")]
    [DefaultValue(true)]
    [Description("v29 profil sistemi. Tek kaynak ViewGridLayoutProfile kullanılır; eski kolon profil JSON dosyaları otomatik migrate edilebilir.")]
    public bool EnableV29LayoutProfiles { get; set; } = true;

    public string GetLayoutProfileFilePath(string profileName, string? roleName = null, string? userName = null, string? machineName = null)
    {
        string safeProfile = MakeSafeFileName(string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim());
        string key = string.IsNullOrWhiteSpace(ColumnLayoutStorageKey) ? Name : ColumnLayoutStorageKey!;
        if (string.IsNullOrWhiteSpace(key)) key = GetType().Name;
        key = MakeSafeFileName(key);

        string scope = "default";
        if (!string.IsNullOrWhiteSpace(roleName)) scope = "role-" + MakeSafeFileName(roleName);
        else if (!string.IsNullOrWhiteSpace(userName)) scope = "user-" + MakeSafeFileName(userName);
        else if (!string.IsNullOrWhiteSpace(machineName)) scope = "machine-" + MakeSafeFileName(machineName);

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViewGridControl", "Profiles", key, scope);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, safeProfile + ".viewgridprofile");
    }

    public void SaveLayoutProfile(string profileName, string? roleName = null, string? userName = null, string? machineName = null)
    {
        if (!EnableV29LayoutProfiles) return;
        ViewGridLayoutProfile profile = CaptureLayoutProfile(profileName);
        profile.RoleName = roleName;
        profile.UserName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
        profile.MachineName = string.IsNullOrWhiteSpace(machineName) ? Environment.MachineName : machineName;
        profile.ProfileVersion = 29;
        profile.UpdatedAt = DateTime.Now;
        string file = GetLayoutProfileFilePath(profileName, roleName, userName, machineName);
        File.WriteAllText(file, profile.ToJson());
    }

    public bool LoadLayoutProfile(string profileName, string? roleName = null, string? userName = null, string? machineName = null)
    {
        if (!EnableV29LayoutProfiles) return false;
        string file = GetLayoutProfileFilePath(profileName, roleName, userName, machineName);
        if (!File.Exists(file))
        {
            string legacy = GetColumnLayoutProfileFilePath(profileName);
            if (!File.Exists(legacy)) return false;
            ViewGridLayoutProfile migrated = ViewGridProfileMigrator.FromLegacyColumnProfileJson(File.ReadAllText(legacy), profileName);
            ApplyLayoutProfile(migrated);
            return true;
        }

        ViewGridLayoutProfile profile = ViewGridLayoutProfile.FromJson(File.ReadAllText(file));
        ApplyLayoutProfile(profile);
        return true;
    }

    public bool DeleteLayoutProfile(string profileName, string? roleName = null, string? userName = null, string? machineName = null)
    {
        string file = GetLayoutProfileFilePath(profileName, roleName, userName, machineName);
        if (!File.Exists(file)) return false;
        File.Delete(file);
        return true;
    }

    public IReadOnlyList<string> GetLayoutProfileNames(string? roleName = null, string? userName = null, string? machineName = null)
    {
        string dir = Path.GetDirectoryName(GetLayoutProfileFilePath("Default", roleName, userName, machineName)) ?? string.Empty;
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        return Directory.GetFiles(dir, "*.viewgridprofile")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .OrderBy(x => x)
            .ToList();
    }

    public void ExportLayoutProfile(string filePath, string profileName = "Default", string? roleName = null, string? userName = null, string? machineName = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        ViewGridLayoutProfile profile = CaptureLayoutProfile(profileName);
        profile.RoleName = roleName;
        profile.UserName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
        profile.MachineName = string.IsNullOrWhiteSpace(machineName) ? Environment.MachineName : machineName;
        profile.ProfileVersion = 29;
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, profile.ToJson());
    }

    public ViewGridLayoutProfile ImportLayoutProfile(string filePath, bool apply = true)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        ViewGridLayoutProfile profile = ViewGridLayoutProfile.FromJson(File.ReadAllText(filePath));
        if (apply) ApplyLayoutProfile(profile);
        return profile;
    }

    public int MigrateLegacyProfiles(bool deleteLegacyAfterMigration = false)
    {
        int migratedCount = 0;
        foreach (string name in GetColumnLayoutProfileNames())
        {
            string legacy = GetColumnLayoutProfileFilePath(name);
            if (!File.Exists(legacy)) continue;
            try
            {
                ViewGridLayoutProfile profile = ViewGridProfileMigrator.FromLegacyColumnProfileJson(File.ReadAllText(legacy), name);
                string target = GetLayoutProfileFilePath(name);
                File.WriteAllText(target, profile.ToJson());
                if (deleteLegacyAfterMigration) File.Delete(legacy);
                migratedCount++;
            }
            catch
            {
                // Migration must be best-effort. A corrupt profile must not break the control.
            }
        }
        return migratedCount;
    }
}
