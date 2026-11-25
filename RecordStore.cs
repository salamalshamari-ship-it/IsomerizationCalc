using System.Globalization;
using System.IO;
using System.Text.Json;
using ClosedXML.Excel;

namespace IsomerizationCalc;

/// <summary>
/// The saved records. The database is a JSON file; the Excel file is rebuilt from it after every
/// change (add / delete), so both always match. Opening an Excel file accepts only a file that
/// this program exported.
/// </summary>
static class RecordStore
{
    const string SummarySheet = "Summary";
    const string InputsSheet = "Inputs";

    static readonly string[] InputHeaders = { "ID", "Field", "Value" };

    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Default folder for the Excel file: Documents\\Isomerization Calculation.</summary>
    public static string Folder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Isomerization Calculation");

    static string AppData => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IsomerizationCalc");

    /// <summary>The database file (kept in the user's AppData).</summary>
    public static string DbPath => Path.Combine(AppData, "records.json");

    static string SettingsPath => Path.Combine(AppData, "settings.json");

    sealed class Settings { public string? ExcelPath { get; set; } }

    /// <summary>
    /// The one Excel file every save is added to. The user can move it with "اختيار مكان ملف Excel".
    /// </summary>
    public static string ExcelPath
    {
        get
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var st = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
                    if (!string.IsNullOrWhiteSpace(st?.ExcelPath)) return st!.ExcelPath!;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException) { }
            return Path.Combine(Folder, "IsomerizationData.xlsx");
        }
    }

    static void SaveExcelPath(string path)
    {
        Directory.CreateDirectory(AppData);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Settings { ExcelPath = path }, Json));
    }

    // ---------------------------------------------------------------- database

    static List<SummaryRecord> LoadDb()
    {
        if (!File.Exists(DbPath)) return new List<SummaryRecord>();
        string text = File.ReadAllText(DbPath);
        if (string.IsNullOrWhiteSpace(text)) return new List<SummaryRecord>();
        return JsonSerializer.Deserialize<List<SummaryRecord>>(text) ?? new List<SummaryRecord>();
    }

    static void WriteDb(List<SummaryRecord> all)
    {
        Directory.CreateDirectory(AppData);
        string temp = DbPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(all, Json));
        File.Move(temp, DbPath, true);
    }

    /// <summary>
    /// The records of the database, plus any record found in the Excel file that the database
    /// does not have yet (the Excel file is the other half of the same data).
    /// </summary>
    public static List<SummaryRecord> Load()
    {
        var all = LoadDb();
        try
        {
            if (File.Exists(ExcelPath))
            {
                var known = all.Select(r => r.Id).ToHashSet();
                var extra = ImportExcel(ExcelPath).Where(r => !known.Contains(r.Id)).ToList();
                if (extra.Count > 0)
                {
                    all.AddRange(extra);
                    WriteDb(all);
                }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException) { }
        return all;
    }

    /// <summary>Message for a problem with the Excel file (the database change itself succeeded).</summary>
    static string ExcelWarning(Exception ex) =>
        ex is FormatException
            ? "تم تحديث قاعدة البيانات لكن ملف Excel الحالي ليس من تصدير البرنامج: " + ex.Message
            : "تم تحديث قاعدة البيانات لكن تعذّر تحديث ملف Excel (أغلقه إن كان مفتوحاً): " + ex.Message;

    /// <summary>Adds one record: a new row at the end of the same Excel file, nothing else is touched.</summary>
    /// <returns>null when all went well, otherwise a warning</returns>
    public static string? Add(SummaryRecord record)
    {
        var all = Load();
        all.Add(record);
        WriteDb(all);
        return TryExcel(() => AppendToExcel(new[] { record }));
    }

    /// <summary>Deletes only the given records (their rows), from the database and from the Excel file.</summary>
    public static string? Delete(ISet<string> ids)
    {
        var all = Load();
        all.RemoveAll(r => ids.Contains(r.Id));
        WriteDb(all);
        return TryExcel(() => DeleteFromExcel(ids));
    }

    /// <summary>Adds the imported records; a record whose ID already exists is replaced in the database.</summary>
    public static string? Merge(IEnumerable<SummaryRecord> imported)
    {
        var list = imported.ToList();
        var all = Load();
        foreach (var rec in list)
        {
            int at = all.FindIndex(r => r.Id == rec.Id);
            if (at >= 0) all[at] = rec; else all.Add(rec);
        }
        WriteDb(all);
        return TryExcel(() => AppendToExcel(list));
    }

    static string? TryExcel(Action action)
    {
        try { action(); return null; }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException)
        {
            return ExcelWarning(ex);
        }
    }

    /// <summary>
    /// Makes <paramref name="path"/> the Excel file used from now on. An existing file of this
    /// program is kept and its records join the database; a new file is created from the database.
    /// </summary>
    /// <returns>a message for the user, and whether the path was accepted</returns>
    public static (bool Ok, string Message) UseExcelFile(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var all = LoadDb();
            if (File.Exists(path))
            {
                var found = ImportExcel(path); // throws FormatException for any other file
                var known = all.Select(r => r.Id).ToHashSet();
                all.AddRange(found.Where(r => !known.Contains(r.Id)));
                WriteDb(all);
                SaveExcelPath(path);
                AppendToExcel(all);            // rows the file does not have yet
                return (true, $"سيُحفظ في هذا الملف من الآن (به {found.Count} سجل سابق، ولن يُمسح شيء):\n{path}");
            }
            ExportExcel(all, path);
            SaveExcelPath(path);
            return (true, $"تم إنشاء الملف وسيُحفظ فيه من الآن:\n{path}");
        }
        catch (FormatException)
        {
            return (false, "هذا الملف موجود وليس من تصدير البرنامج، اختر اسماً آخر لكي لا يُستبدل.");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return (false, "تعذّر استخدام الملف: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------- backup / restore

    sealed class BackupFile
    {
        public string App { get; set; } = "IsomerizationCalc";
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; }
        public List<SummaryRecord> Records { get; set; } = new();
    }

    /// <summary>Writes every saved record (with its typed inputs) to a single backup file.</summary>
    public static (bool Ok, string Message) Backup(string path)
    {
        try
        {
            var all = Load();
            var file = new BackupFile { CreatedAt = DateTime.Now, Records = all };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(file, Json));
            return (true, $"تم النسخ الاحتياطي ({all.Count} سجل):\n{path}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return (false, "تعذّر النسخ الاحتياطي: " + ex.Message);
        }
    }

    /// <summary>
    /// Reads a backup made by <see cref="Backup"/>. <paramref name="replace"/> = true makes the
    /// database and the Excel file hold exactly the backup; false adds the backup's records to
    /// what is already saved (a record with the same ID is replaced).
    /// </summary>
    public static (bool Ok, string Message) Restore(string path, bool replace)
    {
        BackupFile? file;
        try
        {
            file = JsonSerializer.Deserialize<BackupFile>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            file = null;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return (false, "تعذّرت قراءة الملف: " + ex.Message);
        }

        if (file == null || file.App != "IsomerizationCalc" || file.Records == null
            || file.Records.Any(r => string.IsNullOrWhiteSpace(r.Id)
                                     || r.Results == null
                                     || r.Results.Length != SummaryRecord.ResultHeaders.Length))
            return (false, "هذا الملف ليس نسخة احتياطية من البرنامج.");

        foreach (var r in file.Records) r.Inputs ??= new Dictionary<string, string>();

        try
        {
            string? warning;
            if (replace)
            {
                WriteDb(file.Records);
                warning = TryExcel(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ExcelPath))!);
                    ExportExcel(file.Records, ExcelPath);
                });
            }
            else
            {
                warning = Merge(file.Records);
            }
            string msg = $"تم الاسترجاع ({file.Records.Count} سجل).";
            return (true, warning == null ? msg : msg + "\n" + warning);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return (false, "تعذّر الاسترجاع: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------- Excel

    static double Finite(double v) => double.IsFinite(v) ? v : 0;

    static void WriteHeaders(IXLWorksheet sheet, string[] headers)
    {
        for (int c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
    }

    static void WriteSummaryRow(IXLWorksheet ws, int row, SummaryRecord rec)
    {
        ws.Cell(row, 1).Value = rec.Id;
        ws.Cell(row, 2).Value = rec.DateText;
        for (int c = 0; c < SummaryRecord.ResultHeaders.Length; c++)
        {
            double v = c < rec.Results.Length ? rec.Results[c] : 0;
            ws.Cell(row, c + 3).Value = Finite(v);
        }
    }

    /// <summary>Writes the typed inputs of one record from <paramref name="row"/> down; returns the next free row.</summary>
    static int WriteInputRows(IXLWorksheet wi, int row, SummaryRecord rec)
    {
        foreach (var kv in rec.Inputs)
        {
            if (Num.IsBlank(kv.Value)) continue;
            wi.Cell(row, 1).Value = rec.Id;
            wi.Cell(row, 2).Value = kv.Key;
            // A typed number is saved as a number; anything else stays text.
            string normal = Num.Normalize(kv.Value);
            if (double.TryParse(normal, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                && double.IsFinite(d))
                wi.Cell(row, 3).Value = d;
            else
                wi.Cell(row, 3).Value = kv.Value;
            row++;
        }
        return row;
    }

    /// <summary>Creates a brand-new file holding all the records.</summary>
    public static void ExportExcel(IReadOnlyList<SummaryRecord> records, string path)
    {
        using var wb = new XLWorkbook();

        var ws = wb.Worksheets.Add(SummarySheet);
        WriteHeaders(ws, SummaryRecord.SheetHeaders);
        for (int r = 0; r < records.Count; r++) WriteSummaryRow(ws, r + 2, records[r]);
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        var wi = wb.Worksheets.Add(InputsSheet);
        WriteHeaders(wi, InputHeaders);
        int row = 2;
        foreach (var rec in records) row = WriteInputRows(wi, row, rec);
        wi.SheetView.FreezeRows(1);
        wi.Columns().AdjustToContents();

        wb.SaveAs(path);
    }

    static FormatException Bad(string why) =>
        new("هذا الملف ليس ملف Excel المُصدَّر من البرنامج (" + why + ").");

    /// <summary>Opens a workbook and checks that it is one exported by this program.</summary>
    static XLWorkbook OpenChecked(string path)
    {
        XLWorkbook wb;
        try
        {
            // ClosedXML reads the file lazily, so it must be given a stream that stays open:
            // copy the file into memory first (this also works while Excel has the file open).
            var memory = new MemoryStream();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                fs.CopyTo(memory);
            memory.Position = 0;
            wb = new XLWorkbook(memory);
        }
        catch (IOException) { throw; }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            throw new FormatException("تعذّرت قراءة الملف: ليس ملف Excel صالحاً.", ex);
        }

        try
        {
            if (!wb.Worksheets.Contains(SummarySheet) || !wb.Worksheets.Contains(InputsSheet))
                throw Bad("الأوراق غير موجودة");
            var ws = wb.Worksheet(SummarySheet);
            var wi = wb.Worksheet(InputsSheet);
            string[] headers = SummaryRecord.SheetHeaders;
            for (int c = 0; c < headers.Length; c++)
                if (ws.Cell(1, c + 1).GetString().Trim() != headers[c]) throw Bad("عناوين الأعمدة مختلفة");
            for (int c = 0; c < InputHeaders.Length; c++)
                if (wi.Cell(1, c + 1).GetString().Trim() != InputHeaders[c]) throw Bad("عناوين ورقة المدخلات مختلفة");
            return wb;
        }
        catch
        {
            wb.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Adds one new row per record at the end of the existing file (creates the file if it is
    /// missing). Rows already in the file are never touched; a record whose ID is there is skipped.
    /// </summary>
    static void AppendToExcel(IReadOnlyList<SummaryRecord> records)
    {
        string path = ExcelPath;
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            ExportExcel(LoadDb(), path);
            return;
        }

        using var wb = OpenChecked(path);
        var ws = wb.Worksheet(SummarySheet);
        var wi = wb.Worksheet(InputsSheet);

        int last = ws.LastRowUsed()?.RowNumber() ?? 1;
        var present = new HashSet<string>();
        for (int r = 2; r <= last; r++) present.Add(ws.Cell(r, 1).GetString().Trim());

        int row = last + 1;
        int inRow = (wi.LastRowUsed()?.RowNumber() ?? 1) + 1;
        bool added = false;
        foreach (var rec in records)
        {
            if (!present.Add(rec.Id)) continue;
            WriteSummaryRow(ws, row++, rec);
            inRow = WriteInputRows(wi, inRow, rec);
            added = true;
        }
        if (!added) return;

        ws.Columns().AdjustToContents();
        wi.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    /// <summary>Removes the rows of the given records from both sheets; every other row stays.</summary>
    static void DeleteFromExcel(ISet<string> ids)
    {
        string path = ExcelPath;
        if (!File.Exists(path)) return;

        using var wb = OpenChecked(path);
        bool changed = false;
        foreach (string sheetName in new[] { SummarySheet, InputsSheet })
        {
            var sheet = wb.Worksheet(sheetName);
            int last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = last; r >= 2; r--) // bottom-up so row numbers stay valid
            {
                if (ids.Contains(sheet.Cell(r, 1).GetString().Trim()))
                {
                    sheet.Row(r).Delete();
                    changed = true;
                }
            }
        }
        if (changed) wb.SaveAs(path);
    }

    /// <summary>
    /// Reads a file exported by this program. Any other file is rejected with a FormatException.
    /// </summary>
    public static List<SummaryRecord> ImportExcel(string path)
    {
        using var wb = OpenChecked(path);
        var ws = wb.Worksheet(SummarySheet);
        var wi = wb.Worksheet(InputsSheet);

        var byId = new Dictionary<string, SummaryRecord>();
        var order = new List<SummaryRecord>();

        int last = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= last; r++)
        {
            string id = ws.Cell(r, 1).GetString().Trim();
            string date = ws.Cell(r, 2).GetString().Trim();
            if (id.Length == 0 && date.Length == 0) continue; // blank row
            if (id.Length == 0) throw Bad($"معرّف السجل مفقود في الصف {r}");
            if (!DateTime.TryParseExact(date, SummaryRecord.DateFormat, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime when))
                throw Bad($"تاريخ غير صالح في الصف {r}");

            var rec = new SummaryRecord { Id = id, SavedAt = when };
            for (int c = 0; c < SummaryRecord.ResultHeaders.Length; c++)
            {
                if (!ws.Cell(r, c + 3).TryGetValue(out double v) || !double.IsFinite(v))
                    throw Bad($"قيمة غير رقمية في الصف {r}");
                rec.Results[c] = v;
            }
            if (byId.TryGetValue(id, out var old)) order.Remove(old);
            byId[id] = rec;
            order.Add(rec);
        }

        int lastIn = wi.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastIn; r++)
        {
            string id = wi.Cell(r, 1).GetString().Trim();
            string field = wi.Cell(r, 2).GetString().Trim();
            if (id.Length == 0 || field.Length == 0) continue;
            if (!byId.TryGetValue(id, out var rec)) continue;
            var cell = wi.Cell(r, 3);
            rec.Inputs[field] = cell.DataType == XLDataType.Number
                ? cell.GetDouble().ToString("R", CultureInfo.InvariantCulture)
                : cell.GetString();
        }
        return order;
    }
}
