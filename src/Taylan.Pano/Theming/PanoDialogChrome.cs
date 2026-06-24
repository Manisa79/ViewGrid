using System.Runtime.InteropServices;

namespace Taylan.Pano.Theming;

/// <summary>
/// Standardizes Pano helper/dialog windows so popup forms, chooser windows and
/// designer tools use the same resize behavior, themed title bar and simplified
/// system menu. Native calls are best-effort and ignored on unsupported Windows versions.
/// </summary>
public enum PanoDialogIconKind
{
    None,
    Grid,
    Filter,
    Column,
    Search,
    Export,
    Designer,
    Command,
    Info,
    Warning,
    Success,
    Error
}

public static class PanoDialogChrome
{
    private const int SC_RESTORE = 0xF120;
    private const int SC_MOVE = 0xF010;
    private const int SC_SIZE = 0xF000;
    private const int SC_MINIMIZE = 0xF020;
    private const int SC_MAXIMIZE = 0xF030;
    private const int MF_BYCOMMAND = 0x00000000;

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool DeleteMenu(IntPtr hMenu, int uPosition, int uFlags);

    [DllImport("user32.dll")]
    private static extern bool DrawMenuBar(IntPtr hWnd);

    /// <summary>
    /// Compatibility overload: older sample/project code sometimes passed dialog width and height
    /// as integers. The main API now uses Size, but this overload keeps those calls compiling.
    /// </summary>
    public static void ConfigureStandardDialog(Form form, PanoTheme theme, int minimumWidth, int minimumHeight, bool sizeable = true, PanoDialogIconKind iconKind = PanoDialogIconKind.Grid)
        => ConfigureStandardDialog(form, theme, new Size(Math.Max(1, minimumWidth), Math.Max(1, minimumHeight)), sizeable, iconKind);

    /// <summary>
    /// Compatibility overload: when a single integer is supplied, use it as a square minimum size.
    /// </summary>
    public static void ConfigureStandardDialog(Form form, PanoTheme theme, int minimumSize, bool sizeable = true, PanoDialogIconKind iconKind = PanoDialogIconKind.Grid)
        => ConfigureStandardDialog(form, theme, new Size(Math.Max(1, minimumSize), Math.Max(1, minimumSize)), sizeable, iconKind);

    public static void ConfigureStandardDialog(Form form, PanoTheme theme, Size minimumSize, bool sizeable = true, PanoDialogIconKind iconKind = PanoDialogIconKind.Grid)
    {
        if (form is null) return;

        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.FormBorderStyle = sizeable ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
        form.SizeGripStyle = sizeable ? SizeGripStyle.Show : SizeGripStyle.Hide;
        form.MinimumSize = minimumSize;
        form.MinimizeBox = false;
        form.MaximizeBox = false;
        form.ShowIcon = iconKind != PanoDialogIconKind.None;
        form.ShowInTaskbar = false;
        ApplyDialogIcon(form, theme, iconKind);
        form.StartPosition = form.StartPosition == FormStartPosition.WindowsDefaultLocation
            ? FormStartPosition.CenterParent
            : form.StartPosition;
        form.KeyPreview = true;

        PanoWindowChrome.ApplyOnHandleCreated(form, () => theme, preferGlassWhenAvailable: true);
        ApplySystemMenuOnHandleCreated(form);
        form.HandleCreated += (_, _) => ApplyDialogIcon(form, theme, iconKind);
        form.Shown += (_, _) => ApplyDialogIcon(form, theme, iconKind);
    }

    public static void ApplyDialogIcon(Form form, PanoTheme theme, PanoDialogIconKind iconKind)
    {
        if (form is null || form.IsDisposed || iconKind == PanoDialogIconKind.None) return;

        try
        {
            // Do not dispose the previous Form.Icon here. WinForms can hand out a shared/default
            // icon instance and disposing it may later break Form.CreateHandle with
            // ObjectDisposedException: Cannot access a disposed object. Object name: 'Icon'.
            // The new icon is owned by the form and will be disposed with the form.
            form.Icon = PanoDialogIconFactory.Create(iconKind, theme);
            form.ShowIcon = true;
        }
        catch
        {
            // Icons are cosmetic. If GDI+ cannot create an icon, keep the dialog usable.
        }
    }

    public static void ApplySystemMenuOnHandleCreated(Form form)
    {
        if (form is null) return;

        void Apply()
        {
            try
            {
                SimplifySystemMenu(form);
            }
            catch
            {
                // System menu cleanup is cosmetic. Never fail because of native menu support.
            }
        }

        if (form.IsHandleCreated) Apply();
        form.HandleCreated += (_, _) => Apply();
        form.Shown += (_, _) => Apply();
    }

    public static void SimplifySystemMenu(Form form)
    {
        if (form.IsDisposed || !form.IsHandleCreated) return;

        IntPtr menu = GetSystemMenu(form.Handle, false);
        if (menu == IntPtr.Zero) return;

        if (!form.MinimizeBox)
            DeleteMenu(menu, SC_MINIMIZE, MF_BYCOMMAND);

        if (!form.MaximizeBox)
            DeleteMenu(menu, SC_MAXIMIZE, MF_BYCOMMAND);

        if (!form.MinimizeBox && !form.MaximizeBox)
            DeleteMenu(menu, SC_RESTORE, MF_BYCOMMAND);

        if (form.FormBorderStyle is FormBorderStyle.FixedDialog or FormBorderStyle.FixedSingle or FormBorderStyle.FixedToolWindow)
            DeleteMenu(menu, SC_SIZE, MF_BYCOMMAND);

        if (form.FormBorderStyle == FormBorderStyle.None)
            DeleteMenu(menu, SC_MOVE, MF_BYCOMMAND);

        DrawMenuBar(form.Handle);
    }

    public static void ConfigureOpenPanoDialogs(Control root, PanoTheme theme)
    {
        if (root is null) return;

        Form? form = root.FindForm();
        if (form != null)
        {
            PanoWindowChrome.ApplyOnHandleCreated(form, () => theme, preferGlassWhenAvailable: true);
            ApplySystemMenuOnHandleCreated(form);
        }

        PanoDialogThemeApplier.Apply(root, theme);
    }
}
