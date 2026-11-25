using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>Feed / Saladin / North / LPG volumes, yield and hourly rates (full-day operation).</summary>
public partial class ProductionView : UserControl, IScreen
{
    readonly List<TextBox> inputs;

    public event Action? Changed;

    public ProductionView()
    {
        InitializeComponent();
        inputs = ScreenHelper.Inputs(this).ToList();
        if (DesignerProperties.GetIsInDesignMode(this)) return;

        ScreenHelper.Hook(this, Recalculate, () => Changed?.Invoke());
        Recalculate();
    }

    static double Volume(TextBox ton, TextBox density) =>
        Calc.VolumeM3(Num.Parse(ton.Text), Num.Parse(density.Text), false, 0);

    static double Rate(TextBox ton, TextBox density) =>
        Calc.HourlyRateM3(Num.Parse(ton.Text), Num.Parse(density.Text));

    /// <summary>Yield (%) = (Saladin + North) / Feed x 100. North uses the Saladin density.</summary>
    public double YieldPercent() =>
        Calc.YieldPercent(Volume(SaladinTon, SaladinDensity) + Volume(NorthTon, SaladinDensity),
                          Volume(FeedTon, FeedDensity));

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
        // North density is always the Saladin density.
        NorthDensity.Text = SaladinDensity.Text;

        double feedVol = Volume(FeedTon, FeedDensity);
        double saladinVol = Volume(SaladinTon, SaladinDensity);
        double northVol = Volume(NorthTon, SaladinDensity);
        double lpgVol = Volume(LpgTon, LpgDensity);
        double totalProd = saladinVol + northVol;
        double yield = Calc.YieldPercent(totalProd, feedVol);

        double feedRate = Rate(FeedTon, FeedDensity);
        double saladinRate = Rate(SaladinTon, SaladinDensity);
        double northRate = Rate(NorthTon, SaladinDensity);
        double lpgRate = Rate(LpgTon, LpgDensity);

        TvYield.Text = yield.ToString("F2", CultureInfo.InvariantCulture) + "%";
        TvTotal.Text = "Total production: " + Whole(totalProd) + " m³/day";

        new ResultText(14, 10)
            .Heading("Volumes (m³/day)")
            .Row("Feed", Whole(feedVol))
            .Row("Saladin", Whole(saladinVol))
            .Row("North", Whole(northVol))
            .Row("L.P.G gas", Whole(lpgVol))
            .Gap()
            .Heading("Hourly rate (m³/hr)")
            .Row("Feed", Whole(feedRate))
            .Row("Saladin", Whole(saladinRate))
            .Row("North", Whole(northRate))
            .Row("Sal + North", Whole(saladinRate + northRate))
            .Row("L.P.G gas", Whole(lpgRate))
            .ApplyTo(TvDetails);
    }

    static string Whole(double v) => v.ToString("F0", CultureInfo.InvariantCulture);
}
