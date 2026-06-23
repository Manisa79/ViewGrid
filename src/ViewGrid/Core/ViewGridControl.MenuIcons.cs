using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ViewGrid.Core;

public enum ViewGridMenuIconMode
{
    None = 0,
    BuiltIn = 1,
    Custom = 2,
    BuiltInThenCustom = 3
}

public enum ViewGridMenuIconSize
{
    Small16 = 16,
    Medium20 = 20,
    Large24 = 24
}

public partial class ViewGridControl
{
    private readonly Dictionary<string, Image> _menuIconCache = new(StringComparer.OrdinalIgnoreCase);

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(ViewGridMenuIconMode.BuiltIn)]
    [Description("Popup/context menülerde ikon gösterimini belirler. None: ikon yok, BuiltIn: ViewGrid ikonları, Custom: kullanıcı ikonları, BuiltInThenCustom: özel ikon varsa onu, yoksa yerleşik ikon.")]
    public ViewGridMenuIconMode MenuIconMode { get; set; } = ViewGridMenuIconMode.BuiltIn;

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(ViewGridMenuIconSize.Small16)]
    public ViewGridMenuIconSize MenuIconSize { get; set; } = ViewGridMenuIconSize.Small16;

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(true)]
    [Description("Kullanıcı layout profilinde menü ikon tercihini otomatik saklar.")]
    public bool SaveMenuIconPreferencesInUserLayout { get; set; } = true;

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue("")]
    [Description("Kullanıcı özel menü ikonlarının bulunduğu klasör. Dosya adları action key ile eşleşirse otomatik kullanılır: filter.png, sort_asc.png, columns.png gibi.")]
    public string CustomMenuIconFolder { get; set; } = string.Empty;

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(true)]
    [Description("Kullanıcı ContextMenuStrip verdiğinde ViewGrid otomatik eklenen alt menüye ve desteklenen öğelere ikon uygular.")]
    public bool ApplyIconsToMergedUserMenus { get; set; } = true;

    public void ClearMenuIconCache()
    {
        foreach (var img in _menuIconCache.Values)
        {
            try { img.Dispose(); } catch { }
        }
        _menuIconCache.Clear();
    }

    public void SetCustomMenuIconFolder(string folder, bool saveLayout = true)
    {
        CustomMenuIconFolder = folder ?? string.Empty;
        ClearMenuIconCache();
        if (saveLayout) QueueAutoSaveUserLayout();
    }

    internal void ApplyMenuIcons(ToolStripItemCollection items)
    {
        if (items == null || MenuIconMode == ViewGridMenuIconMode.None) return;
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripSeparator) continue;
            string key = GetMenuActionKey(item.Text);
            if (item is ToolStripMenuItem mi)
            {
                if (mi.Image == null)
                    mi.Image = ResolveMenuIcon(key);
                if (mi.DropDownItems.Count > 0)
                    ApplyMenuIcons(mi.DropDownItems);
            }
        }
    }

    private Image? ResolveMenuIcon(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (MenuIconMode is ViewGridMenuIconMode.Custom or ViewGridMenuIconMode.BuiltInThenCustom)
        {
            var resolved = TryResolveCustomMenuIcon(key);
            if (resolved != null) return resolved;
            var customFromImageList = TryGetCustomMenuIconFromImageList(key);
            if (customFromImageList != null) return customFromImageList;
            var custom = TryLoadCustomMenuIcon(key);
            if (custom != null) return custom;
            if (MenuIconMode == ViewGridMenuIconMode.Custom) return null;
        }

        return CreateBuiltInMenuIcon(key, (int)MenuIconSize);
    }

    private Image? TryResolveCustomMenuIcon(string key)
    {
        try
        {
            foreach (string candidate in GetMenuIconCandidateKeys(key))
            {
                var image = CustomMenuIconResolver?.Invoke(candidate);
                if (image != null) return new Bitmap(image, new Size((int)MenuIconSize, (int)MenuIconSize));
            }
        }
        catch { }
        return null;
    }

    private Image? TryGetCustomMenuIconFromImageList(string key)
    {
        ImageList? themed = null;
        bool dark = MenuIconThemeMode == ViewGridMenuIconTheme.Dark || (MenuIconThemeMode == ViewGridMenuIconTheme.Auto && _theme.IsDark);
        bool light = MenuIconThemeMode == ViewGridMenuIconTheme.Light || (MenuIconThemeMode == ViewGridMenuIconTheme.Auto && !_theme.IsDark);
        if (dark) themed = CustomMenuDarkImageList;
        if (light) themed = CustomMenuLightImageList;
        ImageList? list = themed ?? CustomMenuImageList;
        if (list == null || list.Images.Count == 0) return null;
        foreach (string candidate in GetMenuIconCandidateKeys(key))
        {
            int index = list.Images.IndexOfKey(candidate);
            if (index >= 0) return new Bitmap(list.Images[index], new Size((int)MenuIconSize, (int)MenuIconSize));
        }
        return null;
    }

    private static IEnumerable<string> GetMenuIconCandidateKeys(string key)
    {
        yield return key;
        yield return key.Replace(' ', '_').ToLowerInvariant();
        yield return key.Replace('_', '-').ToLowerInvariant();
        yield return key.ToLowerInvariant() + ".png";
        yield return key.ToLowerInvariant() + ".svg";
    }

    private Image? TryLoadCustomMenuIcon(string key)
    {
        if (string.IsNullOrWhiteSpace(CustomMenuIconFolder) || !Directory.Exists(CustomMenuIconFolder)) return null;
        var safeKey = key.Replace(' ', '_').ToLowerInvariant();
        foreach (var ext in new[] { ".png", ".ico", ".jpg", ".jpeg", ".bmp", ".svg" })
        {
            var path = Path.Combine(CustomMenuIconFolder, safeKey + ext);
            if (!File.Exists(path)) continue;
            if (_menuIconCache.TryGetValue(path, out var cached)) return cached;
            try
            {
                using var src = Image.FromFile(path);
                var bmp = new Bitmap(src, new Size((int)MenuIconSize, (int)MenuIconSize));
                _menuIconCache[path] = bmp;
                return bmp;
            }
            catch { return null; }
        }
        return null;
    }

    private Image CreateBuiltInMenuIcon(string key, int size)
    {
        string cacheKey = "builtin:" + key + ":" + size + ":" + _theme.Accent.ToArgb();
        if (_menuIconCache.TryGetValue(cacheKey, out var cached)) return cached;

        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        var accent = _theme.Accent;
        var text = _theme.Text;
        using var pen = new Pen(accent, Math.Max(1.6f, size / 11f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var textPen = new Pen(text, Math.Max(1.2f, size / 13f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(accent);
        using var soft = new SolidBrush(Color.FromArgb(55, accent));
        var r = new Rectangle(2, 2, size - 4, size - 4);

        switch (key)
        {
            case "filter":
                var funnel = new[] { new Point(r.Left, r.Top + 1), new Point(r.Right, r.Top + 1), new Point(r.Left + r.Width * 2 / 3, r.Top + r.Height / 2), new Point(r.Left + r.Width * 2 / 3, r.Bottom - 1), new Point(r.Left + r.Width / 3, r.Bottom - 3), new Point(r.Left + r.Width / 3, r.Top + r.Height / 2) };
                g.FillPolygon(soft, funnel); g.DrawPolygon(pen, funnel); break;
            case "clear_filter":
                g.DrawEllipse(pen, r); g.DrawLine(textPen, r.Left + 3, r.Bottom - 3, r.Right - 3, r.Top + 3); break;
            case "sort_asc":
                g.FillPolygon(brush, new[] { new Point(size/2, 3), new Point(size-4, size-5), new Point(4, size-5) }); break;
            case "sort_desc":
                g.FillPolygon(brush, new[] { new Point(4, 4), new Point(size-4, 4), new Point(size/2, size-3) }); break;
            case "columns":
                int w = Math.Max(3, size / 5); for (int x = 3; x < size-2; x += w+2) g.FillRectangle(soft, x, 3, w, size-6); g.DrawRectangle(pen, 3, 3, size-6, size-6); break;
            case "layout":
                g.DrawRectangle(pen, 3, 3, size-6, size-6); g.DrawLine(pen, size/2, 4, size/2, size-4); g.DrawLine(pen, 4, size/2, size-4, size/2); break;
            case "theme":
                g.FillEllipse(brush, 3, 3, size-6, size-6); g.FillEllipse(new SolidBrush(_theme.Surface), size/2, 3, size/2-2, size-6); break;
            case "view":
                for (int y=4; y<size-4; y+=5) g.DrawLine(pen, 4, y, size-4, y); break;
            case "copy":
                g.DrawRectangle(textPen, 5, 3, size-7, size-8); g.DrawRectangle(pen, 3, 6, size-7, size-8); break;
            case "print":
                g.DrawRectangle(pen, 3, 6, size-6, size-6); g.DrawRectangle(textPen, 5, 2, size-10, 5); break;
            case "export":
                g.DrawLine(pen, size/2, 3, size/2, size-5); g.DrawLine(pen, size/2, size-5, size-4, size/2); g.DrawLine(pen, size/2, size-5, 4, size/2); break;
            case "group":
                g.DrawEllipse(pen, 3, 3, size/3, size/3); g.DrawEllipse(pen, size/2, 3, size/3, size/3); g.DrawEllipse(pen, size/3, size/2, size/3, size/3); break;
            case "advanced_filter":
                g.DrawPolygon(pen, new[] { new Point(3,4), new Point(size-3,4), new Point(size/2,size-5) }); g.DrawEllipse(textPen, size-8, size-8, 6, 6); break;
            case "state":
                g.DrawRectangle(pen, 3, 3, size-6, size-6); g.DrawLine(textPen, 6, size/2, size-6, size/2); break;
            case "scenario":
                g.DrawEllipse(pen, 3, 3, size-6, size-6); g.DrawLine(textPen, size/2, 4, size/2, size-4); g.DrawLine(textPen, 4, size/2, size-4, size/2); break;
            case "drag":
                for (int y=4; y<size; y+=5) for (int x=4; x<size; x+=5) g.FillEllipse(brush, x, y, 2, 2); break;
            default:
                g.FillEllipse(soft, r); g.DrawEllipse(pen, r); break;
        }
        _menuIconCache[cacheKey] = bmp;
        return bmp;
    }

    private static string GetMenuActionKey(string? text)
    {
        var t = (text ?? string.Empty).ToLowerInvariant();
        if (t.Contains("filtre") || t.Contains("filter")) return t.Contains("temiz") || t.Contains("clear") ? "clear_filter" : "filter";
        if (t.Contains("artan") || t.Contains("ascending") || t.Contains("a-z")) return "sort_asc";
        if (t.Contains("azalan") || t.Contains("descending") || t.Contains("z-a")) return "sort_desc";
        if (t.Contains("sort") || t.Contains("sırala")) return "sort_asc";
        if (t.Contains("kolon") || t.Contains("column")) return "columns";
        if (t.Contains("layout") || t.Contains("düzen") || t.Contains("profil")) return "layout";
        if (t.Contains("tema") || t.Contains("theme")) return "theme";
        if (t.Contains("görün") || t.Contains("view")) return "view";
        if (t.Contains("kopya") || t.Contains("copy")) return "copy";
        if (t.Contains("yazdır") || t.Contains("print") || t.Contains("öniz")) return "print";
        if (t.Contains("gelişmiş filtre") || t.Contains("advanced filter")) return "advanced_filter";
        if (t.Contains("state") || t.Contains("durum kaydet")) return "state";
        if (t.Contains("senaryo") || t.Contains("scenario")) return "scenario";
        if (t.Contains("export") || t.Contains("dışa aktar") || t.Contains("excel") || t.Contains("csv")) return "export";
        if (t.Contains("grup") || t.Contains("group")) return "group";
        if (t.Contains("drag") || t.Contains("sürük")) return "drag";
        return "default";
    }
}
