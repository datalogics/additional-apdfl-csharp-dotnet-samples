using Datalogics.PDFL;
using PdfPath = Datalogics.PDFL.Path;

namespace FieldServiceInspectionReport;

internal readonly record struct TextStyle(Font Font, double Size, Color Color)
{
    public double Measure(string text) => Font.MeasureTextWidth(text, Size);
}

internal sealed class PdfDocumentOptions
{
    public string Title { get; init; } = "Untitled";
    public string Producer { get; init; } = "APDFL";
    public double PageWidth { get; init; } = 612;
    public double PageHeight { get; init; } = 792;
    public double MarginHorizontal { get; init; } = 48;
    public double MarginTop { get; init; } = 48;
    public double MarginBottom { get; init; } = 90;
}

/// <summary>
/// Small layout helper for the sample. It deliberately creates ordinary PDF page content
/// and does not create structure trees, marked content, tags, or accessibility metadata.
/// </summary>
internal sealed class PdfDocument : IDisposable
{
    private readonly Document _document;
    private readonly PdfDocumentOptions _options;
    private readonly Rect _pageRect;
    private Page? _page;
    private int _pageCount;

    private PdfDocument(Document document, PdfDocumentOptions options)
    {
        _document = document;
        _options = options;
        _pageRect = new Rect(0, 0, options.PageWidth, options.PageHeight);
    }

    public static PdfDocument Create(PdfDocumentOptions options)
    {
        Document document = new() { Title = options.Title, Producer = options.Producer };
        return new PdfDocument(document, options);
    }

    public event Action<PdfDocument>? PageStarted;
    public Page CurrentPage => _page ?? throw new InvalidOperationException("Call NewPage() first.");
    public Document PdfDocumentHandle => _document;
    public int PageNumber => _pageCount;
    public double Left => _options.MarginHorizontal;
    public double Right => _options.PageWidth - _options.MarginHorizontal;
    public double ContentWidth => Right - Left;
    public double PageTop => _options.PageHeight - _options.MarginTop;
    public double PageBottom => _options.MarginBottom;
    public double Y { get; set; }

    public void NewPage()
    {
        _page?.UpdateContent();
        _page = _document.CreatePage(_pageCount - 1, _pageRect);
        _pageCount++;
        Y = PageTop;
        PageStarted?.Invoke(this);
    }

    public bool EnsureSpace(double needed)
    {
        if (Y - needed >= PageBottom)
            return false;

        NewPage();
        return true;
    }

    public IDisposable Section(string heading, TextStyle style, double spaceAfter = 10, double? x = null)
    {
        EnsureSpace(style.Size * 2.5);
        DrawText(heading, x ?? Left, Y - style.Size, style);
        Y -= style.Size + spaceAfter;
        return NoopScope.Instance;
    }

    public void Paragraph(string text, TextStyle style, double x, double width, double leading, double spaceAfter = 6)
    {
        foreach (string line in Wrap(text, style, width))
        {
            EnsureSpace(leading);
            DrawText(line, x, Y - style.Size, style);
            Y -= leading;
        }
        Y -= spaceAfter;
    }

    public void TextBlock(string _, string text, double x, double baseline, TextStyle style)
        => DrawText(text, x, baseline, style);

    public ListScope List(ListNumbering _)
        => new(this);

    public TableScope Table(IReadOnlyList<string> headers, IReadOnlyList<double> widths, TextStyle headerStyle, double headerHeight)
        => new(this, headers, widths, headerStyle, headerHeight);

    public void Image(Image image) => CurrentPage.Content.AddElement(image);

    public void Caption(string text, double x, double baseline, TextStyle style)
        => DrawText(text, x, baseline, style);

    public void ParagraphUnder(string text, TextStyle style, double x, double top, double width, double leading)
    {
        double baseline = top;
        foreach (string line in Wrap(text, style, width))
        {
            DrawText(line, x, baseline, style);
            baseline -= leading;
        }
    }

    public void ArtifactRect(double x, double top, double width, double height, Color fill, Color stroke, double lineWidth = 0.5)
    {
        PdfPath path = new()
        {
            GraphicState = new GraphicState { FillColor = fill, StrokeColor = stroke, Width = lineWidth },
            PaintOp = PathPaintOpFlags.Fill | PathPaintOpFlags.Stroke,
        };
        path.AddRect(new Point(x, top - height), width, height);
        CurrentPage.Content.AddElement(path);
    }

    public void ArtifactText(string text, double x, double baseline, TextStyle style, string _ = "Footer")
        => DrawText(text, x, baseline, style);

    public void ArtifactRule(double x, double y, double width, Color color)
    {
        PdfPath path = new()
        {
            GraphicState = new GraphicState { FillColor = color, StrokeColor = color, Width = 0.5 },
            PaintOp = PathPaintOpFlags.Fill,
        };
        path.AddRect(new Point(x, y), width, 0.5);
        CurrentPage.Content.AddElement(path);
    }

    public IReadOnlyList<string> Wrap(string text, TextStyle style, double width)
    {
        List<string> lines = new();
        string current = string.Empty;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (style.Measure(candidate) <= width)
                current = candidate;
            else
            {
                if (current.Length > 0) lines.Add(current);
                current = word;
            }
        }
        if (current.Length > 0) lines.Add(current);
        return lines.Count == 0 ? new[] { string.Empty } : lines;
    }

    internal void DrawText(string value, double x, double baseline, TextStyle style)
    {
        GraphicState graphics = new() { FillColor = style.Color };
        TextRun run = new(value, style.Font, graphics, new TextState(), new Matrix(style.Size, 0, 0, style.Size, x, baseline));
        Datalogics.PDFL.Text text = new();
        text.AddRun(run);
        CurrentPage.Content.AddElement(text);
    }

    public void Save(string path)
    {
        _page?.UpdateContent();
        _document.MajorVersion = 2;
        _document.MinorVersion = 0;
        PDFDict viewerPreferences = new(_document, false);
        viewerPreferences.Put("DisplayDocTitle", new PDFBoolean(true, _document, false));
        _document.Root.Put("ViewerPreferences", viewerPreferences);
        _document.EmbedFonts(EmbedFlags.None);
        _document.Save(SaveFlags.Full | SaveFlags.Compressed, path);
    }

    public void Dispose() => _document.Dispose();

    internal sealed class ListScope : IDisposable
    {
        private readonly PdfDocument _document;
        internal ListScope(PdfDocument document) => _document = document;
        public void Item(string label, string text, TextStyle style, double x, double labelWidth, double leading)
        {
            _document.EnsureSpace(leading);
            _document.DrawText(label, x, _document.Y - style.Size, style);
            _document.DrawText(text, x + labelWidth, _document.Y - style.Size, style);
            _document.Y -= leading;
        }
        public void Dispose() { }
    }

    internal sealed class TableScope : IDisposable
    {
        private readonly PdfDocument _document;
        private readonly IReadOnlyList<string> _headers;
        private readonly IReadOnlyList<double> _widths;
        private readonly TextStyle _headerStyle;
        private readonly double _headerHeight;
        public Color HeaderBackground { get; set; } = new Color(0, 0, 0);

        internal TableScope(PdfDocument document, IReadOnlyList<string> headers, IReadOnlyList<double> widths, TextStyle headerStyle, double headerHeight)
        {
            _document = document;
            _headers = headers;
            _widths = widths;
            _headerStyle = headerStyle;
            _headerHeight = headerHeight;
            document.Y -= headerHeight;
        }

        public void DrawHeaderBand(double top)
        {
            double total = 0;
            foreach (double width in _widths) total += width;
            _document.ArtifactRect(_document.Left, top, total, _headerHeight, HeaderBackground, HeaderBackground);

            double x = _document.Left;
            for (int i = 0; i < _headers.Count; i++)
            {
                _document.DrawText(_headers[i], x + 5, top - 15, _headerStyle);
                x += _widths[i];
            }
        }

        public RowScope Row(double height)
        {
            if (_document.Y - height < _document.PageBottom)
            {
                _document.NewPage();
                DrawHeaderBand(_document.Y);
                _document.Y -= _headerHeight;
            }
            return new RowScope(_document, _widths);
        }

        public void Dispose() { }
    }

    internal sealed class RowScope
    {
        private readonly PdfDocument _document;
        private readonly IReadOnlyList<double> _widths;
        internal RowScope(PdfDocument document, IReadOnlyList<double> widths) { _document = document; _widths = widths; }
        public void Cell(string text, int column, double x, double top, TextStyle style, double leading, bool isRowHeader = false)
        {
            double baseline = top;
            foreach (string line in _document.Wrap(text, style, _widths[column] - 10))
            {
                _document.DrawText(line, x + 5, baseline, style);
                baseline -= leading;
            }
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}

internal enum ListNumbering { None, Disc, Circle, Square, Decimal, UpperAlpha, LowerAlpha }
