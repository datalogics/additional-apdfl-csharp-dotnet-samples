using Datalogics.PDFL;

namespace FieldServiceInspectionReport;

internal sealed class PdfInspectionRenderer
{
    private const double W = 612, H = 792, M = 48, Body = 8.5, Line = 11;
    private readonly Font _regular = NewFont("Times-Roman"), _bold = NewFont("Times-Bold");
    private readonly PdfColor _navy = PdfColor.FromHex("#3D315B"), _teal = PdfColor.FromHex("#2D6A73"), _ink = PdfColor.FromHex("#293241"), _muted = PdfColor.FromHex("#68737E"), _pale = PdfColor.FromHex("#EEF0F7"), _line = PdfColor.FromHex("#C9C4D8");
    private Document _document = null!; private PdfLayoutContext _layout = null!; private PdfTaggingContext _tags = null!; private PdfTaggedElement _root = null!;

    public void Render(Document document, InspectionReport report, IReadOnlyList<Finding> findings, string imageDirectory, string output)
    {
        _document = document; _tags = new PdfTaggingContext(document, "en-US"); _root = _tags.DocumentElement;
        _layout = new PdfLayoutContext(document, new Rect(0, 0, W, H), H - M);
        Header(report, 1); Summary(report); FindingsTable(findings); Evidence(report, imageDirectory); Closing(report);
        _layout.CurrentPage.UpdateContent(); _tags.Finish(); document.EmbedFonts(EmbedFlags.None); document.Save(SaveFlags.Full, output);
    }

    private void Header(InspectionReport r, int page)
    {
        double headerHeight = 104; double headerTop = H - M; double statusTop = headerTop - 54; double titleX = M + 20; double statusLeft = W - M - 132; double titleWidth = statusLeft - titleX - 18;
        Box(M, headerTop, W - 2 * M, headerHeight, _pale, _line); Text("FIELD SERVICE INSPECTION REPORT", titleX, headerTop - 21, 9.5, _teal, _bold, "H1", _root);
        IReadOnlyList<string> titleLines = Wrap(r.Title, titleWidth, 18); double titleY = headerTop - 45;
        for (int i = 0; i < titleLines.Count; i++) Text(titleLines[i], titleX, titleY - (i * 20), 18, _navy, _bold, "H1", _root);
        double metadataY = titleY - (titleLines.Count * 20) + 1; Text($"{r.ReportNumber}  |  Inspection date: {r.InspectionDate}", titleX, metadataY, Body, _muted, _regular, "P", _root);
        Box(statusLeft, statusTop, 116, 27, StatusColor(r.OverallStatus), StatusColor(r.OverallStatus)); Text(r.OverallStatus.ToUpperInvariant(), statusLeft + 8, statusTop - 18, 7.5, new PdfColor(1, 1, 1), _bold, "P", _root); _layout.Y = headerTop - headerHeight - 24; Footer(page, r);
    }

    private void Summary(InspectionReport r)
    {
        Ensure(150, r); Section("Inspection summary", "H2"); Box(M, _layout.Y, 250, 96, new PdfColor(1, 1, 1), _line); Box(M + 266, _layout.Y, W - 2 * M - 266, 96, new PdfColor(1, 1, 1), _line); Text("CUSTOMER / SITE", M + 14, _layout.Y - 18, 8, _teal, _bold, "H3", _root); Text(r.Customer.Name, M + 14, _layout.Y - 38, 11, _ink, _bold, "P", _root); Text(r.Customer.Contact, M + 14, _layout.Y - 54, Body, _muted, _regular, "P", _root); Text(r.Site.Name, M + 14, _layout.Y - 70, Body, _ink, _regular, "P", _root); Text(r.Site.Address, M + 14, _layout.Y - 84, 8, _muted, _regular, "P", _root); Text("INSPECTOR", M + 280, _layout.Y - 18, 8, _teal, _bold, "H3", _root); Text(r.Inspector.Name, M + 280, _layout.Y - 38, 11, _ink, _bold, "P", _root); Text(r.Inspector.Role, M + 280, _layout.Y - 54, Body, _muted, _regular, "P", _root); Text($"Equipment: {r.Site.EquipmentId}", M + 280, _layout.Y - 76, Body, _ink, _regular, "P", _root); _layout.Y -= 122; Paragraph(r.Summary, M, W - 2 * M, _root); Checklist(); _layout.Y -= 10;
    }

    private void FindingsTable(IReadOnlyList<Finding> findings)
    {
        Section("Findings and corrective actions", "H2"); double[] widths = { 40, 64, 70, 51, 163, 75, 53 }; string[] headers = { "ID", "Category", "Location", "Severity", "Description", "Action", "Status" }; PdfTaggedElement table = _tags.CreateElement("Table", _root); PdfTaggedElement tableHead = _tags.CreateElement("THead", table); PdfTaggedElement tableBody = _tags.CreateElement("TBody", table); DrawTableHeader(headers, widths, tableHead);
        foreach (Finding f in findings)
        {
            int rows = new[] { f.Id, f.Category, f.Location, f.Severity, f.Description, f.RecommendedAction, f.Status }.Select((s, i) => Wrap(s, widths[i] - 10, Body).Count).Max(); double height = Math.Max(28, rows * Line + 10);
            if (_layout.Y - height < M + 42) { NewPage("Findings continued"); DrawTableHeader(headers, widths, tableHead); }
            PdfTaggedElement row = _tags.CreateElement("TR", tableBody); Box(M, _layout.Y + 5, widths.Sum(), height, new PdfColor(1, 1, 1), _line); double x = M; string[] values = { f.Id, f.Category, f.Location, f.Severity, f.Description, f.RecommendedAction, f.Status };
            for (int i = 0; i < values.Length; i++) { PdfTaggedElement cell = _tags.CreateElement(i == 0 ? "TH" : "TD", row); double y = _layout.Y - 10; foreach (string line in Wrap(values[i], widths[i] - 10, Body)) { Text(line, x + 5, y, Body, i == 3 ? StatusColor(f.Severity) : _ink, i == 0 ? _bold : _regular, "P", cell); y -= Line; } x += widths[i]; } _layout.Y -= height;
        }
    }

    private void Evidence(InspectionReport r, string imageDirectory)
    {
        if (r.Photographs is null || r.Photographs.Count == 0) return; NewPage("Photographic evidence"); Section("Photographic evidence", "H2");
        foreach (Photo photo in r.Photographs)
        { Ensure(175, r); PdfTaggedElement figure = _tags.CreateElement("Figure", _root); _tags.SetAltText(figure, photo.AltText); using Image image = new(System.IO.Path.Combine(imageDirectory, photo.File), _document); double scale = Math.Min(190 / image.Matrix.A, 125 / image.Matrix.D); image.Scale(scale, scale); image.Translate(M + 8, _layout.Y - 130); _tags.AddTaggedElement(_layout, image, figure); Text(photo.Caption, M + 210, _layout.Y - 20, 10, _ink, _bold, "P", figure); Paragraph(photo.AltText, M + 210, 208, figure); _layout.Y -= 160; }
    }

    private void Checklist()
    {
        Section("Inspection checklist", "H3");
        PdfTaggedElement list = _tags.CreateElement("L", _root);
        string[] items = { "Review observed conditions and severity", "Confirm corrective action ownership", "Record photographic evidence for follow-up" };
        foreach (string item in items)
        {
            PdfTaggedElement listItem = _tags.CreateElement("LI", list);
            Text("- " + item, M + 8, _layout.Y, Body, _ink, _regular, "P", listItem);
            _layout.Y -= Line;
        }
        _layout.Y -= 6;
    }

    private void Closing(InspectionReport r) { Ensure(100, r); Section("Closing certification", "H2"); Paragraph(r.CertificationText ?? "The inspection record above reflects the conditions observed at the time of inspection. Follow-up actions should be tracked through the responsible service process.", M, W - 2 * M, _root); Text($"Prepared by {r.Inspector.Name}  |  APDFL sample output", M, _layout.Y - 12, 8, _muted, _regular, "P", _root); }

    private void Section(string title, string role) { Text(title, M, _layout.Y, 13, _navy, _bold, role, _root); _layout.Y -= 22; }
    private void Paragraph(string value, double x, double width, PdfTaggedElement parent) { PdfTaggedElement p = _tags.CreateElement("P", parent); foreach (string line in Wrap(value, width, Body)) { Text(line, x, _layout.Y, Body, _ink, _regular, "P", p); _layout.Y -= Line; } _layout.Y -= 6; }
    private void DrawTableHeader(string[] headers, double[] widths, PdfTaggedElement tableHead) { PdfTaggedElement row = _tags.CreateElement("TR", tableHead); Box(M, _layout.Y + 5, widths.Sum(), 24, _navy, _navy); double x = M; for (int i = 0; i < headers.Length; i++) { PdfTaggedElement cell = _tags.CreateElement("TH", row); Text(headers[i], x + 5, _layout.Y - 10, 7.5, new PdfColor(1, 1, 1), _bold, "TH", cell); x += widths[i]; } _layout.Y -= 24; }
    private void Ensure(double amount, InspectionReport r) { if (_layout.Y - amount >= M + 42) return; NewPage("Inspection report continued"); }
    private void NewPage(string title) { _layout.AddPage(H - M - 52); Footer(_layout.PageCount, null); Text(title, M, _layout.Y, 14, _navy, _bold, "H1", _root); _layout.Y -= 24; }
    private void Footer(int page, InspectionReport? r) { Box(M, M + 19, W - 2 * M, 0.5, _line, _line); Text("Northstar Facilities  |  Confidential field record", M, M + 5, 7.5, _muted, _regular, "Artifact", null); Text($"Page {page}", W - M - 40, M + 5, 7.5, _muted, _regular, "Artifact", null); }
    private void Text(string value, double x, double y, double size, PdfColor color, Font font, string role, PdfTaggedElement? parent) { GraphicState gs = new() { FillColor = color.ToPdfColor() }; TextRun run = new(value, font, gs, new TextState(), new Matrix(size, 0, 0, size, x, y)); Datalogics.PDFL.Text element = new(); element.AddRun(run); if (parent is null) _tags.AddArtifactElement(_layout, element); else _tags.AddTaggedElement(_layout, element, parent); }
    private void Box(double x, double top, double width, double height, PdfColor fill, PdfColor stroke) { Datalogics.PDFL.Path path = new(); path.GraphicState = new GraphicState { FillColor = fill.ToPdfColor(), StrokeColor = stroke.ToPdfColor(), Width = 0.5 }; path.PaintOp = PathPaintOpFlags.Fill | PathPaintOpFlags.Stroke; path.AddRect(new Point(x, top - height), width, height); _tags.AddArtifactElement(_layout, path); }
    private static Font NewFont(string name) => new(name, FontCreateFlags.Subset);
    private static PdfColor StatusColor(string value) => value.Contains("Critical", StringComparison.OrdinalIgnoreCase) ? PdfColor.FromHex("#B34A43") : value.Contains("Attention", StringComparison.OrdinalIgnoreCase) || value.Contains("High", StringComparison.OrdinalIgnoreCase) ? PdfColor.FromHex("#D48632") : PdfColor.FromHex("#2C8A72");
    private IReadOnlyList<string> Wrap(string value, double width, double size) { List<string> lines = new(); string current = string.Empty; foreach (string word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) { string candidate = current.Length == 0 ? word : current + " " + word; if (_regular.MeasureTextWidth(candidate, size) <= width) current = candidate; else { if (current.Length > 0) lines.Add(current); current = word; } } if (current.Length > 0) lines.Add(current); return lines.Count == 0 ? new[] { string.Empty } : lines; }
}
