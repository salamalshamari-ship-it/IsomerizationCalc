using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace IsomerizationCalc;

/// <summary>Shows the summary page before printing; the print button opens the Windows print dialog.</summary>
public partial class PrintPreviewWindow : Window
{
    readonly SummaryRecord record;

    /// <summary>Message after a successful print (null when nothing was printed).</summary>
    public string? Result { get; private set; }

    public PrintPreviewWindow(SummaryRecord record)
    {
        InitializeComponent();
        this.record = record;
        ShowPages(SummaryPrinter.BuildA4(record));
    }

    /// <summary>Lays the document out page by page and shows every page as a white A4 sheet.</summary>
    void ShowPages(FlowDocument doc)
    {
        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        paginator.PageSize = new Size(doc.PageWidth, doc.PageHeight);
        paginator.ComputePageCount();

        for (int i = 0; i < paginator.PageCount; i++)
        {
            DocumentPage page = paginator.GetPage(i); // kept alive: the sheet draws from its visual
            var sheet = new Border
            {
                Width = doc.PageWidth,
                Height = doc.PageHeight,
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 18),
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.5 },
                Child = new Rectangle
                {
                    Width = page.Size.Width,
                    Height = page.Size.Height,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Fill = new VisualBrush(page.Visual) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }
                }
            };
            Pages.Children.Add(sheet);
        }
    }

    void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? msg = SummaryPrinter.Print(record);
            if (msg == null) return; // cancelled in the print dialog: stay in the preview
            Result = msg;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "تعذّرت الطباعة: " + ex.Message, "الطباعة",
                            MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK,
                            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }
    }

    void Close_Click(object sender, RoutedEventArgs e) => Close();
}
