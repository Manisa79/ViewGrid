using System.Text;

namespace ViewGrid.Columns;

internal static class ViewGridColumnNameHelper
{
    public const string DefaultPrefix = "glv";

    public static string CreateDefaultName(int ordinal)
        => DefaultPrefix + "Column" + Math.Max(1, ordinal).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static string CreateNameFromAspectOrText(string? aspectName, string? text, int ordinal = 1)
    {
        var source = !string.IsNullOrWhiteSpace(aspectName) ? aspectName : text;

        // Collection editor yeni kolonları varsayılan başlıkla (Column) oluşturur.
        // Bu durumda kolon adları glvColumn1, glvColumn2... şeklinde tutarlı üretilir.
        if (IsDefaultColumnCaption(source))
            return CreateDefaultName(ordinal);

        var identifier = ToIdentifierSuffix(source);
        return string.IsNullOrWhiteSpace(identifier)
            ? CreateDefaultName(ordinal)
            : DefaultPrefix + identifier;
    }

    public static bool IsDefaultColumnCaption(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;

        var text = value.Trim();
        return string.Equals(text, "Column", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(text, "ViewGridColumn", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(text, "GLVColumn", StringComparison.OrdinalIgnoreCase);
    }

    public static string EnsureUnique(string baseName, IEnumerable<ViewGridColumn> existing)
    {
        if (string.IsNullOrWhiteSpace(baseName)) baseName = CreateDefaultName(1);
        var used = new HashSet<string>(existing.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseName)) return baseName;

        int i = 2;
        while (used.Contains(baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture))) i++;
        return baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string GetDesignerDisplayText(ViewGridColumn? column)
    {
        if (column == null) return string.Empty;

        string name = FirstNonEmpty(column.Name, column.Header, column.AspectName, CreateDefaultName(1));
        string aspect = column.AspectName?.Trim() ?? string.Empty;

        // Designer listesinde stabil kimlik her zaman Name'dir.
        // Header/caption burada gösterilmez; header değişimi kolon kimliği gibi davranmamalıdır.
        string text = !string.IsNullOrWhiteSpace(aspect) &&
            !string.Equals(name, aspect, StringComparison.OrdinalIgnoreCase)
            ? name + " (" + aspect + ")"
            : name;

        if (column.PrivateColumn)
            text += " [Private]";

        return text;
    }

    public static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return string.Empty;
    }

    public static bool LooksLikeGeneratedName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var n = name.Trim();
        return n.StartsWith(DefaultPrefix + "Column", StringComparison.OrdinalIgnoreCase) ||
               n.StartsWith("colColumn", StringComparison.OrdinalIgnoreCase) ||
               n.StartsWith("viewgridColumn", StringComparison.OrdinalIgnoreCase) ||
               n.StartsWith("column", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetDefaultOrdinal(string? name, out int ordinal)
    {
        ordinal = 0;
        if (string.IsNullOrWhiteSpace(name)) return false;

        var n = name.Trim();
        var prefix = DefaultPrefix + "Column";
        if (!n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var suffix = n.Substring(prefix.Length);
        return int.TryParse(suffix, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ordinal)
            && ordinal > 0;
    }

    public static int GetNextDefaultOrdinal(IEnumerable<ViewGridColumn> columns)
    {
        int max = 0;
        foreach (var column in columns)
        {
            if (TryGetDefaultOrdinal(column?.Name, out var value))
                max = Math.Max(max, value);
        }

        return max + 1;
    }

    public static string ToIdentifierSuffix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var sb = new StringBuilder();
        bool upperNext = true;
        foreach (char original in value.Trim())
        {
            foreach (char ch in NormalizeIdentifierChar(original))
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    if (sb.Length == 0 && char.IsDigit(ch)) sb.Append('_');
                    sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
                    upperNext = false;
                }
                else
                {
                    upperNext = true;
                }
            }
        }

        var result = sb.ToString().Trim('_');
        return result;
    }

    private static string NormalizeIdentifierChar(char ch)
    {
        return ch switch
        {
            'ç' or 'Ç' => "c",
            'ğ' or 'Ğ' => "g",
            'ı' or 'I' or 'İ' or 'i' => "i",
            'ö' or 'Ö' => "o",
            'ş' or 'Ş' => "s",
            'ü' or 'Ü' => "u",
            _ => ch.ToString()
        };
    }
}
