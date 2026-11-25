using System.ComponentModel;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>X. Factor = CHX + Benzene + C7+ (wt%).</summary>
public partial class XFactorView : UserControl, IScreen
{
    readonly List<TextBox> inputs;

    public event Action? Changed;

    public XFactorView()
    {
        InitializeComponent();
        inputs = ScreenHelper.Inputs(this).ToList();
        if (DesignerProperties.GetIsInDesignMode(this)) return;

        ScreenHelper.Hook(this, Recalculate, () => Changed?.Invoke());
        Recalculate();
    }

    /// <summary>X. Factor in wt%.</summary>
    public double Value() =>
        Calc.XFactor(Num.Parse(Chx.Text), Num.Parse(Benzene.Text), Num.Parse(C7Plus.Text));

    public void Refresh() => Recalculate();

    public bool HasInput() => inputs.Any(b => !Num.IsBlank(b.Text));

    public Dictionary<string, string> GetValues() => ScreenHelper.Read(inputs);

    public void SetValues(IReadOnlyDictionary<string, string> values)
    {
        ScreenHelper.Write(inputs, values);
        Recalculate();
    }

    public void Clear()
    {
        foreach (var b in inputs) b.Text = "";
        Recalculate();
        Changed?.Invoke();
    }

    void Recalculate()
    {
        TvResult.Text = Num.Fixed(Value(), 4) + " wt%";
    }
}
