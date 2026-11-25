using System.ComponentModel;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>
/// H2/HC ratio from three streams: make-up gas, recycle gas and the feed composition.
/// Every result is recomputed whenever any input of the tab changes (port of H2HcScreen.java).
/// </summary>
public partial class H2HcView : UserControl, IScreen
{
    /// <summary>One stream: a composition table plus its flow rate.</summary>
    sealed class StreamTable
    {
        readonly TextBox[] inputs;
        readonly TextBlock[] contributions;
        readonly double[] molecularWeights;
        readonly TextBox flow;
        /// <summary>Converts the flow field to kg/hr (1 for kg/hr, 1000 for t/hr).</summary>
        readonly double kgPerFlowUnit;
        readonly TextBlock tvSum;
        readonly TextBlock tvAvgMw;
        readonly TextBlock tvMolar;
        /// <summary>Null for the feed stream, which has no hydrogen result.</summary>
        readonly TextBlock? tvH2Molar;

        public double SumInput;
        public double AvgMw;
        public double MolarFlow;
        public double H2MolarFlow;

        public StreamTable(TextBox[] inputs, TextBlock[] contributions, double[] mw, TextBox flow,
                           double kgPerFlowUnit, TextBlock tvSum, TextBlock tvAvgMw, TextBlock tvMolar,
                           TextBlock? tvH2Molar)
        {
            this.inputs = inputs;
            this.contributions = contributions;
            this.molecularWeights = mw;
            this.flow = flow;
            this.kgPerFlowUnit = kgPerFlowUnit;
            this.tvSum = tvSum;
            this.tvAvgMw = tvAvgMw;
            this.tvMolar = tvMolar;
            this.tvH2Molar = tvH2Molar;
        }

        public void Recalculate()
        {
            double[] percent = new double[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                percent[i] = Num.Parse(inputs[i].Text);
            }
            double[] mwc = Calc.MwContributions(percent, molecularWeights);
            for (int i = 0; i < mwc.Length; i++)
            {
                contributions[i].Text = Num.Fixed(mwc[i], 4);
            }
            AvgMw = Calc.Sum(mwc);
            SumInput = Calc.Sum(percent);
            tvAvgMw.Text = Num.Fixed(AvgMw, 4);
            tvSum.Text = Num.Fixed(SumInput, 2);

            MolarFlow = Calc.MolarFlow(Num.Parse(flow.Text) * kgPerFlowUnit, AvgMw);
            tvMolar.Text = Num.Fixed(MolarFlow, 4);

            if (tvH2Molar != null)
            {
                H2MolarFlow = Calc.H2MolarFlow(MolarFlow, percent[0]);
                tvH2Molar.Text = Num.Fixed(H2MolarFlow, 4);
            }
        }
    }

    readonly List<TextBox> inputs;
    readonly StreamTable makeUp = null!;
    readonly StreamTable recycle = null!;
    readonly StreamTable feed = null!;
    double ratio;

    public event Action? Changed;

    public H2HcView()
    {
        InitializeComponent();
        inputs = ScreenHelper.Inputs(this).ToList();
        if (DesignerProperties.GetIsInDesignMode(this)) return;

        // Flow units (from the phone app): make-up in kg/hr, recycle and feed in t/hr.
        makeUp = new StreamTable(
            new[] { MkInH2, MkInC1, MkInC2, MkInC3, MkInIC4, MkInNC4, MkInIC5, MkInNC5 },
            new[] { MkMwH2, MkMwC1, MkMwC2, MkMwC3, MkMwIC4, MkMwNC4, MkMwIC5, MkMwNC5 },
            Calc.GasMw, MkFlow, 1.0, MkSum, MkAvg, MkMolar, MkH2Molar);
        recycle = new StreamTable(
            new[] { RcInH2, RcInC1, RcInC2, RcInC3, RcInIC4, RcInNC4, RcInIC5, RcInNC5 },
            new[] { RcMwH2, RcMwC1, RcMwC2, RcMwC3, RcMwIC4, RcMwNC4, RcMwIC5, RcMwNC5 },
            Calc.GasMw, RcFlow, 1000.0, RcSum, RcAvg, RcMolar, RcH2Molar);
        feed = new StreamTable(
            new[] { FdInIC4, FdInNC4, FdInIC5, FdInNC5, FdInDMB22, FdInCP, FdInDMB23, FdInMP2, FdInMP3, FdInNHEX, FdInDMP22, FdInMCP, FdInDMP24, FdInBEN, FdInCHEX, FdInMHEX2, FdInDMP23, FdInMHEX3, FdInCISDMCP, FdInTRDMCP, FdInNHEP, FdInMCH },
            new[] { FdMwIC4, FdMwNC4, FdMwIC5, FdMwNC5, FdMwDMB22, FdMwCP, FdMwDMB23, FdMwMP2, FdMwMP3, FdMwNHEX, FdMwDMP22, FdMwMCP, FdMwDMP24, FdMwBEN, FdMwCHEX, FdMwMHEX2, FdMwDMP23, FdMwMHEX3, FdMwCISDMCP, FdMwTRDMCP, FdMwNHEP, FdMwMCH },
            Calc.ProductMw, FdFlow, 1000.0, FdSum, FdAvg, FdMolar, null);

        ScreenHelper.Hook(this, Recalculate, () => Changed?.Invoke());
        Recalculate();
    }

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
        if (feed == null) return; // designer
        makeUp.Recalculate();
        recycle.Recalculate();
        feed.Recalculate();
        ratio = Calc.H2HcRatio(makeUp.H2MolarFlow, recycle.H2MolarFlow, feed.MolarFlow);
        TvRatio.Text = Num.Fixed(ratio, 4);
    }

    // Results for the Summary (valid after Refresh()).
    public double Ratio => ratio;
    public double MakeUpAvgMw => makeUp.AvgMw;
    public double RecycleAvgMw => recycle.AvgMw;
    public double FeedAvgMw => feed.AvgMw;
    public double MakeUpMolar => makeUp.MolarFlow;
    public double RecycleMolar => recycle.MolarFlow;
    public double FeedMolar => feed.MolarFlow;
    public double MakeUpH2Molar => makeUp.H2MolarFlow;
    public double RecycleH2Molar => recycle.H2MolarFlow;
}
