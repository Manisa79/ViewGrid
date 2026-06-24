using System.Collections;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Taylan.Pano.Intelligence;

public sealed class PanoExpressionEngine
{
    public object? Evaluate(object row, string expression)
    {
        if (row == null || string.IsNullOrWhiteSpace(expression)) return null;
        string expr = expression.Trim();

        if (TryEvaluateFunction(row, expr, out object? functionValue)) return functionValue;
        if (TryEvaluateTernary(row, expr, out object? ternaryValue)) return ternaryValue;
        if (TryEvaluateCondition(row, expr, out bool conditionValue)) return conditionValue;

        string computeExpression = Regex.Replace(expr, @"\b[A-Za-z_][A-Za-z0-9_]*\b", m => FormatForCompute(GetValue(row, m.Value)));
        try { return new DataTable().Compute(computeExpression, string.Empty); }
        catch { return ReplaceTokensAsText(row, expr); }
    }

    public bool EvaluateCondition(object row, string condition)
    {
        if (row == null || string.IsNullOrWhiteSpace(condition)) return false;
        if (TryEvaluateCondition(row, condition, out bool result)) return result;
        object? value = Evaluate(row, condition);
        if (value is bool b) return b;
        if (value == null || value == DBNull.Value) return false;
        string text = Convert.ToString(value) ?? string.Empty;
        if (bool.TryParse(text, out bool parsedBool)) return parsedBool;
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double number)) return Math.Abs(number) > double.Epsilon;
        return !string.IsNullOrWhiteSpace(text);
    }

    private bool TryEvaluateFunction(object row, string expr, out object? value)
    {
        value = null;
        Match m = Regex.Match(expr, @"^(?<fn>[A-Za-z_][A-Za-z0-9_]*)\((?<args>.*)\)$", RegexOptions.Singleline);
        if (!m.Success) return false;
        string fn = m.Groups["fn"].Value.ToUpperInvariant();
        List<string> args = SplitArguments(m.Groups["args"].Value);

        switch (fn)
        {
            case "IF":
                if (args.Count >= 3)
                {
                    bool ok = EvaluateCondition(row, args[0]);
                    value = EvaluateLoose(row, ok ? args[1] : args[2]);
                    return true;
                }
                break;
            case "CONCAT":
                value = string.Concat(args.Select(a => Convert.ToString(EvaluateLoose(row, a)) ?? string.Empty));
                return true;
            case "UPPER":
                value = Convert.ToString(EvaluateLoose(row, args.FirstOrDefault() ?? string.Empty))?.ToUpperInvariant() ?? string.Empty;
                return true;
            case "LOWER":
                value = Convert.ToString(EvaluateLoose(row, args.FirstOrDefault() ?? string.Empty))?.ToLowerInvariant() ?? string.Empty;
                return true;
            case "LEN":
                value = (Convert.ToString(EvaluateLoose(row, args.FirstOrDefault() ?? string.Empty)) ?? string.Empty).Length;
                return true;
            case "DATE":
                value = DateTime.TryParse(Convert.ToString(EvaluateLoose(row, args.FirstOrDefault() ?? string.Empty)), CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : null;
                return true;
            case "ISBLANK":
                value = string.IsNullOrWhiteSpace(Convert.ToString(EvaluateLoose(row, args.FirstOrDefault() ?? string.Empty)));
                return true;
            case "COALESCE":
                foreach (string arg in args)
                {
                    object? v = EvaluateLoose(row, arg);
                    if (v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(Convert.ToString(v))) { value = v; return true; }
                }
                value = string.Empty;
                return true;
        }
        return false;
    }

    private object? EvaluateLoose(object row, string token)
    {
        string t = token.Trim();
        if ((t.StartsWith("'", StringComparison.Ordinal) && t.EndsWith("'", StringComparison.Ordinal)) ||
            (t.StartsWith("\"", StringComparison.Ordinal) && t.EndsWith("\"", StringComparison.Ordinal)))
            return t.Substring(1, t.Length - 2);
        object? direct = GetValue(row, t);
        if (direct != null) return direct;
        return Evaluate(row, t);
    }

    private bool TryEvaluateTernary(object row, string expr, out object? value)
    {
        value = null;
        int q = IndexOfTopLevel(expr, '?');
        if (q <= 0) return false;
        int c = IndexOfTopLevel(expr, ':', q + 1);
        if (c <= q) return false;
        string condition = expr.Substring(0, q).Trim();
        string yes = expr.Substring(q + 1, c - q - 1).Trim();
        string no = expr.Substring(c + 1).Trim();
        value = EvaluateCondition(row, condition) ? EvaluateLoose(row, yes) : EvaluateLoose(row, no);
        return true;
    }

    private bool TryEvaluateCondition(object row, string condition, out bool result)
    {
        result = false;
        string[] logical = { " OR ", " || ", " AND ", " && " };
        foreach (string op in logical)
        {
            int idx = condition.IndexOf(op, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                bool left = EvaluateCondition(row, condition.Substring(0, idx));
                bool right = EvaluateCondition(row, condition.Substring(idx + op.Length));
                result = op.Trim().Equals("OR", StringComparison.OrdinalIgnoreCase) || op.Trim() == "||" ? left || right : left && right;
                return true;
            }
        }

        string[] operators = { ">=", "<=", "!=", "==", "=", "~", ">", "<" };
        foreach (string op in operators)
        {
            int index = condition.IndexOf(op, StringComparison.Ordinal);
            if (index <= 0) continue;
            string leftName = condition.Substring(0, index).Trim();
            string rightText = condition.Substring(index + op.Length).Trim().Trim('\'', '"');
            object? leftValue = GetValue(row, leftName);
            string leftText = Convert.ToString(leftValue) ?? string.Empty;
            if (op == "~") { result = leftText.IndexOf(rightText, StringComparison.CurrentCultureIgnoreCase) >= 0; return true; }
            if (double.TryParse(leftText, NumberStyles.Any, CultureInfo.CurrentCulture, out double l) && double.TryParse(rightText, NumberStyles.Any, CultureInfo.CurrentCulture, out double r))
            {
                result = op switch { ">=" => l >= r, "<=" => l <= r, "!=" => Math.Abs(l - r) > double.Epsilon, "==" or "=" => Math.Abs(l - r) <= double.Epsilon, ">" => l > r, "<" => l < r, _ => false };
                return true;
            }
            int cmp = string.Compare(leftText, rightText, StringComparison.CurrentCultureIgnoreCase);
            result = op switch { "!=" => cmp != 0, "==" or "=" => cmp == 0, ">" => cmp > 0, "<" => cmp < 0, ">=" => cmp >= 0, "<=" => cmp <= 0, _ => false };
            return true;
        }
        return false;
    }

    private static List<string> SplitArguments(string text)
    {
        List<string> result = new();
        if (string.IsNullOrEmpty(text)) return result;
        StringBuilder current = new();
        int depth = 0;
        bool inQuote = false;
        char quote = '\0';
        foreach (char ch in text)
        {
            if ((ch == '\'' || ch == '"') && (quote == '\0' || quote == ch))
            {
                inQuote = !inQuote;
                quote = inQuote ? ch : '\0';
                current.Append(ch);
                continue;
            }
            if (!inQuote)
            {
                if (ch == '(') depth++;
                if (ch == ')') depth--;
                if (ch == ',' && depth == 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }
            }
            current.Append(ch);
        }
        result.Add(current.ToString().Trim());
        return result;
    }

    private static int IndexOfTopLevel(string text, char target, int start = 0)
    {
        int depth = 0;
        bool inQuote = false;
        char quote = '\0';
        for (int i = start; i < text.Length; i++)
        {
            char ch = text[i];
            if ((ch == '\'' || ch == '"') && (quote == '\0' || quote == ch)) { inQuote = !inQuote; quote = inQuote ? ch : '\0'; continue; }
            if (inQuote) continue;
            if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if (ch == target && depth == 0) return i;
        }
        return -1;
    }

    private static string FormatForCompute(object? value)
    {
        if (value == null || value == DBNull.Value) return "0";
        if (value is bool b) return b ? "1" : "0";
        if (value is IFormattable f && value is not string) return f.ToString(null, CultureInfo.InvariantCulture) ?? "0";
        if (double.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out double d)) return d.ToString(CultureInfo.InvariantCulture);
        return "0";
    }

    private static string ReplaceTokensAsText(object row, string expr)
        => Regex.Replace(expr, @"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", m => Convert.ToString(GetValue(row, m.Groups["name"].Value)) ?? string.Empty);

    public static object? GetValue(object row, string name)
    {
        if (row == null || string.IsNullOrWhiteSpace(name)) return null;
        if ((name.StartsWith("'", StringComparison.Ordinal) && name.EndsWith("'", StringComparison.Ordinal)) ||
            (name.StartsWith("\"", StringComparison.Ordinal) && name.EndsWith("\"", StringComparison.Ordinal)))
            return name.Substring(1, name.Length - 2);
        if (row is DataRow dr && dr.Table.Columns.Contains(name)) return dr[name];
        if (row is IDictionary<string, object?> gd && gd.TryGetValue(name, out object? gv)) return gv;
        if (row is IDictionary dict && dict.Contains(name)) return dict[name];
        Type type = row.GetType();
        PropertyInfo? prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(row);
        FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return field?.GetValue(row);
    }
}
