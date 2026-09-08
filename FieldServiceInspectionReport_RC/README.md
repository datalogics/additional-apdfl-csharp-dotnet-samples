# CreateFieldServiceInspectionReport

This public sample creates a polished, multi-page field-service inspection
report from scratch with the Datalogics Adobe PDF Library (APDFL). It reads
fictional report metadata from JSON, findings and corrective actions from CSV,
and local photographs with captions and alternate text.

The sample demonstrates PDFL document creation, text measurement and wrapping,
status panels, tables, repeated table headers, pagination, image placement,
captions, headers, footers, page numbers, and tagged PDF structure.

## What is included

This `_RC` directory contains only the sample source, project configuration,
README, `.gitignore`, and public-safe fictional `SampleData`. It does not
contain APDFL binaries, NuGet packages, an APDFL license, build output, or
generated PDFs.

## Prerequisites

1. Windows, macOS, or Linux supported by your Datalogics APDFL package.
2. The .NET 10 SDK.
3. An APDFL license-managed NuGet package and activation key from Datalogics.
4. Optional: `qpdf` for checking generated PDF syntax.

APDFL is intentionally not stored in this sample. Obtain it through
Datalogics' official .NET getting-started instructions:

- https://dev.datalogics.com/adobe-pdf-library/dot-net/getting-started
- https://www.datalogics.com/adobe-pdf-library-nuget

Those instructions identify the package `Adobe.PDF.Library.LM.NET`, explain
how to install it through NuGet, and describe how to request and activate a
trial or production license.

## Open the project

From PowerShell:

```powershell
Set-Location "C:\Datalogics\MCP\FieldServiceInspectionReport\_RC"
```

Or change to the directory containing this README using your operating
system's terminal.

## Restore APDFL from NuGet

Restore the dependency. This downloads APDFL into NuGet's external package
cache; it does not place APDFL binaries or a license in this sample directory.

```powershell
dotnet restore
```

The project references:

```xml
<PackageReference Include="Adobe.PDF.Library.LM.NET" Version="21.*" />
```

Do not copy APDFL DLLs, native libraries, license files, or activation keys
into `_RC`.

## Configure licensing

Activate APDFL according to Datalogics' instructions. For a non-interactive
run, set the activation key only in the process environment:

```powershell
$env:APDFL_LICENSE_KEY = "<your-datalogics-activation-key>"
```

Do not commit the key or put it in `README.md`, source code, JSON, or CSV
files. A normal Datalogics license configuration may be used instead.

## Build

```powershell
dotnet build --configuration Release
```

The APDFL package and its runtime files are supplied by NuGet and copied to
the build output directory by the .NET build. They are not part of `_RC`.

## Run with the included fictional data

```powershell
dotnet run --configuration Release
```

The default run reads:

- `SampleData\report.json`
- `SampleData\findings.csv`
- `SampleData\images\`

It creates `field-service-inspection-report.pdf` in the current directory.

## Run with explicit paths

The command-line arguments are JSON report, CSV findings, image directory,
and output PDF, in that order:

```powershell
dotnet run --configuration Release -- `
  SampleData\report.json `
  SampleData\findings.csv `
  SampleData\images `
  inspection-report.pdf
```

The input JSON must include report metadata, site and inspector information,
status, and photograph metadata. The CSV must contain the documented findings
columns. Photograph references must remain inside the supplied image directory.

## Verify the result

Confirm that the PDF contains the report header, summary, checklist, findings
table, photographs, captions, and closing certification. Check that long text
wraps within table cells and that headers, footers, and page numbers do not
overlap the body.

If qpdf is installed:

```powershell
qpdf --check .\field-service-inspection-report.pdf
```

## Test invalid input

This should print a clear error and return a non-zero exit code:

```powershell
dotnet run --configuration Release -- `
  missing.json missing.csv missing-images invalid-output.pdf
```

## Source layout

- `Program.cs` — command-line paths, JSON/CSV parsing, and input validation.
- `PdfInspectionRenderer.cs` — PDFL page construction and visual formatting.
- `PdfLayoutContext.cs` — page and coordinate layout state.
- `PdfTaggingContext.cs` — tagged-PDF structure.
- `PdfColor.cs` — color conversion helpers.
- `SampleData` — small fictional JSON, CSV, and image inputs.

## Licensing and distribution

This sample does not redistribute APDFL or an APDFL license. Users must obtain
the appropriate APDFL package and license directly from Datalogics and accept
the applicable license terms. The sample's source and fictional inputs should
be reviewed under the license terms used by the surrounding public sample
repository before publication.
