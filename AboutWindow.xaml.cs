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
                TvUpdate.Text = "أنت تستخدم أحدث إصدار.";
                return;
            }
            TvUpdate.Text = "يوجد إصدار أحدث: " + info.Tag;
            if (info.InstallerUrl == null)
            {
                TvUpdate.Text += "\nلا يحتوي ملف تثبيت، افتح صفحة الإصدار على GitHub.";
                return;
            }
            var answer = MessageBox.Show(this,
                $"يوجد إصدار جديد ({info.Tag}).\nسيُنزَّل التحديث ثم يُغلق البرنامج ويبدأ التثبيت.\nتأكد أنك حفظت عملك. هل تريد التحديث الآن؟",
                "تحديث", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            if (answer != MessageBoxResult.Yes) return;

            TvUpdate.Text = "جارٍ تنزيل التحديث...";
            string path = await UpdateChecker.DownloadInstallerAsync(info);
            UpdateChecker.RunInstaller(path);
            Environment.Exit(0);   // the installer replaces the running program
        }
        catch (InvalidOperationException ex)
        {
            TvUpdate.Text = ex.Message;
        }
        catch (Exception ex)
        {
            TvUpdate.Text = "تعذّر التحديث: " + ex.Message;
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }
}
