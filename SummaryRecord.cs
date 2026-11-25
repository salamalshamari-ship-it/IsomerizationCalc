using System.Globalization;

namespace IsomerizationCalc;

/// <summary>
/// One saved summary: the results plus everything that was typed in the calculator tabs
/// (so the record can be loaded back into its fields). Kept in the database file and
/// written as one row of the Excel file.
/// </summary>
public sealed class SummaryRecord
{
    // Order of the result columns in <see cref="Results"/> and in the Excel sheet.
    public const int IxYield = 0, IxRon = 1, IxPin = 2, IxXFactor = 3, IxLhsv = 4, IxH2Hc = 5,
                     IxMwMakeUp = 6, IxMwRecycle = 7, IxMwFeed = 8,
                     IxMolarMakeUp = 9, IxMolarRecycle = 10, IxMolarFeed = 11,
                     IxH2MolarMakeUp = 12, IxH2MolarRecycle = 13;

    public static readonly string[] ResultHeaders =
    {
        "Yield (%)", "RON", "PIN (%)", "X. Factor (wt%)", "LHSV (1/hr)", "H2/HC ratio (mol/mol)",
        "Avg. M.Wt Make Up (g/mol)", "Avg. M.Wt Recycle (g/mol)", "Avg. M.Wt Feed (g/mol)",
        "Molar flow Make Up (kmol/hr)", "Molar flow Recycle (kmol/hr)", "Molar flow Feed (kmol/hr)",
        "H2 molar flow Make Up (kmol/hr)", "H2 molar flow Recycle (kmol/hr)"
    };

    /// <summary>Columns of the "Summary" sheet: ID, date, then the results.</summary>
    public static string[] SheetHeaders =>
        new[] { "ID", "Date & time" }.Concat(ResultHeaders).ToArray();

    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public string Id { get; set; } = "R" + Guid.NewGuid().ToString("N");
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public double[] Results { get; set; } = new double[ResultHeaders.Length];

    /// <summary>Typed texts keyed "Tab.BoxName" (for example "Production.FeedTon").</summary>
    public Dictionary<string, string> Inputs { get; set; } = new();

    public string DateText => SavedAt.ToString(DateFormat, CultureInfo.InvariantCulture);
}
