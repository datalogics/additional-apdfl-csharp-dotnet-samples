using Datalogics.PDFL;

namespace FieldServiceInspectionReport;

/// <summary>
/// Builds the inspection report using ordinary PDF page content. This sample intentionally
/// does not create a PDF structure tree or tagged content.
/// </summary>
internal sealed class PdfInspectionRenderer
{
    private const double Body = 8.5, Line = 11;

    private readonly Font _regular = new("Times-Roman", FontCreateFlags.Subset);
    private readonly Font _bold = new("Times-Bold", FontCreateFlags.Subset);

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

        // The title section stays open for the whole body, so every section below nests
        // inside it and its headings come out one level down.
        using (Header(report))
        {
            Summary(report);
            FindingsTable(findings);
            Evidence(report, imageDirectory);
            Closing(report);
        }

        doc.Save(output);
    }

    /// <summary>Draws the masthead and opens the document's top-level section, which the caller closes.</summary>
    private IDisposable Header(InspectionReport report)
    {
        const double headerHeight = 104;
        double headerTop = _doc.PageTop;
        double titleX = _doc.Left + 20;
        double statusLeft = _doc.Right - 132;
        double titleWidth = statusLeft - titleX - 18;

        _doc.ArtifactRect(_doc.Left, headerTop, _doc.ContentWidth, headerHeight, Fill(_pale), Fill(_rule));

        // The masthead label restates the document type rather than adding content, so it is
        // decoration: an artifact, not a structure element.
        _doc.ArtifactText("FIELD SERVICE INSPECTION REPORT", titleX, headerTop - 21, Style(_bold, 9.5, _teal), "Header");

        TextStyle titleStyle = Style(_bold, 18, _navy);
        IReadOnlyList<string> titleLines = _doc.Wrap(report.Title, titleStyle, titleWidth);
        double titleBaseline = headerTop - 45;

        _doc.Y = titleBaseline + titleStyle.Size;
        IDisposable titleSection = _doc.Section(titleLines[0], titleStyle, spaceAfter: 0, x: titleX);

        for (int i = 1; i < titleLines.Count; i++)
        {
            _doc.TextBlock("P", titleLines[i], titleX, titleBaseline - (i * 20), titleStyle);
        }

        double metadataY = titleBaseline - (titleLines.Count * 20) + 1;
        _doc.TextBlock(
            "P",
            $"{report.ReportNumber}  |  Inspection date: {report.InspectionDate}",
            titleX,
            metadataY,
            Style(_regular, Body, _muted));

        PdfColor status = StatusColor(report.OverallStatus);
        double statusTop = headerTop - 54;
        _doc.ArtifactRect(statusLeft, statusTop, 116, 27, Fill(status), Fill(status));
        _doc.TextBlock("P", report.OverallStatus.ToUpperInvariant(), statusLeft + 8, statusTop - 18, Style(_bold, 7.5, _white));

        _doc.Y = headerTop - headerHeight - 24;
        return titleSection;
    }

    private void Summary(InspectionReport report)
    {
        using IDisposable section = _doc.Section("Inspection summary", Style(_bold, 13, _navy));

        double panelTop = _doc.Y;
        _doc.ArtifactRect(_doc.Left, panelTop, 250, 96, Fill(_white), Fill(_rule));
        _doc.ArtifactRect(_doc.Left + 266, panelTop, _doc.ContentWidth - 266, 96, Fill(_white), Fill(_rule));

        TextStyle label = Style(_bold, 8, _teal);
        TextStyle strong = Style(_bold, 11, _ink);
        TextStyle plain = Style(_regular, Body, _ink);
        TextStyle quiet = Style(_regular, Body, _muted);

        _doc.TextBlock("P", "CUSTOMER / SITE", _doc.Left + 14, panelTop - 18, label);
        _doc.TextBlock("P", report.Customer.Name, _doc.Left + 14, panelTop - 38, strong);
        _doc.TextBlock("P", report.Customer.Contact, _doc.Left + 14, panelTop - 54, quiet);
        _doc.TextBlock("P", report.Site.Name, _doc.Left + 14, panelTop - 70, plain);
        _doc.TextBlock("P", report.Site.Address, _doc.Left + 14, panelTop - 84, Style(_regular, 8, _muted));

        _doc.TextBlock("P", "INSPECTOR", _doc.Left + 280, panelTop - 18, label);
        _doc.TextBlock("P", report.Inspector.Name, _doc.Left + 280, panelTop - 38, strong);
        _doc.TextBlock("P", report.Inspector.Role, _doc.Left + 280, panelTop - 54, quiet);
        _doc.TextBlock("P", $"Equipment: {report.Site.EquipmentId}", _doc.Left + 280, panelTop - 76, plain);

        _doc.Y = panelTop - 122;
        _doc.Paragraph(report.Summary, plain, _doc.Left, _doc.ContentWidth, Line);

        Checklist();
    }

    private void Checklist()
    {
        using IDisposable section = _doc.Section("Inspection checklist", Style(_bold, 11, _navy), spaceAfter: 6);
        using PdfDocument.ListScope list = _doc.List(ListNumbering.Disc);

        TextStyle plain = Style(_regular, Body, _ink);
        string[] items =
        {
            "Review observed conditions and severity",
            "Confirm corrective action ownership",
            "Record photographic evidence for follow-up",
        };

        foreach (string item in items)
        {
            // The marker is a real Lbl, not a hyphen glued onto the text.
            list.Item("•", item, plain, _doc.Left + 8, labelWidth: 12, leading: Line);
        }

        _doc.Y -= 10;
    }

    private void FindingsTable(IReadOnlyList<Finding> findings)
    {
        using IDisposable section = _doc.Section("Findings and corrective actions", Style(_bold, 13, _navy));

        double[] widths = { 40, 64, 70, 51, 163, 75, 53 };
        string[] headers = { "ID", "Category", "Location", "Severity", "Description", "Action", "Status" };

        double bandTop = _doc.Y;
        using PdfDocument.TableScope table = _doc.Table(headers, widths, Style(_bold, 7.5, _white), headerHeight: 24);
        table.HeaderBackground = Fill(_navy);
        table.DrawHeaderBand(bandTop);

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
            {
                lines = Math.Max(lines, _doc.Wrap(values[i], plain, widths[i] - 10).Count);
            }

            double height = Math.Max(28, (lines * Line) + 10);
            PdfDocument.RowScope row = table.Row(height);

            _doc.ArtifactRect(_doc.Left, _doc.Y + 5, Sum(widths), height, Fill(_white), Fill(_rule));

            double x = _doc.Left;
            for (int i = 0; i < values.Length; i++)
            {
                TextStyle style = i == 0
                    ? idStyle
                    : i == 3 ? Style(_regular, Body, StatusColor(finding.Severity)) : plain;

                // Column zero identifies the row, so it is a TH with /Scope Row.
                row.Cell(values[i], i, x, _doc.Y - 10, style, Line, isRowHeader: i == 0);
                x += widths[i];
            }

            _doc.Y -= height;
        }
    }

    private void Evidence(InspectionReport report, string imageDirectory)
    {
        if (report.Photographs is null || report.Photographs.Count == 0)
        {
            return;
        }

        _doc.NewPage();
        using IDisposable section = _doc.Section("Photographic evidence", Style(_bold, 13, _navy));

        foreach (Photo photo in report.Photographs)
        {
            _doc.EnsureSpace(175);

            using Image image = new(System.IO.Path.Combine(imageDirectory, photo.File), _doc.PdfDocumentHandle);
            double scale = Math.Min(190 / image.Matrix.A, 125 / image.Matrix.D);
            image.Scale(scale, scale);

            double imageLeft = _doc.Left + 8;
            double imageBottom = _doc.Y - 130;
            image.Translate(imageLeft, imageBottom);

            double textLeft = _doc.Left + 210;
            double captionBaseline = _doc.Y - 20;

            _doc.Image(image);
            _doc.Caption(photo.Caption, textLeft, captionBaseline, Style(_bold, 10, _ink));

            _doc.Y -= 160;
        }
    }

    private void Closing(InspectionReport report)
    {
        _doc.EnsureSpace(100);
        using IDisposable section = _doc.Section("Closing certification", Style(_bold, 13, _navy));

        string text = report.CertificationText
            ?? "The inspection record above reflects the conditions observed at the time of "
             + "inspection. Follow-up actions should be tracked through the responsible service process.";

        _doc.Paragraph(text, Style(_regular, Body, _ink), _doc.Left, _doc.ContentWidth, Line);
        _doc.TextBlock(
            "P",
            $"Prepared by {report.Inspector.Name}  |  APDFL sample output",
            _doc.Left,
            _doc.Y - 12,
            Style(_regular, 8, _muted));
    }

    /// <summary>Running foot: pagination artifacts, so it stays out of the reading order.</summary>
    private void DrawFooter(PdfDocument doc)
    {
        TextStyle style = Style(_regular, 7.5, _muted);
        doc.ArtifactRule(doc.Left, doc.PageBottom - 26, doc.ContentWidth, Fill(_rule));
        doc.ArtifactText("Northstar Facilities  |  Confidential field record", doc.Left, doc.PageBottom - 40, style);
        doc.ArtifactText($"Page {doc.PageNumber}", doc.Right - 40, doc.PageBottom - 40, style);
    }

    private static double Sum(IReadOnlyList<double> values)
    {
        double total = 0;
        foreach (double value in values)
        {
            total += value;
        }

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
