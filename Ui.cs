using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace IsomerizationCalc;

static class Ui
{
    /// <summary>A colour from App.xaml (Ink, Muted, Accent, ...).</summary>
    public static Brush Res(string key) => (Brush)Application.Current.FindResource(key);
}

/// <summary>
/// Monospace "heading + label/value rows" block used in the result cards (port of ResultText.java).
/// The text is written into a TextBlock that is placed in the XAML.
/// </summary>
sealed class ResultText
{
    readonly List<Inline> parts = new();
    readonly int labelWidth;
    readonly int valueWidth;
    bool started;

    public ResultText(int labelWidth, int valueWidth)
    {
        this.labelWidth = labelWidth;
        this.valueWidth = valueWidth;
    }

    void Break()
    {
        if (started) parts.Add(new Run("\n"));
        started = true;
    }

    public ResultText Heading(string text)
    {
        Break();
        parts.Add(new Run(text) { FontWeight = FontWeights.Bold, Foreground = Ui.Res("Muted") });
        return this;
    }

    public ResultText Row(string label, string value, string? unit = null)
    {
        Break();
        parts.Add(new Run(label.PadRight(labelWidth)) { Foreground = Ui.Res("Ink") });
        parts.Add(new Run(value.PadLeft(valueWidth)) { Foreground = Ui.Res("Accent"), FontWeight = FontWeights.Bold });
        if (unit != null) parts.Add(new Run(" " + unit) { Foreground = Ui.Res("Muted") });
        return this;
    }

    /// <summary>An empty line between two blocks.</summary>
    public ResultText Gap()
    {
        if (started) parts.Add(new Run("\n"));
        return this;
    }

    public void ApplyTo(System.Windows.Controls.TextBlock target)
    {
        target.Inlines.Clear();
        foreach (var p in parts) target.Inlines.Add(p);
    }
}
