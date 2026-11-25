namespace IsomerizationCalc;

/// <summary>
/// All the unit calculations: a line-by-line port of Calc.java. Pure functions, no UI.
/// </summary>
static class Calc
{
    public const double HoursPerDay = 24.0;

    // ===================================================================================
    // Production
    // ===================================================================================

    /// <summary>Volume per day (m3/day) from a mass flow in ton/day and a density in ton/m3.</summary>
    public static double DailyVolumeM3(double tonPerDay, double density) =>
        density > 0 ? tonPerDay / density : 0;

    /// <summary>Volume over the operating period (full day, or partial: hours/24 of a day).</summary>
    public static double VolumeM3(double tonPerDay, double density, bool partial, double hours)
    {
        double daily = DailyVolumeM3(tonPerDay, density);
        return partial ? daily / HoursPerDay * hours : daily;
    }

    /// <summary>Hourly rate (m3/h) = ((ton/day) / density) / 24.</summary>
    public static double HourlyRateM3(double tonPerDay, double density) =>
        DailyVolumeM3(tonPerDay, density) / HoursPerDay;

    /// <summary>Yield (%) = total production volume / feed volume x 100.</summary>
    public static double YieldPercent(double productM3, double feedM3) =>
        feedM3 > 0 ? productM3 / feedM3 * 100.0 : 0;

    // ===================================================================================
    // RON / PIN
    // ===================================================================================

    /// <summary>Reference RON of the 15 components, in the order of the table.</summary>
    public static readonly double[] RonRef =
    {
        100.2, 95.0, 93.5, 61.7, 102.3, 94.0, 105.0,
        74.4, 75.5, 31.0, 96.0, 120.0, 84.0, 55.0, 76.0
    };

    public const int IdxIc5 = 2;
    public const int IdxNc5 = 3;
    public const int Idx22Dmb = 5;
    public const int Idx23Dmb = 6;
    public const int Idx2Mp = 7;
    public const int Idx3Mp = 8;
    public const int IdxNc6 = 9;
    public const int IdxP7Plus = 13;
    public const int IdxN7Plus = 14;

    public sealed class RonResult
    {
        /// <summary>Area / 100 for each component (N7+ already corrected).</summary>
        public double[] Fraction { get; }
        /// <summary>REF x fraction for each component.</summary>
        public double[] Contribution { get; }
        /// <summary>Sum of the corrected areas, should be close to 100.</summary>
        public double AreaSum { get; }
        public double Ron { get; }
        public double Pin { get; }

        public RonResult(double[] fraction, double[] contribution, double areaSum, double ron, double pin)
        {
            Fraction = fraction;
            Contribution = contribution;
            AreaSum = areaSum;
            Ron = ron;
            Pin = pin;
        }
    }

    /// <param name="area">the 15 areas as typed, in table order</param>
    /// <param name="n7Entered">
    /// whether the N7+ field has been filled. When it has, N7+ is corrected to N7+ - P7+ for the
    /// calculation; the typed value itself is never modified.
    /// </param>
    public static RonResult Ron(double[] area, bool n7Entered)
    {
        int n = RonRef.Length;
        double[] eff = (double[])area.Clone();
        if (n7Entered)
        {
            eff[IdxN7Plus] = area[IdxN7Plus] - area[IdxP7Plus];
        }

        double[] fraction = new double[n];
        double[] contribution = new double[n];
        double ron = 0;
        double areaSum = 0;
        for (int i = 0; i < n; i++)
        {
            fraction[i] = eff[i] / 100.0;
            contribution[i] = RonRef[i] * fraction[i];
            ron += contribution[i];
            areaSum += eff[i];
        }
        return new RonResult(fraction, contribution, areaSum, ron, Pin(area));
    }

    /// <summary>PIN = iP5/(iP5+nP5) + 2,2DMB/SumP6 + 2,3DMB/SumP6, in %.</summary>
    public static double Pin(double[] area)
    {
        double ip5 = area[IdxIc5];
        double sumP5 = ip5 + area[IdxNc5];
        double dmb22 = area[Idx22Dmb];
        double dmb23 = area[Idx23Dmb];
        double sumP6 = dmb22 + dmb23 + area[Idx2Mp] + area[Idx3Mp] + area[IdxNc6];

        double pin = 0;
        if (sumP5 > 0) pin += ip5 / sumP5;
        if (sumP6 > 0) pin += dmb22 / sumP6 + dmb23 / sumP6;
        return pin * 100.0;
    }

    // ===================================================================================
    // X. Factor / LHSV
    // ===================================================================================

    /// <summary>X. Factor = CHX + Benzene + C7+ (wt%).</summary>
    public static double XFactor(double chx, double benzene, double c7plus) => chx + benzene + c7plus;

    /// <summary>LHSV (1/h) = (flow ton/h / density ton/m3) / catalyst volume m3.</summary>
    public static double Lhsv(double densityTonPerM3, double flowTonPerH, double catalystM3)
    {
        if (densityTonPerM3 <= 0 || catalystM3 <= 0) return 0;
        return flowTonPerH / densityTonPerM3 / catalystM3;
    }

    // ===================================================================================
    // H2/HC ratio
    // ===================================================================================

    /// <summary>Molecular weights: H2, C1, C2, C3, iC4, nC4, iC5, nC5 (make-up and recycle gas).</summary>
    public static readonly double[] GasMw = { 2.016, 16.04, 30.07, 44.10, 58.12, 58.12, 72.15, 72.15 };

    /// <summary>
    /// Molecular weights of the 22 feed components, in the order of the GC report: iC4, nC4, iC5, nC5,
    /// 2.2DMB, CP, 2.3DMB, 2MP, 3MP, nC6, 2.2DMP, MCP, 2.4DMP, Benzene, CHX, 2MHex, 2.3DMP, 3MHex,
    /// cis-1.3-DMCP, trans-1.3-DMCP, nC7, MCH.
    /// </summary>
    public static readonly double[] ProductMw =
    {
        58.12, 58.12, 72.15, 72.15, 86.18, 70.13,
        86.18, 86.18, 86.18, 86.18, 100.20,
        84.16, 100.20, 78.11, 84.16, 100.20,
        100.20, 100.20, 98.19,
        98.19, 100.20, 98.19
    };

    /// <summary>(percent / 100) x MW for each component.</summary>
    public static double[] MwContributions(double[] percent, double[] mw)
    {
        double[] result = new double[mw.Length];
        for (int i = 0; i < mw.Length; i++)
        {
            result[i] = percent[i] / 100.0 * mw[i];
        }
        return result;
    }

    public static double Sum(double[] values)
    {
        double s = 0;
        foreach (double v in values) s += v;
        return s;
    }

    /// <summary>kmol/h = (kg/h) / (kg/kmol).</summary>
    public static double MolarFlow(double massKgPerH, double avgMw) =>
        avgMw > 0 ? massKgPerH / avgMw : 0;

    public static double H2MolarFlow(double molarFlow, double h2Percent) =>
        molarFlow * (h2Percent / 100.0);

    /// <summary>H2/HC = (H2 make-up + H2 recycle) / feed molar flow.</summary>
    public static double H2HcRatio(double h2MakeUp, double h2Recycle, double feedMolar) =>
        feedMolar > 0 ? (h2MakeUp + h2Recycle) / feedMolar : 0;
}
