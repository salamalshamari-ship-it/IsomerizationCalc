using System.Windows;

namespace IsomerizationCalc;

/// <summary>The small "About" window (version, developer, short description, update check).</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var v = UpdateChecker.Current;
        RunVersion.Text = $"{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
    }

    void Ok_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Every result of the update check is shown in a message box so it cannot be missed.</summary>
    void Say(string text, MessageBoxImage icon = MessageBoxImage.Information) =>
        MessageBox.Show(this, text, "تحديث", MessageBoxButton.OK, icon, MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

    /// <summary>Looks on GitHub for a newer release and, if the user agrees, installs it.</summary>
    async void Update_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        TvUpdate.Text = "جارٍ البحث عن تحديث...";
        try
        {
            var info = await UpdateChecker.CheckAsync();
            if (info == null)
            {
                TvUpdate.Text = "";
                Say("إصدارك الأحدث");
                return;
            }
            TvUpdate.Text = "";
            if (info.InstallerUrl == null)
            {
                TvUpdate.Text = "";
                Say("يوجد تحديث جديد لكن تعذّر العثور على ملف التحديث.", MessageBoxImage.Warning);
                return;
            }
            var answer = MessageBox.Show(this,
                "يوجد تحديث جديد , هل تريد التحديث؟",
                "تحديث", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            if (answer != MessageBoxResult.Yes) return;

            // Keep a copy of the database and the Excel file before anything changes.
            var (backedUp, backupMessage) = RecordStore.BackupBeforeUpdate();
            if (!backedUp)
            {
                TvUpdate.Text = "";
                Say(backupMessage + "\nلم يتم التحديث.", MessageBoxImage.Warning);
                return;
            }

            TvUpdate.Text = "جارٍ تنزيل التحديث...";
            string path = await UpdateChecker.DownloadInstallerAsync(info);
            UpdateChecker.RunInstaller(path);
            Environment.Exit(0);   // the installer replaces the running program
        }
        catch (InvalidOperationException ex)
        {
            TvUpdate.Text = "";
            Say(ex.Message, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            TvUpdate.Text = "";
            Say("تعذّر التحديث: " + ex.Message, MessageBoxImage.Warning);
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }
}
