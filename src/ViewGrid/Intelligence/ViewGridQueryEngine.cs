using System.Globalization;
using System.Text.RegularExpressions;

namespace ViewGrid.Intelligence;

public sealed class ViewGridQueryEngine
{
    public Func<object, bool> Compile(string query)
    {
        string source = query?.Trim() ?? string.Empty;
        if (source.Length == 0) return _ => true;
        return row => EvaluateOr(row, source);
    }

    public bool Matches(object row, string query) => Compile(query)(row);

    private bool EvaluateOr(object row, string text)
    {
        string[] parts = Regex.Split(text, @"\s+OR\s+", RegexOptions.IgnoreCase);
        if (parts.Length > 1) return parts.Any(p => EvaluateAnd(row, p));
        return EvaluateAnd(row, text);
    }

    private bool EvaluateAnd(object row, string text)
    {
        string[] parts = Regex.Split(text, @"\s+AND\s+", RegexOptions.IgnoreCase);
        if (parts.Length > 1) return parts.All(p => EvaluateTerm(row, p));
        return EvaluateTerm(row, text);
    }

    private bool EvaluateTerm(object row, string raw)
    {
        string term = raw.Trim();
        if (term.Length == 0) return true;
        if (term.StartsWith("(", StringComparison.Ordinal) && term.EndsWith(")", StringComparison.Ordinal)) return EvaluateOr(row, term.Substring(1, term.Length - 2));
        if (term.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase)) return !EvaluateTerm(row, term.Substring(4));
        if (term.StartsWith("!", StringComparison.Ordinal)) return !EvaluateTerm(row, term.Substring(1));

        Match colon = Regex.Match(term, @"^(?<name>[A-Za-z_][A-Za-z0-9_\. ]*)\s*:\s*(?<value>.+)$");
        if (colon.Success)
        {
            object? left = ViewGridExpressionEngine.GetValue(row, colon.Groups["name"].Value.Trim());
            string right = Unquote(colon.Groups["value"].Value.Trim());
            return Convert.ToString(left)?.IndexOf(right, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        string[] operators = { ">=", "<=", "!=", "==", "=", "~", ">", "<" };
        foreach (string op in operators)
        {
            int idx = term.IndexOf(op, StringComparison.Ordinal);
            if (idx <= 0) continue;
            string name = term.Substring(0, idx).Trim();
            string rightText = Unquote(term.Substring(idx + op.Length).Trim());
            object? left = ViewGridExpressionEngine.GetValue(row, name);
            return Compare(left, rightText, op);
        }

        return RowContains(row, term);
    }

    private static bool Compare(object? left, string rightText, string op)
    {
        string leftText = Convert.ToString(left) ?? string.Empty;
        if (op == "~") return leftText.IndexOf(rightText, StringComparison.CurrentCultureIgnoreCase) >= 0;
        if (double.TryParse(leftText, NumberStyles.Any, CultureInfo.CurrentCulture, out double l) && double.TryParse(rightText, NumberStyles.Any, CultureInfo.CurrentCulture, out double r))
        {
            return op switch { ">=" => l >= r, "<=" => l <= r, "!=" => Math.Abs(l - r) > double.Epsilon, "==" or "=" => Math.Abs(l - r) <= double.Epsilon, ">" => l > r, "<" => l < r, _ => false };
        }
        if (DateTime.TryParse(leftText, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime ld) && DateTime.TryParse(rightText, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime rd))
        {
            int dateCmp = ld.CompareTo(rd);
            return op switch { ">=" => dateCmp >= 0, "<=" => dateCmp <= 0, "!=" => dateCmp != 0, "==" or "=" => dateCmp == 0, ">" => dateCmp > 0, "<" => dateCmp < 0, _ => false };
        }
        int cmp = string.Compare(leftText, rightText, StringComparison.CurrentCultureIgnoreCase);
        return op switch { "!=" => cmp != 0, "==" or "=" => cmp == 0, ">" => cmp > 0, "<" => cmp < 0, ">=" => cmp >= 0, "<=" => cmp <= 0, _ => false };
    }

    private static bool RowContains(object row, string text)
    {
        if (row == null) return false;
        string needle = Unquote(text);
        if (string.IsNullOrWhiteSpace(needle)) return true;
        return Convert.ToString(row)?.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
            || row.GetType().GetProperties().Any(p => Convert.ToString(SafeGet(row, p))?.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
    }

    private static object? SafeGet(object row, System.Reflection.PropertyInfo prop)
    {
        try { return prop.GetValue(row); } catch { return null; }
    }

    private static string Unquote(string text)
    {
        string t = text.Trim();
        if ((t.StartsWith("'", StringComparison.Ordinal) && t.EndsWith("'", StringComparison.Ordinal)) ||
            (t.StartsWith("\"", StringComparison.Ordinal) && t.EndsWith("\"", StringComparison.Ordinal)))
            return t.Substring(1, t.Length - 2);
        return t;
    }
}
