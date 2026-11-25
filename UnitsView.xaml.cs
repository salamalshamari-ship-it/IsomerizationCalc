using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace IsomerizationCalc;

/// <summary>
/// Unit converter tab. Every unit is stored as two functions (to / from a base unit of its
/// category), which covers ordinary units and also temperature and API gravity.
/// </summary>
public partial class UnitsView : UserControl
{
    sealed class Unit
    {
        public string Name { get; }
        public override string ToString() => Name;
        public Func<double, double> ToBase { get; }
        public Func<double, double> FromBase { get; }

        public Unit(string name, Func<double, double> toBase, Func<double, double> fromBase)
        {
            Name = name;
            ToBase = toBase;
            FromBase = fromBase;
        }

        /// <summary>base = value x factor.</summary>
        public static Unit Lin(string name, double factor) =>
            new(name, v => v * factor, b => b / factor);
    }

    sealed class Category
    {
        public string Name { get; }
        public override string ToString() => Name;
        public Unit[] Units { get; }
        public string Note { get; }

        public Category(string name, string note, params Unit[] units)
        {
            Name = name;
            Note = note;
            Units = units;
        }
    }

    const double WaterKgM3 = 999.016; // water at 60 F, used for specific gravity

    static readonly Category[] Categories =
    {
        new("Temperature", "",
            new Unit("°C", c => c + 273.15, k => k - 273.15),
            new Unit("°F", f => (f - 32) * 5.0 / 9.0 + 273.15, k => (k - 273.15) * 9.0 / 5.0 + 32),
            new Unit("K", k => k, k => k),
            new Unit("°R (Rankine)", r => r * 5.0 / 9.0, k => k * 9.0 / 5.0)),

        new("Pressure", "Gauge and absolute pressure are not distinguished: add the atmosphere yourself when needed.",
            Unit.Lin("bar", 1e5), Unit.Lin("kPa", 1e3), Unit.Lin("MPa", 1e6), Unit.Lin("Pa", 1),
            Unit.Lin("mbar", 100), Unit.Lin("psi", 6894.757293168), Unit.Lin("kg/cm²", 98066.5),
            Unit.Lin("atm", 101325), Unit.Lin("mmHg (Torr)", 133.322387415),
            Unit.Lin("inHg", 3386.389), Unit.Lin("mmH₂O", 9.80665), Unit.Lin("inH₂O", 249.0889)),

        new("Volumetric flow", "",
            Unit.Lin("m³/h", 1.0 / 3600), Unit.Lin("m³/day", 1.0 / 86400), Unit.Lin("m³/s", 1),
            Unit.Lin("L/min", 1e-3 / 60), Unit.Lin("L/h", 1e-3 / 3600), Unit.Lin("L/s", 1e-3),
            Unit.Lin("bbl/day", 0.158987294928 / 86400), Unit.Lin("bbl/h", 0.158987294928 / 3600),
            Unit.Lin("gal/min (US)", 0.003785411784 / 60), Unit.Lin("gal/h (US)", 0.003785411784 / 3600),
            Unit.Lin("ft³/h", 0.028316846592 / 3600), Unit.Lin("ft³/min", 0.028316846592 / 60)),

        new("Mass flow", "",
            Unit.Lin("kg/h", 1.0 / 3600), Unit.Lin("ton/h", 1000.0 / 3600), Unit.Lin("ton/day", 1000.0 / 86400),
            Unit.Lin("kg/s", 1), Unit.Lin("kg/day", 1.0 / 86400), Unit.Lin("g/s", 1e-3),
            Unit.Lin("lb/h", 0.45359237 / 3600), Unit.Lin("lb/s", 0.45359237), Unit.Lin("lb/day", 0.45359237 / 86400),
            Unit.Lin("short ton/day", 907.18474 / 86400)),

        new("Density", "SG and °API are taken at 60 °F (water = 999.016 kg/m³).",
            Unit.Lin("kg/m³", 1), Unit.Lin("g/cm³", 1000), Unit.Lin("ton/m³", 1000), Unit.Lin("kg/L", 1000),
            Unit.Lin("g/L", 1), Unit.Lin("lb/ft³", 16.01846337), Unit.Lin("lb/gal (US)", 119.826427),
            Unit.Lin("lb/bbl", 2.853010174),
            Unit.Lin("SG (60/60 °F)", WaterKgM3),
            new Unit("°API",
                api => 141.5 / (api + 131.5) * WaterKgM3,
                kg => kg > 0 ? 141.5 / (kg / WaterKgM3) - 131.5 : double.NaN)),

        new("Volume", "",
            Unit.Lin("m³", 1), Unit.Lin("L", 1e-3), Unit.Lin("mL", 1e-6), Unit.Lin("bbl (oil)", 0.158987294928),
            Unit.Lin("gal (US)", 0.003785411784), Unit.Lin("gal (UK)", 0.00454609),
            Unit.Lin("ft³", 0.028316846592), Unit.Lin("in³", 1.6387064e-5), Unit.Lin("yd³", 0.764554857984)),

        new("Mass", "",
            Unit.Lin("kg", 1), Unit.Lin("ton (metric)", 1000), Unit.Lin("g", 1e-3), Unit.Lin("mg", 1e-6),
            Unit.Lin("lb", 0.45359237), Unit.Lin("oz", 0.028349523125),
            Unit.Lin("short ton (US)", 907.18474), Unit.Lin("long ton (UK)", 1016.0469088)),

        new("Length", "",
            Unit.Lin("m", 1), Unit.Lin("cm", 1e-2), Unit.Lin("mm", 1e-3), Unit.Lin("km", 1e3), Unit.Lin("µm", 1e-6),
            Unit.Lin("in", 0.0254), Unit.Lin("ft", 0.3048), Unit.Lin("yd", 0.9144), Unit.Lin("mile", 1609.344)),

        new("Area", "",
            Unit.Lin("m²", 1), Unit.Lin("cm²", 1e-4), Unit.Lin("mm²", 1e-6), Unit.Lin("km²", 1e6),
            Unit.Lin("ha", 1e4), Unit.Lin("in²", 6.4516e-4), Unit.Lin("ft²", 0.09290304),
            Unit.Lin("yd²", 0.83612736), Unit.Lin("acre", 4046.8564224)),

        new("Energy", "",
            Unit.Lin("kJ", 1e3), Unit.Lin("J", 1), Unit.Lin("MJ", 1e6), Unit.Lin("GJ", 1e9),
            Unit.Lin("kcal", 4184), Unit.Lin("cal", 4.184), Unit.Lin("BTU", 1055.05585262),
            Unit.Lin("kWh", 3.6e6), Unit.Lin("therm", 1.05505585e8), Unit.Lin("ft·lbf", 1.3558179483)),

        new("Power", "",
            Unit.Lin("kW", 1e3), Unit.Lin("W", 1), Unit.Lin("MW", 1e6),
            Unit.Lin("hp (mechanical)", 745.69987158), Unit.Lin("hp (metric, PS)", 735.49875),
            Unit.Lin("BTU/h", 0.29307107), Unit.Lin("kcal/h", 1.162222), Unit.Lin("MMBTU/h", 293071.07)),

        new("Heating value", "Energy per unit mass (fuel).",
            Unit.Lin("kcal/kg", 4184), Unit.Lin("kJ/kg", 1e3), Unit.Lin("MJ/kg", 1e6),
            Unit.Lin("BTU/lb", 2326), Unit.Lin("J/kg", 1)),

        new("Dynamic viscosity", "",
            Unit.Lin("cP", 1e-3), Unit.Lin("Pa·s", 1), Unit.Lin("mPa·s", 1e-3), Unit.Lin("P (poise)", 0.1),
            Unit.Lin("lb/(ft·s)", 1.488164)),

        new("Kinematic viscosity", "",
            Unit.Lin("cSt", 1e-6), Unit.Lin("m²/s", 1), Unit.Lin("mm²/s", 1e-6), Unit.Lin("St", 1e-4),
            Unit.Lin("ft²/s", 0.09290304)),

        new("Speed", "",
            Unit.Lin("m/s", 1), Unit.Lin("km/h", 1.0 / 3.6), Unit.Lin("ft/s", 0.3048),
            Unit.Lin("mph", 0.44704), Unit.Lin("knot", 0.514444)),

        new("Time", "",
            Unit.Lin("s", 1), Unit.Lin("min", 60), Unit.Lin("h", 3600), Unit.Lin("day", 86400),
            Unit.Lin("week", 604800))
    };

    bool busy;

    public UnitsView()
    {
        InitializeComponent();

        CategoryBox.ItemsSource = Categories;
        CategoryBox.SelectionChanged += (_, _) => CategoryChanged();
        FromBox.SelectionChanged += (_, _) => Convert();
        ToBox.SelectionChanged += (_, _) => Convert();
        ValueBox.TextChanged += (_, _) => Convert();

        CategoryBox.SelectedIndex = 0; // Temperature: °C → °F
    }

    void CategoryChanged()
    {
        if (CategoryBox.SelectedItem is not Category cat) return;
        busy = true;
        try
        {
            FromBox.ItemsSource = cat.Units;
            ToBox.ItemsSource = cat.Units;
            FromBox.SelectedIndex = 0;
            ToBox.SelectedIndex = cat.Units.Length > 1 ? 1 : 0;
            TvNote.Text = cat.Note;
        }
        finally { busy = false; }
        Convert();
    }

    void Swap_Click(object sender, RoutedEventArgs e)
    {
        int a = FromBox.SelectedIndex, b = ToBox.SelectedIndex;
        busy = true;
        try
        {
            FromBox.SelectedIndex = b;
            ToBox.SelectedIndex = a;
        }
        finally { busy = false; }
        Convert();
    }

    static string Format(double v) =>
        double.IsFinite(v) ? v.ToString("G7", CultureInfo.InvariantCulture) : "—";

    void Convert()
    {
        if (busy || CategoryBox.SelectedItem is not Category cat) return;
        if (FromBox.SelectedItem is not Unit from || ToBox.SelectedItem is not Unit to) return;

        string text = Num.Normalize(ValueBox.Text);
        if (text.Length == 0
            || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
            || !double.IsFinite(x))
        {
            TvResult.Text = "—";
            TvAll.Text = "";
            return;
        }

        double baseValue = from.ToBase(x);
        TvResult.Text = Format(to.FromBase(baseValue));

        var sb = new StringBuilder();
        foreach (var unit in cat.Units)
            sb.Append(unit.Name.PadRight(18)).Append(Format(unit.FromBase(baseValue))).AppendLine();
        TvAll.Text = sb.ToString().TrimEnd();
    }
}
