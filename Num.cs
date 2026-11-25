using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IsomerizationCalc;

/// <summary>Parsing and formatting of the numbers typed by the user (port of Num.java).</summary>
static class Num
{
    static readonly Regex PlainNumber = new(@"^-?([0-9]+\.?[0-9]*|\.[0-9]+)$", RegexOptions.CultureInvariant);

    /// <summary>True when the field is empty or contains only spaces.</summary>
    public static bool IsBlank(string? s) => s == null || s.Trim().Length == 0;

    /// <summary>
    /// Turns what the keyboard produced into a plain ASCII number: Arabic-Indic digits become 0-9,
    /// the Arabic decimal mark or a comma becomes a dot, spaces and the Arabic thousands mark are dropped.
    /// </summary>
    public static string Normalize(string? s)
    {
        if (s == null) return "";
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
            else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
            else if (c == '٫' || c == ',') sb.Append('.');
            else if (c == '٬' || char.IsWhiteSpace(c)) { /* dropped */ }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Value of the text, or 0 when it is empty or not a valid number.</summary>
    public static double Parse(string? s)
    {
        string t = Normalize(s);
        if (t.Length == 0 || !PlainNumber.IsMatch(t)) return 0;
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
    }

    /// <summary>Fixed number of decimals with a dot separator.</summary>
    public static string Fixed(double value, int decimals) =>
        value.ToString("F" + decimals, CultureInfo.InvariantCulture);
}
