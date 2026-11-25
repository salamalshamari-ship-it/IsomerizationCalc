using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace IsomerizationCalc;

public partial class MainWindow : Window
{
    const int UnitsIndex = 5;    // no inputs, nothing to clear
    const int SummaryIndex = 6;  // always the last tab

    // The five calculator tabs, in tab order (the Summary has no inputs).
    readonly IScreen[] screens = null!;

    public MainWindow()
    {
        InitializeComponent();

        screens = new IScreen[] { ProductionPage, RonPage, XFactorPage, LhsvPage, H2HcPage };
        SummaryPage.Attach(ProductionPage, RonPage, XFactorPage, LhsvPage, H2HcPage);

        // The Summary stays current whenever something is typed on another tab.
        foreach (var s in screens) s.Changed += SummaryPage.Refresh;

        // Centre the tab headers.
        Loaded += (_, _) =>
        {
            var panel = FindChild<System.Windows.Controls.Primitives.TabPanel>(Tabs);
            if (panel != null)
            {
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.SizeChanged += (_, _) => PlaceClearButton();
            }
            PlaceClearButton();
        };
        SizeChanged += (_, _) => PlaceClearButton();
    }

    /// <summary>Keeps the Clear button right above the Production tab header.</summary>
    void PlaceClearButton()
    {
        if (!ProductionTab.IsLoaded || !TitleBar.IsLoaded) return;
        TitleBar.UpdateLayout();
        double x = ProductionTab.TranslatePoint(new Point(0, 0), TitleBar).X;
        ClearButton.HorizontalAlignment = HorizontalAlignment.Left;
        ClearButton.Margin = new Thickness(Math.Max(0, x), 0, 0, 0);
        ClearButton.MinWidth = ProductionTab.ActualWidth;
    }

    static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var deeper = FindChild<T>(child);
            if (deeper != null) return deeper;
        }
        return null;
    }

    void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Fires while the window is being built, before everything exists.
        if (!ReferenceEquals(e.Source, Tabs) || screens == null) return;

        // The Summary and Units tabs have no inputs to clear.
        bool hasInputs = Tabs.SelectedIndex < UnitsIndex;
        ClearButton.Visibility = hasInputs ? Visibility.Visible : Visibility.Hidden;
        if (Tabs.SelectedIndex == SummaryIndex) SummaryPage.Refresh();
    }

    void Clear_Click(object sender, RoutedEventArgs e)
    {
        int i = Tabs.SelectedIndex;
        if (i < 0 || i >= screens.Length) return;
        screens[i].Clear();
    }

    // ------------------------------------------------------------------ menu

    static void Info(string text, MessageBoxImage icon = MessageBoxImage.Information) =>
        MessageBox.Show(Application.Current.MainWindow, text, "IsomerizationCalc",
                        MessageBoxButton.OK, icon, MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

    /// <summary>File ▸ فتح: a file made by this program — the Excel file or a database backup.</summary>
    void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "فتح ملف من البرنامج (Excel أو قاعدة بيانات)",
            Filter = "ملفات البرنامج (*.xlsx;*.isobak)|*.xlsx;*.isobak|Excel (*.xlsx)|*.xlsx|قاعدة البيانات (*.isobak)|*.isobak",
            InitialDirectory = System.IO.Directory.Exists(RecordStore.Folder) ? RecordStore.Folder : "",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        string msg = dialog.FileName.EndsWith(".isobak", StringComparison.OrdinalIgnoreCase)
            ? SummaryPage.ImportBackup(dialog.FileName)
            : SummaryPage.ImportFile(dialog.FileName);
        Info(msg, msg.StartsWith("تم") ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    /// <summary>File ▸ حفظ: same as the "حفظ الملخص" button.</summary>
    void Save_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        string msg = SummaryPage.SaveRecord();
        if (Tabs.SelectedIndex != SummaryIndex && msg.Length > 0) Info(msg);
    }

    void Exit_Click(object sender, RoutedEventArgs e) => Close();

    void About_Click(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();

    /// <summary>The window's X button and File ▸ خروج: Yes closes, No stays.</summary>
    void Window_Closing(object? sender, CancelEventArgs e)
    {
        var answer = MessageBox.Show(this, "هل حفظت عملك؟", "الخروج",
                                     MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No,
                                     MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        if (answer != MessageBoxResult.Yes) e.Cancel = true;
    }
}
