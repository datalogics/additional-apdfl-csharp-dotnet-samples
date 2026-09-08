using Datalogics.PDFL;

namespace FieldServiceInspectionReport;

internal sealed class PdfTaggingContext
{
    private readonly Document _document;
    private readonly StructTreeRoot _structTreeRoot;

    public PdfTaggingContext(Document document, string language)
    {
        _document = document;

        PDFDict markInfo = new(_document, false);
        markInfo.Put("Marked", Bool(true));
        markInfo.Put("Suspects", Bool(false));
        _document.Root.Put("MarkInfo", markInfo);
        _document.Root.Put("Lang", Str(language));

        _structTreeRoot = _document.CreateStructTreeRoot();
        DocumentElement = CreateElement("Document", parent: null);
    }

    public PdfTaggedElement DocumentElement { get; }

    public PdfTaggedElement CreateElement(string tagName, PdfTaggedElement? parent)
    {
        StructElement element = parent is null
            ? _structTreeRoot.AddChild(tagName)
            : parent.Element.AddChild(tagName);
        return new PdfTaggedElement(tagName, element);
    }

    public void SetAltText(PdfTaggedElement element, string altText)
    {
        if (!string.IsNullOrWhiteSpace(altText))
        {
            element.Element.PDFDict.Put("Alt", Str(altText));
        }
    }

    public void AddTaggedElement(PdfLayoutContext layout, Element element, PdfTaggedElement owner)
    {
        Page page = layout.CurrentPage;

        PDFDict propertyList = new(_document, false);

        Container container = new(owner.TagName, propertyList, isInline: true)
        {
            Content = new Content(element)
        };

        page.Content.AddElement(container);
        owner.Element.AddMarkedContentRef(page, container);
        layout.MarkPageDirty();
    }

    public void AddArtifactElement(PdfLayoutContext layout, Element element)
    {
        PDFDict artifactProperties = new(_document, false);
        artifactProperties.Put("Type", Name("Layout"));

        Container artifact = new("Artifact", artifactProperties, isInline: true)
        {
            Content = new Content(element)
        };

        layout.CurrentPage.Content.AddElement(artifact);
        layout.MarkPageDirty();
    }

    public void Finish()
    {
    }

    private PDFName Name(string value) => new(value, _document, false);

    private PDFInteger Int(int value) => new(value, _document, false);

    private PDFBoolean Bool(bool value) => new(value, _document, false);

    private PDFString Str(string value) => new(value, _document, false, storedAsHex: false);

}

