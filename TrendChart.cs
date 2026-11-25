using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace IsomerizationCalc;

/// <summary>
/// A smooth line chart (a curve, not bars) of the values saved over time, like the phone app.
/// Points are spaced evenly in saving order. Moving the mouse (or clicking) selects the nearest
/// point and shows its date and value on top; the newest point is selected by default.
/// Colours come from App.xaml.
/// </summary>
public sealed class TrendChart : FrameworkElement
{
    double[] values = Array.Empty<double>();
    DateTime[] times = Array.Empty<DateTime>();
    string unit = "";
    int selected = -1;
    /// <summary>True when the last point is the figure typed now and not yet saved.</summary>
    bool lastIsCurrent;

    // Geometry, rebuilt at every draw.
    double left, right, top, bottom;
    double[] px = Array.Empty<double>();
    double[] py = Array.Empty<double>();
    double axisLow, axisHigh, axisStep;
    int axisDecimals;

    /// <param name="values">the figures, oldest first</param>
    /// <param name="times">when each was saved, same length as values</param>
    /// <param name="unit">shown after the value in the selected-point label, may be empty</param>
    /// <param name="lastIsCurrent">whether the last point is the current, unsaved figure</param>
    public void SetData(double[] values, DateTime[] times, string unit, bool lastIsCurrent)
    {
        this.values = (double[])values.Clone();
        this.times = (DateTime[])times.Clone();
        this.unit = unit;
        this.lastIsCurrent = lastIsCurrent;
        selected = this.values.Length - 1;
        InvalidateVisual();
    }

    string AxisLabel(double v) => v.ToString("F" + axisDecimals, CultureInfo.InvariantCulture);

    FormattedText Text(string text, Brush brush, double size, bool bold = false) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
                         bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

    void Rebuild(Brush muted)
    {
        int n = Math.Min(values.Length, times.Length);
        if (n == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            px = Array.Empty<double>();
            py = Array.Empty<double>();
            return;
        }

        double min = values[0], max = values[0];
        for (int i = 1; i < n; i++)
        {
            min = Math.Min(min, values[i]);
            max = Math.Max(max, values[i]);
        }
        (axisLow, axisHigh, axisStep) = Curve.NiceBounds(min, max, 4);
        axisDecimals = Curve.DecimalsFor(axisStep);

        double labelWidth = 0;
        for (double v = axisLow; v <= axisHigh + axisStep / 2; v += axisStep)
            labelWidth = Math.Max(labelWidth, Text(AxisLabel(v), muted, 11).Width);

        left = labelWidth + 12;
        right = ActualWidth - 12;
        top = 30;                    // room for the selected-point label
        bottom = ActualHeight - 26;  // room for the date labels

        px = new double[n];
        py = new double[n];
        double span = axisHigh - axisLow;
        for (int i = 0; i < n; i++)
        {
            px[i] = n == 1 ? (left + right) / 2 : left + (right - left) * i / (n - 1.0);
            py[i] = bottom - (values[i] - axisLow) / span * (bottom - top);
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w < 80 || h < 80) return;

        Brush ink = Ui.Res("Ink"), muted = Ui.Res("Muted"), accent = Ui.Res("Accent"),
              card = Ui.Res("Card"), outline = Ui.Res("Outline");

        // Transparent background so the whole area receives the mouse.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        Rebuild(muted);
        int n = px.Length;
        if (n == 0)
        {
            var empty = new FormattedText("لا توجد بيانات للرسم", CultureInfo.GetCultureInfo("ar"),
                FlowDirection.RightToLeft, new Typeface("Segoe UI"), 13, muted,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(empty, new Point((w - empty.Width) / 2, (h - empty.Height) / 2));
            return;
        }

        // Grid and value axis.
        var gridPen = new Pen(outline, 1);
        for (double v = axisLow; v <= axisHigh + axisStep / 2; v += axisStep)
        {
            double y = bottom - (v - axisLow) / (axisHigh - axisLow) * (bottom - top);
            dc.DrawLine(gridPen, new Point(left, y), new Point(right, y));
            var label = Text(AxisLabel(v), muted, 11);
            dc.DrawText(label, new Point(left - 6 - label.Width, y - label.Height / 2));
        }

        // Date axis: first and last point.
        var first = Text(times[0].ToString("MM-dd", CultureInfo.InvariantCulture), muted, 11);
        dc.DrawText(first, new Point(left, h - 6 - first.Height));
        if (n > 1)
        {
            var last = Text(lastIsCurrent ? "Now" : times[n - 1].ToString("MM-dd", CultureInfo.InvariantCulture), muted, 11);
            dc.DrawText(last, new Point(right - last.Width, h - 6 - last.Height));
        }

        // The smooth curve (monotone cubic through the points).
        if (n >= 2)
        {
            double[] m = Curve.Tangents(px, py);
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(px[0], py[0]), false, false);
                for (int i = 0; i < n - 1; i++)
                {
                    double dx = px[i + 1] - px[i];
                    ctx.BezierTo(new Point(px[i] + dx / 3, py[i] + m[i] * dx / 3),
                                 new Point(px[i + 1] - dx / 3, py[i + 1] - m[i + 1] * dx / 3),
                                 new Point(px[i + 1], py[i + 1]), true, true);
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, new Pen(accent, 2.5) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, geometry);
        }

        // Points: all of them while there are few, otherwise only the selected one.
        const double r = 3.5;
        if (n <= 40)
        {
            var dotPen = new Pen(accent, 2);
            for (int i = 0; i < n; i++)
            {
                if (i == selected) continue;
                dc.DrawEllipse(card, dotPen, new Point(px[i], py[i]), r, r);
            }
        }
        if (selected >= 0 && selected < n)
        {
            var guide = accent.Clone();
            guide.Opacity = 0.45;
            dc.DrawLine(new Pen(guide, 1), new Point(px[selected], top), new Point(px[selected], bottom));
            dc.DrawEllipse(accent, null, new Point(px[selected], py[selected]), r * 1.6, r * 1.6);

            var tip = Text(PointLabel(selected), ink, 13, bold: true);
            dc.DrawText(tip, new Point((w - tip.Width) / 2, 4));
        }
    }

    /// <summary>"date   value unit" for a point; the unsaved current point says so instead of a date.</summary>
    string PointLabel(int i)
    {
        string when = lastIsCurrent && i == values.Length - 1
            ? "Now (unsaved)"
            : times[i].ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        return when + "   " + values[i].ToString("F4", CultureInfo.InvariantCulture)
               + (unit.Length == 0 ? "" : " " + unit);
    }

    void Select(double x)
    {
        if (px.Length == 0) return;
        int best = 0;
        double bestDistance = double.MaxValue;
        for (int i = 0; i < px.Length; i++)
        {
            double d = Math.Abs(px[i] - x);
            if (d < bestDistance) { bestDistance = d; best = i; }
        }
        if (best != selected)
        {
            selected = best;
            InvalidateVisual();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Select(e.GetPosition(this).X);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Select(e.GetPosition(this).X);
    }
}
