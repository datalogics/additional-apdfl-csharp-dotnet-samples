using Datalogics.PDFL;
using PdfPath = Datalogics.PDFL.Path;

namespace FieldServiceInspectionReport;

/// <summary>Renders the inspection report by adding APDFL text, path, and image elements to pages.</summary>
internal sealed class PdfInspectionRenderer
{
    private const double Body = 8.5, Line = 11;

    // These font programs are supplied in APDFL's Resources/Font package and are embedded as subsets.
    private readonly Font _regular = new("MyriadPro-Regular", FontCreateFlags.Embedded | FontCreateFlags.Subset);
    private readonly Font _bold = new("MyriadPro-Bold", FontCreateFlags.Embedded | FontCreateFlags.Subset);

    private readonly PdfColor _navy = PdfColor.FromHex("#3D315B");
    private readonly PdfColor _teal = PdfColor.FromHex("#2D6A73");
    private readonly PdfColor _ink = PdfColor.FromHex("#293241");
    private readonly PdfColor _muted = PdfColor.FromHex("#68737E");
    private readonly PdfColor _pale = PdfColor.FromHex("#EEF0F7");
    private readonly PdfColor _rule = PdfColor.FromHex("#C9C4D8");
    private readonly PdfColor _white = new(1, 1, 1);

    private PdfDocument _doc = null!;

    public void Render(
        InspectionReport report,
        IReadOnlyList<Finding> findings,
        string imageDirectory,
        string output)
    {
        using PdfDocument doc = PdfDocument.Create(new PdfDocumentOptions
        {
            Title = report.Title,
            Producer = "CreateFieldServiceInspectionReport using APDFL",
        });

        _doc = doc;
        doc.PageStarted += DrawFooter;
        doc.NewPage();

        Header(report);
        Summary(report);
        FindingsTable(findings);
        Evidence(report, imageDirectory);
        Closing(report);
        doc.Save(output);
    }

    private void Header(InspectionReport report)
    {
        const double headerHeight = 104;
        double headerTop = _doc.PageTop;
        double titleX = _doc.Left + 20;
        double statusLeft = _doc.Right - 132;
        double titleWidth = statusLeft - titleX - 18;

        DrawRectangle(_doc.Left, headerTop, _doc.ContentWidth, headerHeight, Fill(_pale), Fill(_rule));
        DrawText("FIELD SERVICE INSPECTION REPORT", titleX, headerTop - 21, Style(_bold, 9.5, _teal));

        TextStyle titleStyle = Style(_bold, 18, _navy);
        IReadOnlyList<string> titleLines = _doc.Wrap(report.Title, titleStyle, titleWidth);
        double titleBaseline = headerTop - 45;

        _doc.Y = titleBaseline + titleStyle.Size;
        DrawSectionTitle(titleLines[0], titleStyle, spaceAfter: 0, x: titleX);

        for (int i = 1; i < titleLines.Count; i++)
            DrawText(titleLines[i], titleX, titleBaseline - (i * 20), titleStyle);

        double metadataY = titleBaseline - (titleLines.Count * 20) + 1;
        DrawText($"{report.ReportNumber}  |  Inspection date: {report.InspectionDate}",
            titleX, metadataY, Style(_regular, Body, _muted));

        PdfColor status = StatusColor(report.OverallStatus);
        double statusTop = headerTop - 54;
        DrawRectangle(statusLeft, statusTop, 116, 27, Fill(status), Fill(status));
        DrawText(report.OverallStatus.ToUpperInvariant(), statusLeft + 8, statusTop - 18, Style(_bold, 7.5, _white));

        _doc.Y = headerTop - headerHeight - 24;
    }

    private void Summary(InspectionReport report)
    {
        DrawSectionTitle("Inspection summary", Style(_bold, 13, _navy));

        double panelTop = _doc.Y;
        DrawRectangle(_doc.Left, panelTop, 250, 96, Fill(_white), Fill(_rule));
        DrawRectangle(_doc.Left + 266, panelTop, _doc.ContentWidth - 266, 96, Fill(_white), Fill(_rule));

        TextStyle label = Style(_bold, 8, _teal);
        TextStyle strong = Style(_bold, 11, _ink);
        TextStyle plain = Style(_regular, Body, _ink);
        TextStyle quiet = Style(_regular, Body, _muted);

        DrawText("CUSTOMER / SITE", _doc.Left + 14, panelTop - 18, label);
        DrawText(report.Customer.Name, _doc.Left + 14, panelTop - 38, strong);
        DrawText(report.Customer.Contact, _doc.Left + 14, panelTop - 54, quiet);
        DrawText(report.Site.Name, _doc.Left + 14, panelTop - 70, plain);
        DrawText(report.Site.Address, _doc.Left + 14, panelTop - 84, Style(_regular, 8, _muted));

        DrawText("INSPECTOR", _doc.Left + 280, panelTop - 18, label);
        DrawText(report.Inspector.Name, _doc.Left + 280, panelTop - 38, strong);
        DrawText(report.Inspector.Role, _doc.Left + 280, panelTop - 54, quiet);
        DrawText($"Equipment: {report.Site.EquipmentId}", _doc.Left + 280, panelTop - 76, plain);

        _doc.Y = panelTop - 122;
        DrawParagraph(report.Summary, plain, _doc.Left, _doc.ContentWidth, Line);
        Checklist();
    }

    private void Checklist()
    {
        DrawSectionTitle("Inspection checklist", Style(_bold, 11, _navy), spaceAfter: 6);
        TextStyle plain = Style(_regular, Body, _ink);
        string[] items =
        {
            "Review observed conditions and severity",
            "Confirm corrective action ownership",
            "Record photographic evidence for follow-up",
        };

        foreach (string item in items)
        {
            _doc.EnsureSpace(Line);
            DrawText("•", _doc.Left + 8, _doc.Y - plain.Size, plain);
            DrawText(item, _doc.Left + 20, _doc.Y - plain.Size, plain);
            _doc.Y -= Line;
        }

        _doc.Y -= 10;
    }

    private void FindingsTable(IReadOnlyList<Finding> findings)
    {
        DrawSectionTitle("Findings and corrective actions", Style(_bold, 13, _navy));

        double[] widths = { 40, 64, 70, 51, 163, 75, 53 };
        string[] headers = { "ID", "Category", "Location", "Severity", "Description", "Action", "Status" };
        const double headerHeight = 24;
        TextStyle headerStyle = Style(_bold, 7.5, _white);
        DrawTableHeader(headers, widths, headerStyle, headerHeight);

        TextStyle plain = Style(_regular, Body, _ink);
        TextStyle idStyle = Style(_bold, Body, _ink);
        foreach (Finding finding in findings)
        {
            string[] values =
            {
                finding.Id, finding.Category, finding.Location, finding.Severity,
                finding.Description, finding.RecommendedAction, finding.Status,
            };

            int lines = 1;
            for (int i = 0; i < values.Length; i++)
                lines = Math.Max(lines, _doc.Wrap(values[i], plain, widths[i] - 10).Count);

            double height = Math.Max(28, (lines * Line) + 10);
            if (_doc.Y - height < _doc.PageBottom)
                DrawTableHeaderOnNewPage(headers, widths, headerStyle, headerHeight);

            DrawRectangle(_doc.Left, _doc.Y + 5, Sum(widths), height, Fill(_white), Fill(_rule));
            double x = _doc.Left;
            for (int i = 0; i < values.Length; i++)
            {
                TextStyle style = i == 0
                    ? idStyle
                    : i == 3 ? Style(_regular, Body, StatusColor(finding.Severity)) : plain;
                DrawWrappedCell(values[i], x + 5, _doc.Y - 10, widths[i] - 10, height, style, Line);
                x += widths[i];
            }

            _doc.Y -= height;
        }
    }

    private void DrawTableHeaderOnNewPage(
        IReadOnlyList<string> headers,
        IReadOnlyList<double> widths,
        TextStyle style,
        double height)
    {
        _doc.NewPage();
        DrawTableHeader(headers, widths, style, height);
    }

    private void DrawTableHeader(
        IReadOnlyList<string> headers,
        IReadOnlyList<double> widths,
        TextStyle style,
        double height)
    {
        double totalWidth = Sum(widths);
        double top = _doc.Y;
        DrawRectangle(_doc.Left, top, totalWidth, height, Fill(_navy), Fill(_navy));

        // Draw the APDFL text elements after the band so the labels remain visible over its fill.
        double x = _doc.Left;
        for (int i = 0; i < headers.Count; i++)
        {
            DrawText(headers[i], x + 5, top - 15, style);
            x += widths[i];
        }

        _doc.Y -= height;
    }

    private void DrawWrappedCell(string value, double x, double top, double width, double height, TextStyle style, double leading)
    {
        double baseline = top;
        foreach (string line in _doc.Wrap(value, style, width))
        {
            if (top - baseline + leading > height)
                break;
            DrawText(line, x, baseline, style);
            baseline -= leading;
        }
    }

    private void Evidence(InspectionReport report, string imageDirectory)
    {
        if (report.Photographs is null || report.Photographs.Count == 0)
            return;

        _doc.NewPage();
        DrawSectionTitle("Photographic evidence", Style(_bold, 13, _navy));

        foreach (Photo photo in report.Photographs)
        {
            _doc.EnsureSpace(175);

            using Image image = new(System.IO.Path.Combine(imageDirectory, photo.File), _doc.Document);
            double scale = Math.Min(190 / image.Matrix.A, 125 / image.Matrix.D);
            image.Scale(scale, scale);

            double imageLeft = _doc.Left + 8;
            double imageBottom = _doc.Y - 130;
            image.Translate(imageLeft, imageBottom);

            // APDFL's Image is a page-content element; adding it places the scaled image in the PDF.
            _doc.CurrentPage.Content.AddElement(image);
            DrawText(photo.Caption, _doc.Left + 210, _doc.Y - 20, Style(_bold, 10, _ink));
            _doc.Y -= 160;
        }
    }

    private void Closing(InspectionReport report)
    {
        _doc.EnsureSpace(100);
        DrawSectionTitle("Closing certification", Style(_bold, 13, _navy));

        string text = report.CertificationText
            ?? "The inspection record above reflects the conditions observed at the time of "
             + "inspection. Follow-up actions should be tracked through the responsible service process.";

        DrawParagraph(text, Style(_regular, Body, _ink), _doc.Left, _doc.ContentWidth, Line);
        DrawText($"Prepared by {report.Inspector.Name}  |  APDFL sample output",
            _doc.Left, _doc.Y - 12, Style(_regular, 8, _muted));
    }

    private void DrawFooter(PdfDocument doc)
    {
        TextStyle style = Style(_regular, 7.5, _muted);
        DrawRule(doc.Left, doc.PageBottom - 26, doc.ContentWidth, Fill(_rule));
        DrawText("Northstar Facilities  |  Confidential field record", doc.Left, doc.PageBottom - 40, style);
        DrawText($"Page {doc.PageNumber}", doc.Right - 40, doc.PageBottom - 40, style);
    }

    private void DrawSectionTitle(string text, TextStyle style, double spaceAfter = 10, double? x = null)
    {
        _doc.EnsureSpace(style.Size * 2.5);
        DrawText(text, x ?? _doc.Left, _doc.Y - style.Size, style);
        _doc.Y -= style.Size + spaceAfter;
    }

    private void DrawParagraph(string text, TextStyle style, double x, double width, double leading, double spaceAfter = 6)
    {
        foreach (string line in _doc.Wrap(text, style, width))
        {
            _doc.EnsureSpace(leading);
            DrawText(line, x, _doc.Y - style.Size, style);
            _doc.Y -= leading;
        }

        _doc.Y -= spaceAfter;
    }

    private void DrawText(string value, double x, double baseline, TextStyle style)
    {
        // APDFL Text and TextRun objects place measured glyph runs into the current page content.
        GraphicState graphics = new() { FillColor = style.Color };
        TextRun run = new(value, style.Font, graphics, new TextState(), new Matrix(style.Size, 0, 0, style.Size, x, baseline));
        Datalogics.PDFL.Text text = new();
        text.AddRun(run);
        _doc.CurrentPage.Content.AddElement(text);
    }

    private void DrawRectangle(double x, double top, double width, double height, Color fill, Color stroke, double lineWidth = 0.5)
    {
        // APDFL Path geometry is added to page content and painted with the supplied colors.
        PdfPath path = new()
        {
            GraphicState = new GraphicState { FillColor = fill, StrokeColor = stroke, Width = lineWidth },
            PaintOp = PathPaintOpFlags.Fill | PathPaintOpFlags.Stroke,
        };
        path.AddRect(new Point(x, top - height), width, height);
        _doc.CurrentPage.Content.AddElement(path);
    }

    private void DrawRule(double x, double y, double width, Color color)
    {
        PdfPath path = new()
        {
            GraphicState = new GraphicState { FillColor = color, StrokeColor = color, Width = 0.5 },
            PaintOp = PathPaintOpFlags.Fill,
        };
        path.AddRect(new Point(x, y), width, 0.5);
        _doc.CurrentPage.Content.AddElement(path);
    }

    private static double Sum(IReadOnlyList<double> values)
    {
        double total = 0;
        foreach (double value in values)
            total += value;
        return total;
    }

    private static TextStyle Style(Font font, double size, PdfColor color) => new(font, size, color.ToPdfColor());
    private static Color Fill(PdfColor color) => color.ToPdfColor();

    private static PdfColor StatusColor(string value) =>
        value.Contains("Critical", StringComparison.OrdinalIgnoreCase) ? PdfColor.FromHex("#B34A43")
        : value.Contains("Attention", StringComparison.OrdinalIgnoreCase)
          || value.Contains("High", StringComparison.OrdinalIgnoreCase) ? PdfColor.FromHex("#D48632")
        : PdfColor.FromHex("#2C8A72");
}
