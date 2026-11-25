using System.ComponentModel;
using System.Globalization;

namespace IsomerizationCalc;

/// <summary>One line of the saved-records list in the Summary tab (with its check mark).</summary>
public sealed class RecordRow : INotifyPropertyChanged
{
    bool isChecked;

    public RecordRow(SummaryRecord record) { Record = record; }

    public SummaryRecord Record { get; }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked == value) return;
            isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    string F(int index, int decimals) =>
        Record.Results[index].ToString("F" + decimals, CultureInfo.InvariantCulture);

    public string Date => Record.SavedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    public string Yield => F(SummaryRecord.IxYield, 2);
    public string Ron => F(SummaryRecord.IxRon, 2);
    public string H2Hc => F(SummaryRecord.IxH2Hc, 3);

    public event PropertyChangedEventHandler? PropertyChanged;
}
