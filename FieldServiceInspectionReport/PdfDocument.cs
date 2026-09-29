using System.Globalization;
using Datalogics.PDFL;

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

/// <summary>Creates the APDFL document and manages its page lifecycle and layout bounds.</summary>
internal sealed class PdfDocument : IDisposable
{
    private readonly Document _document;
    private readonly PdfDocumentOptions _options;
    private readonly Rect _pageBounds;
    private Page? _currentPage;
    private int _pageCount;

    private PdfDocument(Document document, PdfDocumentOptions options)
    {
        _document = document;
        _options = options;
        _pageBounds = new Rect(0, 0, options.PageWidth, options.PageHeight);
    }

    public static PdfDocument Create(PdfDocumentOptions options)
    {
        // APDFL Document owns the PDF catalog, metadata, pages, and save lifecycle.
        Document document = new() { Title = options.Title, Producer = options.Producer };
        return new PdfDocument(document, options);
    }

    public event Action<PdfDocument>? PageStarted;
    public Page CurrentPage => _currentPage ?? throw new InvalidOperationException("Call NewPage() first.");
    public Document Document => _document;
    public int PageNumber => _pageCount;
    public double Left => _options.MarginHorizontal;
    public double Right => _options.PageWidth - _options.MarginHorizontal;
    public double ContentWidth => Right - Left;
    public double PageTop => _options.PageHeight - _options.MarginTop;
    public double PageBottom => _options.MarginBottom;
    public double Y { get; set; }

    public void NewPage()
    {
        // Finalize elements accumulated on the previous APDFL Page before moving the page cursor.
        _currentPage?.UpdateContent();
        // APDFL inserts at the current last-page index; -1 appends the first page to an empty document.
        _currentPage = _document.CreatePage(_pageCount - 1, _pageBounds);
        _pageCount++;
        Y = PageTop;
        PageStarted?.Invoke(this);
    }

    public bool EnsureSpace(double needed)
    {
        if (Y - needed >= PageBottom)
            return false;

        // NewPage raises PageStarted, allowing the renderer to add page-specific APDFL footer elements.
        NewPage();
        return true;
    }

    public IReadOnlyList<string> Wrap(string text, TextStyle style, double width)
    {
        // TextStyle measures with APDFL Font.MeasureTextWidth, so line breaks match the selected PDF font.
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

    public void Save(string path)
    {
        _currentPage?.UpdateContent();
        _document.MajorVersion = 2;
        _document.MinorVersion = 0;
        // Put the title in viewer preferences so PDF viewers can show it instead of the file name.
        PDFDict viewerPreferences = new(_document, false);
        viewerPreferences.Put("DisplayDocTitle", new PDFBoolean(true, _document, false));
        _document.Root.Put("ViewerPreferences", viewerPreferences);

        // The renderer creates subset-embedded Font objects; EmbedFonts writes their font programs before save.
        _document.EmbedFonts(EmbedFlags.None);
        _document.Save(SaveFlags.Full | SaveFlags.Compressed, path);
    }

    public void Dispose() => _document.Dispose();
}

/// <summary>Small RGB color value kept with the PDF layout helpers that consume it.</summary>
internal readonly record struct PdfColor(double Red, double Green, double Blue)
{
    public static PdfColor FromHex(string value)
    {
        string hex = value.TrimStart('#');
        if (hex.Length != 6)
            throw new ArgumentException("Color must be a six-digit RGB hex value.", nameof(value));

        return new PdfColor(
            ParseByte(hex.AsSpan(0, 2)) / 255.0,
            ParseByte(hex.AsSpan(2, 2)) / 255.0,
            ParseByte(hex.AsSpan(4, 2)) / 255.0);
    }

    public Color ToPdfColor() => new(Red, Green, Blue);

    private static byte ParseByte(ReadOnlySpan<char> value) =>
        byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
