using System.Globalization;
using System.Windows;

namespace IsomerizationCalc;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Numbers always use a dot and Western digits, whatever the Windows language is.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        base.OnStartup(e);
    }
}
