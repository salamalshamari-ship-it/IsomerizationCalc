using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace IsomerizationCalc;

/// <summary>
/// Looks for a newer release on GitHub. Set <see cref="Owner"/> and <see cref="Repo"/> below
/// to your GitHub user name and repository name.
/// </summary>
public static class UpdateChecker
{
    // ======== EDIT THESE TWO LINES ========
    public const string Owner = "salamalshamari-ship-it";
    public const string Repo = "IsomerizationCalc";
    // ======================================

    public sealed record UpdateInfo(Version Latest, string Tag, string? InstallerUrl, string PageUrl);

    public static Version Current =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);

    static HttpClient NewClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IsomerizationCalc");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    static bool TryParseTag(string tag, out Version version)
    {
        string t = tag.Trim().TrimStart('v', 'V');
        if (Version.TryParse(t, out var v) && v != null)
        {
            // "1.2" and "1.2.0" are the same version.
            version = new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
            return true;
        }
        version = new Version(0, 0);
        return false;
    }

    /// <returns>the newer release, or null when this version is already the latest</returns>
    /// <exception cref="InvalidOperationException">a message ready to show to the user</exception>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        if (Owner.StartsWith("YOUR_", StringComparison.Ordinal))
            throw new InvalidOperationException("تعذّر البحث عن تحديث حالياً.");

        string json;
        try
        {
            using var client = NewClient();
            using var response = await client.GetAsync($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("لا يوجد تحديث منشور بعد.");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("تعذّر البحث عن تحديث (الرمز " + (int)response.StatusCode + ").");
            json = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("تعذّر الاتصال بالإنترنت.");
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException("انتهت مهلة الاتصال، حاول مرة أخرى.");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!TryParseTag(tag, out var latest))
            throw new InvalidOperationException("رقم الإصدار الجديد غير مفهوم: " + tag);

        var cur = Current;
        var current = new Version(cur.Major, cur.Minor, Math.Max(cur.Build, 0), Math.Max(cur.Revision, 0));
        if (latest <= current) return null;

        string? url = null;
        if (root.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
            {
                string name = a.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    && name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                {
                    url = a.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        string page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";
        return new UpdateInfo(latest, tag, url, page);
    }

    /// <summary>Downloads the installer to a temp folder and starts it. The caller then closes the program.</summary>
    public static async Task<string> DownloadInstallerAsync(UpdateInfo info)
    {
        if (info.InstallerUrl == null)
            throw new InvalidOperationException("لا يحتوي الإصدار على ملف تثبيت (Setup.exe).");
        string path = Path.Combine(Path.GetTempPath(), "IsomerizationCalc_Setup_" + info.Tag.TrimStart('v', 'V') + ".exe");
        try
        {
            using var client = NewClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            using var response = await client.GetAsync(info.InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var file = File.Create(path);
            await response.Content.CopyToAsync(file);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is TaskCanceledException)
        {
            throw new InvalidOperationException("تعذّر تنزيل ملف التحديث: " + ex.Message);
        }
        return path;
    }

    public static void RunInstaller(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
