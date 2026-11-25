using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>
/// Summary of the Production yield and the RON, PIN, X-Factor, LHSV and H2/HC results.
/// It has no inputs of its own; the main window calls Attach() once and Refresh() on every change.
/// </summary>
public partial class SummaryView : UserControl
{
    ProductionView? production;
    RonView? ron;
    XFactorView? xFactor;
    LhsvView? lhsv;
    H2HcView? h2hc;
    readonly ObservableCollection<RecordRow> rows = new();

    public SummaryView()
    {
        InitializeComponent();
        RecordList.ItemsSource = rows;
    }

    /// <summary>Connects the Summary to the five calculator tabs.</summary>
    public void Attach(ProductionView production, RonView ron, XFactorView xFactor,
                       LhsvView lhsv, H2HcView h2hc)
    {
        this.production = production;
        this.ron = ron;
        this.xFactor = xFactor;
        this.lhsv = lhsv;
        this.h2hc = h2hc;
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            ShowExcelPath();
            ReloadRows();
        }
        Refresh();
    }

    public void Refresh()
    {
        if (DesignerProperties.GetIsInDesignMode(this)) return;
        if (production == null || ron == null || xFactor == null || lhsv == null || h2hc == null) return;

        h2hc.Refresh();

        TvXFactor.Text = Num.Fixed(xFactor.Value(), 4);
        TvH2hc.Text = Num.Fixed(h2hc.Ratio, 4);
        TvLhsv.Text = Num.Fixed(lhsv.Value(), 4);
        TvRon.Text = Num.Fixed(ron.Ron(), 4);
        TvPin.Text = Num.Fixed(ron.Pin(), 4);
        TvYield.Text = Num.Fixed(production.YieldPercent(), 2);

        new ResultText(18, 10)
            .Heading("H2/HC")
            .Row("Avg. M.Wt Make Up", Num.Fixed(h2hc.MakeUpAvgMw, 4), "g/mol")
            .Row("Avg. M.Wt Recycle", Num.Fixed(h2hc.RecycleAvgMw, 4), "g/mol")
            .Row("Avg. M.Wt Feed", Num.Fixed(h2hc.FeedAvgMw, 4), "g/mol")
            .Row("Molar Make Up", Num.Fixed(h2hc.MakeUpMolar, 4), "kmol/hr")
            .Row("Molar Recycle", Num.Fixed(h2hc.RecycleMolar, 4), "kmol/hr")
            .Row("Molar Feed", Num.Fixed(h2hc.FeedMolar, 4), "kmol/hr")
            .Row("H2 Molar Make Up", Num.Fixed(h2hc.MakeUpH2Molar, 4), "kmol/hr")
            .Row("H2 Molar Recycle", Num.Fixed(h2hc.RecycleH2Molar, 4), "kmol/hr")
            .ApplyTo(TvDetails);
    }

    /// <summary>True when anything is typed on any of the calculator tabs.</summary>
    bool AnyInput() => production!.HasInput() || ron!.HasInput() || xFactor!.HasInput()
                       || lhsv!.HasInput() || h2hc!.HasInput();

    (string Key, IScreen Screen)[] Screens() => new (string, IScreen)[]
    {
        ("Production", production!), ("Ron", ron!), ("XFactor", xFactor!), ("Lhsv", lhsv!), ("H2Hc", h2hc!)
    };

    Dictionary<string, string> CollectInputs()
    {
        var all = new Dictionary<string, string>();
        foreach (var (key, screen) in Screens())
            foreach (var kv in screen.GetValues()) all[key + "." + kv.Key] = kv.Value;
        return all;
    }

    SummaryRecord Record() => new SummaryRecord
    {
        SavedAt = DateTime.Now,
        Results = new[]
        {
            production!.YieldPercent(), ron!.Ron(), ron.Pin(), xFactor!.Value(), lhsv!.Value(), h2hc!.Ratio,
            h2hc.MakeUpAvgMw, h2hc.RecycleAvgMw, h2hc.FeedAvgMw,
            h2hc.MakeUpMolar, h2hc.RecycleMolar, h2hc.FeedMolar,
            h2hc.MakeUpH2Molar, h2hc.RecycleH2Molar
        },
        Inputs = CollectInputs()
    };

    /// <summary>Puts a saved record's numbers back into the calculator tabs.</summary>
    public void ApplyRecord(SummaryRecord record)
    {
        foreach (var (key, screen) in Screens())
        {
            string prefix = key + ".";
            var part = record.Inputs
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                .ToDictionary(kv => kv.Key.Substring(prefix.Length), kv => kv.Value);
            screen.SetValues(part);
        }
        Refresh();
    }

    const int MaxPoints = 60;

    // The figures the curve can show, in the order of the chips: index in Results, unit.
    static readonly (int Index, string Unit)[] TrendMetrics =
    {
        (SummaryRecord.IxXFactor, "wt%"), (SummaryRecord.IxH2Hc, "mol/mol"), (SummaryRecord.IxRon, ""),
        (SummaryRecord.IxPin, "%"), (SummaryRecord.IxYield, "%"), (SummaryRecord.IxLhsv, "1/hr")
    };

    /// <summary>Saved records, oldest first (database + Excel file).</summary>
    List<SummaryRecord> history = new();
    int metric;

    void Chip_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && int.TryParse(tag, out int i)
            && i >= 0 && i < TrendMetrics.Length)
        {
            metric = i;
            ShowTrend();
        }
    }

    /// <summary>
    /// Draws the saved records only. It is redrawn after a save, a delete, a restore or an import,
    /// not while the figures are being typed.
    /// </summary>
    void ShowTrend()
    {
        // Fires once while the XAML is still being built (the first chip is checked): nothing to draw yet.
        if ((object?)Chart == null) return;

        int n = Math.Min(history.Count, MaxPoints);
        int from = history.Count - n;
        int index = TrendMetrics[metric].Index;

        var values = new double[n];
        var times = new DateTime[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = history[from + i].Results[index];
            times[i] = history[from + i].SavedAt;
        }
        Chart.SetData(values, times, TrendMetrics[metric].Unit, false);

        string Day(DateTime t) => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string text;
        if (n == 0)
            text = "لا توجد سجلات محفوظة بعد. احفظ الملخص ليظهر المنحنى.";
        else if (n == 1)
            text = "سجل واحد محفوظ. احفظ سجلًا آخر ليظهر الاتجاه.";
        else if (history.Count > n)
            text = $"آخر {n} من {history.Count} سجل · من {Day(times[0])} إلى {Day(times[n - 1])}";
        else
            text = $"{n} سجل · من {Day(times[0])} إلى {Day(times[n - 1])}";
        TvTrendInfo.Text = text + "\nمرّر الفأرة على المنحنى لقراءة قيمة أي نقطة.";
    }

    void ReloadRows()
    {
        rows.Clear();
        try
        {
            var all = RecordStore.Load();
            foreach (var rec in all.OrderByDescending(r => r.SavedAt))
                rows.Add(new RecordRow(rec));
            history = all.OrderBy(r => r.SavedAt).ToList();
            ShowTrend();
        }
        catch (Exception ex)
        {
            TvStatus.Text = "تعذّرت قراءة قاعدة البيانات: " + ex.Message;
        }
        TvCount.Text = "عدد السجلات: " + rows.Count;
    }

    /// <summary>Saves the current results as a new record (button and File ▸ حفظ).</summary>
    /// <returns>the message shown to the user</returns>
    public string SaveRecord()
    {
        if (production == null) return "";
        Refresh();
        if (!AnyInput())
        {
            TvStatus.Text = "لا توجد بيانات مدخلة لحفظها";
            return TvStatus.Text;
        }
        try
        {
            string? warning = RecordStore.Add(Record());
            ReloadRows();
            TvStatus.Text = warning ?? $"تم الحفظ ({rows.Count} سجل)\n{RecordStore.ExcelPath}";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                   || ex is System.Text.Json.JsonException)
        {
            TvStatus.Text = "تعذّر الحفظ: " + ex.Message;
        }
        return TvStatus.Text;
    }

    /// <summary>
    /// File ▸ فتح: reads a file exported by this program, adds its records to the database
    /// and loads the newest one into the calculator tabs.
    /// </summary>
    public string ImportFile(string path)
    {
        List<SummaryRecord> imported;
        try
        {
            imported = RecordStore.ImportExcel(path);
        }
        catch (FormatException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return "تعذّر فتح الملف: " + ex.Message;
        }
        if (imported.Count == 0) return "الملف لا يحتوي على أي سجل.";

        string? warning = null;
        try { warning = RecordStore.Merge(imported); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return "تعذّر تحديث قاعدة البيانات: " + ex.Message;
        }
        ReloadRows();
        ApplyRecord(imported.OrderByDescending(r => r.SavedAt).First());
        string msg = $"تم فتح الملف: {imported.Count} سجل، وُضعت أرقام أحدث سجل في أماكنها.";
        if (warning != null) msg += "\n" + warning;
        TvStatus.Text = msg;
        return msg;
    }

    /// <summary>File ▸ فتح on a database backup (.isobak): merges it in and loads the newest record.</summary>
    public string ImportBackup(string path)
    {
        var (ok, message) = RecordStore.Restore(path, replace: false);
        if (!ok) return message;
        ReloadRows();
        try
        {
            var newest = RecordStore.Load().OrderByDescending(r => r.SavedAt).FirstOrDefault();
            if (newest != null) ApplyRecord(newest);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
        string msg = "تم فتح قاعدة البيانات، وُضعت أرقام أحدث سجل في أماكنها.\n" + message;
        TvStatus.Text = msg;
        return msg;
    }

    void Save_Click(object sender, RoutedEventArgs e) => SaveRecord();

    void Print_Click(object sender, RoutedEventArgs e)
    {
        if (production == null) return;
        Refresh();
        if (!AnyInput())
        {
            TvStatus.Text = "لا توجد بيانات مدخلة لطباعتها";
            return;
        }
        try
        {
            // Preview first; the print button inside the preview opens the print dialog.
            var preview = new PrintPreviewWindow(Record()) { Owner = Window.GetWindow(this) };
            preview.ShowDialog();
            if (preview.Result != null) TvStatus.Text = preview.Result;
        }
        catch (Exception ex)
        {
            TvStatus.Text = "تعذّرت المعاينة: " + ex.Message;
        }
    }

    void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "نسخة احتياطية من قاعدة البيانات",
            Filter = "Isomerization backup (*.isobak)|*.isobak",
            DefaultExt = ".isobak",
            FileName = "IsomerizationBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmm")
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        TvStatus.Text = RecordStore.Backup(dialog.FileName).Message;
    }

    void Restore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "استرجاع نسخة احتياطية",
            Filter = "Isomerization backup (*.isobak)|*.isobak",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        var answer = MessageBox.Show(
            Window.GetWindow(this),
            "نعم = استبدال: تصبح قاعدة البيانات وملف Excel مطابقين للنسخة فقط (تُحذف السجلات الحالية).\n" +
            "لا = دمج: تُضاف سجلات النسخة إلى السجلات الحالية دون حذف شيء.\n" +
            "إلغاء = لا يتم شيء.",
            "استرجاع", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        if (answer == MessageBoxResult.Cancel) return;

        var (ok, message) = RecordStore.Restore(dialog.FileName, replace: answer == MessageBoxResult.Yes);
        TvStatus.Text = message;
        if (ok) ReloadRows();
    }

    void ShowExcelPath() => TvExcelPath.Text = "ملف Excel الحالي:\n" + RecordStore.ExcelPath;

    /// <summary>Lets the user choose where the one Excel file lives.</summary>
    void ChooseExcel_Click(object sender, RoutedEventArgs e)
    {
        string current = RecordStore.ExcelPath;
        string? dir = Path.GetDirectoryName(current);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "اختر مكان ملف Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = Path.GetFileName(current),
            InitialDirectory = dir != null && Directory.Exists(dir) ? dir : "",
            OverwritePrompt = false // choosing an existing file means "keep adding rows to it"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        var (ok, message) = RecordStore.UseExcelFile(dialog.FileName);
        TvStatus.Text = message;
        if (ok)
        {
            ShowExcelPath();
            ReloadRows();
        }
    }

    void LoadRow_Click(object sender, RoutedEventArgs e)
    {
        if (production == null || (sender as FrameworkElement)?.Tag is not RecordRow row) return;
        ApplyRecord(row.Record);
        TvStatus.Text = "حُمّل السجل في الحقول: " + row.Date;
    }

    void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        // Everything already marked → unmark all; otherwise mark all.
        bool mark = rows.Any(r => !r.IsChecked);
        foreach (var r in rows) r.IsChecked = mark;
    }

    void DeleteMarked_Click(object sender, RoutedEventArgs e)
    {
        var ids = rows.Where(r => r.IsChecked).Select(r => r.Record.Id).ToHashSet();
        if (ids.Count == 0)
        {
            TvStatus.Text = "ضع علامة على سجل واحد على الأقل ثم اضغط حذف المحدد";
            return;
        }
        var answer = MessageBox.Show(
            Window.GetWindow(this),
            $"هل تريد حذف {ids.Count} سجل نهائياً من قاعدة البيانات وملف Excel؟",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            string? warning = RecordStore.Delete(ids);
            ReloadRows();
            TvStatus.Text = warning ?? $"تم حذف {ids.Count} سجل (السطور المعلَّمة فقط) من قاعدة البيانات وملف Excel";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            TvStatus.Text = "تعذّر الحذف: " + ex.Message;
        }
    }
}
