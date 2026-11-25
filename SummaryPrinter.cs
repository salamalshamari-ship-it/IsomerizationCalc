using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace IsomerizationCalc;

/// <summary>Prints the summary: opens the Windows print dialog (printer, its properties, copies, pages).</summary>
static class SummaryPrinter
{
    static readonly (string Key, string Title)[] Tabs =
    {
        ("Production", "Production"), ("Ron", "RON"), ("XFactor", "X-Factor"), ("Lhsv", "LHSV"), ("H2Hc", "H₂/HC")
    };

    /// <returns>a message, or null when the user cancelled the print dialog</returns>
    public static string? Print(SummaryRecord record)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return null;

        var doc = Build(record);
        doc.PageWidth = dialog.PrintableAreaWidth;
        doc.PageHeight = dialog.PrintableAreaHeight;
        doc.PagePadding = new Thickness(48);
        doc.ColumnWidth = double.PositiveInfinity;

        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Isomerization summary");
        return "تم إرسال الملخص إلى الطابعة";
    }

    /// <summary>The page as shown in the preview window (A4, 96 dpi units).</summary>
    public static FlowDocument BuildA4(SummaryRecord record)
    {
        var doc = Build(record);
        doc.PageWidth = 793.7;
        doc.PageHeight = 1122.5;
        doc.PagePadding = new Thickness(48);
        doc.ColumnWidth = double.PositiveInfinity;
        return doc;
    }

    static string F(double v, int decimals) => v.ToString("F" + decimals, CultureInfo.InvariantCulture);

    static FlowDocument Build(SummaryRecord rec)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Foreground = Brushes.Black,
            FlowDirection = FlowDirection.RightToLeft
        };

        doc.Blocks.Add(new Paragraph(new Run("ملخص حاسبة إنتاج وحدة الازمرة"))
        {
            FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Blocks.Add(new Paragraph(new Run(rec.SavedAt.ToString("yyyy-MM-dd  HH:mm", CultureInfo.InvariantCulture)))
        {
            Foreground = Brushes.DimGray, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 14)
        });

        double[] r = rec.Results;

        doc.Blocks.Add(Heading("النتائج الرئيسية"));
        doc.Blocks.Add(Grid(new List<(string, string)>
        {
            ("RON", F(r[SummaryRecord.IxRon], 4)),
            ("PIN (%)", F(r[SummaryRecord.IxPin], 4)),
            ("X. Factor (wt%)", F(r[SummaryRecord.IxXFactor], 4)),
            ("LHSV (1/hr)", F(r[SummaryRecord.IxLhsv], 4)),
            ("H2/HC (mol/mol)", F(r[SummaryRecord.IxH2Hc], 4)),
            ("Yield (%)", F(r[SummaryRecord.IxYield], 2))
        }, 3));

        doc.Blocks.Add(Heading("نتائج H₂/HC التفصيلية"));
        var detail = new List<(string, string)>();
        for (int i = SummaryRecord.IxMwMakeUp; i < SummaryRecord.ResultHeaders.Length; i++)
            detail.Add((SummaryRecord.ResultHeaders[i], F(r[i], 4)));
        doc.Blocks.Add(Grid(detail, 2));

        foreach (var (key, title) in Tabs)
        {
            string prefix = key + ".";
            var typed = new Dictionary<string, string>();
            foreach (var kv in rec.Inputs)
                if (kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                    typed[kv.Key.Substring(prefix.Length)] = kv.Value;
            if (typed.Count == 0) continue;

            doc.Blocks.Add(Heading("المدخلات: " + title));

            if (key == "H2Hc")
            {
                // One titled group per stream; the fields under it carry only the component name.
                foreach (var stream in Streams)
                {
                    var items = new List<(string, string)>();
                    foreach (var (id, label) in stream.Components)
                        if (typed.TryGetValue(stream.Prefix + "In" + id, out var v)) items.Add((label, v));
                    if (typed.TryGetValue(stream.Prefix + "Flow", out var flow)) items.Add((stream.FlowLabel, flow));
                    if (items.Count == 0) continue;
                    var sub = SubHeading(stream.Title);
                    // Recycle Gas always starts a fresh page so its table is never split across two sheets.
                    if (stream.Prefix == "Rc") sub.BreakPageBefore = true;
                    doc.Blocks.Add(sub);
                    doc.Blocks.Add(Grid(items, 2));
                }
            }
            else
            {
                var items = typed.Select(kv => (Pretty(key, kv.Key), kv.Value)).ToList();
                doc.Blocks.Add(Grid(items, 2));
            }
        }
        return doc;
    }

    // ---- names shown in the printout -------------------------------------------------------

    sealed record StreamDef(string Prefix, string Title, (string Id, string Label)[] Components, string FlowLabel);

    static readonly (string Id, string Label)[] GasComponents =
    {
        ("H2", "H₂"), ("C1", "C1"), ("C2", "C2"), ("C3", "C3"),
        ("IC4", "iC4"), ("NC4", "nC4"), ("IC5", "iC5"), ("NC5", "nC5")
    };

    static readonly (string Id, string Label)[] FeedComponents =
    {
        ("IC4", "IC4"), ("NC4", "NC4"), ("IC5", "IC5"), ("NC5", "NC5"), ("DMB22", "2.2 DMB"), ("CP", "C.P"),
        ("DMB23", "2.3 DMB"), ("MP2", "2. MP"), ("MP3", "3. MP"), ("NHEX", "N.HEX"), ("DMP22", "2.2 DMP-C7"),
        ("MCP", "M.C.P"), ("DMP24", "2.4-DMP-C7"), ("BEN", "BEN"), ("CHEX", "C.HEX"), ("MHEX2", "2.M-HEX-C7"),
        ("DMP23", "2.3-DMP-C7"), ("MHEX3", "3M-HEX-C7"), ("CISDMCP", "CIS-1.3-DMCP-C7"),
        ("TRDMCP", "TRANS-1.3-DMCP-C7"), ("NHEP", "N-HEP-C7"), ("MCH", "M.C.H-C7")
    };

    static readonly StreamDef[] Streams =
    {
        new("Mk", "Make Up", GasComponents, "Flowrate (kg/hr)"),
        new("Rc", "Recycle Gas", GasComponents, "Flowrate (t/hr)"),
        new("Fd", "Feed Components", FeedComponents, "Flowrate (t/hr)")
    };

    static readonly Dictionary<string, string> RonLabels = new()
    {
        ["IC4"] = "iC4", ["NC4"] = "nC4", ["IC5"] = "iC5", ["NC5"] = "nC5", ["CP"] = "CP",
        ["DMB22"] = "22DMB", ["DMB23"] = "23DMB", ["MP2"] = "2MP", ["MP3"] = "3MP", ["NC6"] = "nC6",
        ["MCP"] = "MCP", ["Benzene"] = "Benzene", ["CHX"] = "CHX", ["P7Plus"] = "P7+", ["N7Plus"] = "N7+"
    };

    static readonly Dictionary<string, string> Words = new()
    {
        ["Lpg"] = "LPG", ["Chx"] = "CHX", ["C7Plus"] = "C7+"
    };

    /// <summary>Readable name of an input box: words separated, no code-style names (C7Plus → C7+).</summary>
    static string Pretty(string tab, string name)
    {
        if (tab == "Ron")
        {
            string id = name.StartsWith("Area", StringComparison.Ordinal) ? name.Substring(4) : name;
            if (RonLabels.TryGetValue(id, out var label)) return label;
            name = id;
        }
        if (Words.TryGetValue(name, out var word)) return word;

        // FeedTon → "Feed Ton", CatalystVolume → "Catalyst Volume".
        string spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        return string.Join(" ", spaced.Split(' ').Select(w => Words.TryGetValue(w, out var x) ? x : w));
    }

    static Paragraph SubHeading(string text) => new(new Run(text))
    {
        FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 3),
        FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left
    };

    static Paragraph Heading(string text) => new(new Run(text))
    {
        FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 4)
    };

    /// <summary>Label / value pairs laid out <paramref name="perRow"/> pairs per row.</summary>
    static Table Grid(IReadOnlyList<(string Label, string Value)> items, int perRow)
    {
        var table = new Table
        {
            CellSpacing = 0,
            FlowDirection = FlowDirection.LeftToRight,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
        for (int i = 0; i < perRow; i++)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        }
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        for (int i = 0; i < items.Count; i += perRow)
        {
            var row = new TableRow();
            for (int j = 0; j < perRow; j++)
            {
                if (i + j < items.Count)
                {
                    row.Cells.Add(Cell(items[i + j].Label, false));
                    row.Cells.Add(Cell(items[i + j].Value, true));
                }
                else
                {
                    row.Cells.Add(Cell("", false));
                    row.Cells.Add(Cell("", false));
                }
            }
            group.Rows.Add(row);
        }
        return table;
    }

    static TableCell Cell(string text, bool bold) =>
        new(new Paragraph(new Run(text) { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal })
        {
            Margin = new Thickness(0),
            TextAlignment = bold ? TextAlignment.Center : TextAlignment.Left
        })
        {
            Padding = new Thickness(5, 3, 5, 3),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
}
