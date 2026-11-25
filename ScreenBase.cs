using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IsomerizationCalc;

/// <summary>What every calculator tab offers to the main window.</summary>
interface IScreen
{
    /// <summary>Raised after the user typed something (so the Summary can refresh).</summary>
    event Action? Changed;

    /// <summary>Recomputes every result on the tab from the current inputs.</summary>
    void Refresh();

    /// <summary>Empties every input box of the tab.</summary>
    void Clear();

    /// <summary>True when anything is typed on the tab.</summary>
    bool HasInput();

    /// <summary>Every non-empty input box as (box name, typed text).</summary>
    Dictionary<string, string> GetValues();

    /// <summary>Puts the saved texts back into their boxes (boxes missing from the dictionary are emptied).</summary>
    void SetValues(IReadOnlyDictionary<string, string> values);
}

static class ScreenHelper
{
    /// <summary>
    /// Every input box (editable TextBox) inside a tab. Boxes you add in the designer are picked up
    /// automatically, so Clear and the Summary keep working.
    /// </summary>
    public static IEnumerable<TextBox> Inputs(DependencyObject root)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is TextBox box)
            {
                if (!box.IsReadOnly) yield return box;
            }
            else if (child is DependencyObject d)
            {
                foreach (var inner in Inputs(d)) yield return inner;
            }
        }
    }

    /// <summary>Non-empty boxes as name → text. <paramref name="raw"/> overrides how a box's text is read.</summary>
    public static Dictionary<string, string> Read(IEnumerable<TextBox> boxes, Func<TextBox, string>? raw = null)
    {
        var result = new Dictionary<string, string>();
        foreach (var box in boxes)
        {
            if (string.IsNullOrEmpty(box.Name)) continue;
            string text = raw == null ? box.Text : raw(box);
            if (!Num.IsBlank(text)) result[box.Name] = text;
        }
        return result;
    }

    /// <summary>Writes saved texts back by box name; a box with no saved text is emptied.</summary>
    public static void Write(IEnumerable<TextBox> boxes, IReadOnlyDictionary<string, string> values,
                             Action<TextBox, string>? set = null)
    {
        foreach (var box in boxes)
        {
            if (string.IsNullOrEmpty(box.Name)) continue;
            string text = values.TryGetValue(box.Name, out var v) ? v : "";
            if (set == null) box.Text = text; else set(box, text);
        }
    }

    /// <summary>
    /// Recalculates whenever any TextBox inside the tab changes, then tells the main window.
    /// </summary>
    public static void Hook(UIElement view, Action recalculate, Action changed)
    {
        bool busy = false;
        view.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) =>
        {
            if (busy) return;
            busy = true;
            try { recalculate(); }
            finally { busy = false; }
            changed();
        }));
    }
}
