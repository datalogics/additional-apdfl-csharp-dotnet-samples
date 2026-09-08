using Datalogics.PDFL;

namespace FieldServiceInspectionReport;

internal sealed record PdfTaggedElement(
    string TagName,
    StructElement Element);

