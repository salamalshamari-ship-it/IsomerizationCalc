namespace IsomerizationCalc;

/// <summary>The maths behind the trend chart (port of Curve.java).</summary>
static class Curve
{
    /// <summary>
    /// Slopes for a smooth curve through the points that never rises above or falls below the data
    /// between two points (monotone cubic interpolation, Fritsch-Carlson): a peak in the line is
    /// always a real saved value.
    /// </summary>
    public static double[] Tangents(double[] x, double[] y)
    {
        int n = x.Length;
        var m = new double[n];
        if (n < 2) return m;

        var delta = new double[n - 1];
        for (int k = 0; k < n - 1; k++) delta[k] = (y[k + 1] - y[k]) / (x[k + 1] - x[k]);

        m[0] = delta[0];
        m[n - 1] = delta[n - 2];
        for (int k = 1; k < n - 1; k++)
            m[k] = delta[k - 1] * delta[k] <= 0 ? 0 : (delta[k - 1] + delta[k]) / 2.0;

        for (int k = 0; k < n - 1; k++)
        {
            if (delta[k] == 0)
            {
                m[k] = 0;
                m[k + 1] = 0;
                continue;
            }
            double a = m[k] / delta[k];
            double b = m[k + 1] / delta[k];
            double s = a * a + b * b;
            if (s > 9)
            {
                double tau = 3.0 / Math.Sqrt(s);
                m[k] = tau * a * delta[k];
                m[k + 1] = tau * b * delta[k];
            }
        }
        return m;
    }

    /// <summary>
    /// A tidy axis range around the data: (low, high, step). The step is 1, 2 or 5 times a power of ten.
    /// When the data is never negative the axis does not go below zero.
    /// </summary>
    public static (double Low, double High, double Step) NiceBounds(double min, double max, int targetSteps)
    {
        if (!(max >= min)) { min = 0; max = 1; }
        double dataMin = min;
        double range = max - min;
        if (range < 1e-12)
        {
            double pad = Math.Max(Math.Abs(min) * 0.05, 0.5);
            min -= pad;
            max += pad;
        }
        else
        {
            min -= range * 0.1;
            max += range * 0.1;
        }
        double raw = (max - min) / Math.Max(1, targetSteps);
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / magnitude;
        double step = (norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10) * magnitude;

        double lo = Math.Floor(min / step + 1e-9) * step;
        double hi = Math.Ceiling(max / step - 1e-9) * step;
        if (dataMin >= 0 && lo < 0) lo = 0;
        return (lo, hi, step);
    }

    /// <summary>Decimal places needed to print multiples of <paramref name="step"/> exactly.</summary>
    public static int DecimalsFor(double step)
    {
        if (step <= 0) return 2;
        int d = (int)Math.Ceiling(-Math.Log10(step) - 1e-9);
        return Math.Max(0, Math.Min(6, d));
    }
}
