using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>RON and PIN from the GC areas of 15 components.</summary>
public partial class RonView : UserControl, IScreen
{
    readonly List<TextBox> inputs;
    readonly TextBox[] area = null!;
    readonly TextBlock[] fraction = null!;
    readonly TextBlock[] contribution = null!;

    /// <summary>The N7+ field.</summary>
    readonly TextBox n7 = null!;

    /// <summary>
    /// N7+ as the user typed it. While the field is not being edited it shows N7+ - P7+ instead,
    /// and this value is what the calculation uses.
    /// </summary>
    string n7Kept = "";
    bool n7Focused;

    public event Action? Changed;

    public RonView()
    {
        InitializeComponent();
        inputs = ScreenHelper.Inputs(this).ToList();
        if (DesignerProperties.GetIsInDesignMode(this)) return;

        // Same order as the table (and as Calc.RonRef).
        area = new[] { AreaIC4, AreaNC4, AreaIC5, AreaNC5, AreaCP, AreaDMB22, AreaDMB23, AreaMP2, AreaMP3, AreaNC6, AreaMCP, AreaBenzene, AreaCHX, AreaP7Plus, AreaN7Plus };
        fraction = new[] { PerIC4, PerNC4, PerIC5, PerNC5, PerCP, PerDMB22, PerDMB23, PerMP2, PerMP3, PerNC6, PerMCP, PerBenzene, PerCHX, PerP7Plus, PerN7Plus };
        contribution = new[] { SumIC4, SumNC4, SumIC5, SumNC5, SumCP, SumDMB22, SumDMB23, SumMP2, SumMP3, SumNC6, SumMCP, SumBenzene, SumCHX, SumP7Plus, SumN7Plus };

        // N7+ is special: after editing it shows N7+ - P7+ (the value used in the calculation).
        n7 = area[Calc.IdxN7Plus];
        n7.ToolTip = "عند الخروج من الحقل يظهر N7+ − P7+ (القيمة المستخدمة في الحساب)";
        n7.GotKeyboardFocus += (_, _) =>
        {
            n7Focused = true;
            // Editing: show the typed value again so it can be changed digit by digit.
            if (n7.Text != n7Kept)
            {
                n7.Text = n7Kept;
                n7.CaretIndex = n7.Text.Length;
            }
            Recalculate();
        };
        n7.LostKeyboardFocus += (_, _) =>
        {
            // Done editing: remember what was typed; the field now shows N7+ - P7+.
            n7Focused = false;
            n7Kept = n7.Text;
            Recalculate();
        };

        ScreenHelper.Hook(this, Recalculate, () => Changed?.Invoke());
        Recalculate();
    }

    /// <summary>N7+ as typed: the live text while editing, otherwise the kept value.</summary>
    string N7Text() => n7Focused ? n7.Text : n7Kept;

    string RawText(TextBox box) => box == n7 ? N7Text() : box.Text;

    void SetRawText(TextBox box, string text)
    {
        if (box != n7)
        {
            box.Text = text;
            return;
        }
        n7Kept = text;
        if (n7Focused) n7.Text = text;
        else ShowN7Display();
    }

    /// <summary>Writes N7+ - P7+ into the N7+ field while it is not being edited.</summary>
    void ShowN7Display()
    {
        string shown = Num.IsBlank(n7Kept)
            ? ""
            : Num.Fixed(Num.Parse(n7Kept) - Num.Parse(area[Calc.IdxP7Plus].Text), 4);
        if (shown != n7.Text) n7.Text = shown;
    }

    Calc.RonResult Compute()
    {
        double[] values = new double[area.Length];
        for (int i = 0; i < area.Length; i++)
        {
            values[i] = i == Calc.IdxN7Plus ? Num.Parse(N7Text()) : Num.Parse(area[i].Text);
        }
        bool n7Entered = !Num.IsBlank(N7Text());
        return Calc.Ron(values, n7Entered);
    }

    public double Ron() => Compute().Ron;
    public double Pin() => Compute().Pin;

    public void Refresh() => Recalculate();

    public bool HasInput() => inputs.Any(b => !Num.IsBlank(RawText(b)));

    public Dictionary<string, string> GetValues() => ScreenHelper.Read(inputs, RawText);

    public void SetValues(IReadOnlyDictionary<string, string> values)
    {
        ScreenHelper.Write(inputs, values, SetRawText);
        Recalculate();
    }

    public void Clear()
    {
        foreach (var b in inputs) SetRawText(b, "");
        Recalculate();
        Changed?.Invoke();
    }

    void Recalculate()
    {
        if (area == null) return; // designer
        if (!n7Focused) ShowN7Display();
        var r = Compute();
        for (int i = 0; i < area.Length; i++)
        {
            fraction[i].Text = Num.Fixed(r.Fraction[i], 4);
            contribution[i].Text = Num.Fixed(r.Contribution[i], 4);
        }
        TvRon.Text = Num.Fixed(r.Ron, 4);
        TvPin.Text = Num.Fixed(r.Pin, 4);
        TvAreaSum.Text = Num.Fixed(r.AreaSum, 2);
    }
}
