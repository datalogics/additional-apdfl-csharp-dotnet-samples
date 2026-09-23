# CreateFieldServiceInspectionReport

This public sample creates a multi-page field-service inspection report from
scratch with the Datalogics Adobe PDF Library (APDFL). It reads fictional
report metadata from JSON, findings from CSV, and local photographs with
captions.

The sample demonstrates document creation, text measurement and wrapping,
status panels, tables, repeated table headers, pagination, image placement,
captions, headers, footers, and page numbers. It intentionally creates an
ordinary, untagged PDF: tagged-PDF structure, marked content, PDF/UA metadata,
and accessibility conformance are outside this sample's goals.

## Contents

This directory contains the sample source, project configuration, README, and
fictional `SampleData`. APDFL binaries and an APDFL license are not included;
build and PDF output files are created when you run the sample.

The sample's HVAC images are AI-generated fictional illustrations. They do
not depict real equipment or an actual inspection site.

## Prerequisites

1. Windows, macOS, or Linux supported by your Datalogics APDFL package.
2. The .NET 10 SDK.
3. Choose the APDFL .NET package that matches your licensing:
   - **Evaluating APDFL:** use the LM package and obtain an activation key
     directly from Datalogics.
   - **Licensed Datalogics customer:** use the non-LM package supplied for your
     licensed deployment.
4. Optional: `qpdf` for checking generated PDF syntax.

The project defaults to the LM package. The package can be selected with the
`APDFLPackage` build property (`LM` or `NonLM`). Obtain APDFL through
Datalogics' official .NET instructions:

- https://dev.datalogics.com/adobe-pdf-library/dot-net/getting-started
- https://www.datalogics.com/adobe-pdf-library-nuget

APDFL is restored from a package source and is not stored in this directory.

## Build and run

From this directory, choose one package mode and restore and build with the
same property each time. The `21.*` version range selects the newest 21.x
package available from the chosen source.

For evaluation, restore the LM package from NuGet.org:

```powershell
$project = ".\CreateFieldServiceInspectionReport.csproj"
dotnet msbuild $project -target:Restore `
  -property:RestoreSources=https://api.nuget.org/v3/index.json `
  -property:APDFLPackage=LM
dotnet build $project --configuration Release --no-restore -p:APDFLPackage=LM
```

On first run, LM prompts for activation when needed. Get the key from
Datalogics. For a non-interactive run, provide it only in the process
environment:

```powershell
$env:APDFL_LICENSE_KEY = "<your-datalogics-activation-key>"
dotnet run --configuration Release --no-build
Remove-Item Env:APDFL_LICENSE_KEY
```

Do not commit the key or put it in source, JSON, CSV, or README files.

Licensed Datalogics customers should select the non-LM package from their
approved local or private NuGet feed:

```powershell
$project = ".\CreateFieldServiceInspectionReport.csproj"
$nonLmFeed = "<path-to-non-LM-NuGet-feed>"
dotnet msbuild $project -target:Restore `
  "-property:RestoreSources=$nonLmFeed" `
  -property:APDFLPackage=NonLM
dotnet build $project --configuration Release --no-restore -p:APDFLPackage=NonLM
```

After building the selected package, run the included-data example:

```powershell
dotnet run --configuration Release --no-build
```

The default run reads `SampleData\report.json`, `SampleData\findings.csv`, and
`SampleData\images\`, then creates `field-service-inspection-report.pdf` in
the current directory.

To supply explicit input and output paths:

```powershell
dotnet run --configuration Release --no-build -- `
  SampleData\report.json `
  SampleData\findings.csv `
  SampleData\images `
  inspection-report.pdf
```

The run uses whichever package was selected for the last restore and build.
Repeat those commands with the other `APDFLPackage` value to switch modes.

## Verify the result

Confirm that the PDF contains the report header, summary, checklist, findings
table, photographs, captions, and closing certification. Check that long text
wraps within table cells and that headers, footers, page numbers, and images
do not overlap the body.

If qpdf is installed:

```powershell
qpdf --check .\field-service-inspection-report.pdf
```

## Source layout

- `Program.cs` — command-line paths, JSON/CSV parsing, and input validation.
- `PdfInspectionRenderer.cs` — PDFL page construction and visual formatting.
- `PdfDocument.cs` — untagged page-content, pagination, text, table, and image helpers.
- `PdfColor.cs` — color conversion helpers.
- `SampleData` — small fictional JSON, CSV, and image inputs.

## Licensing and distribution

This sample does not redistribute APDFL, an APDFL license, or non-LM packages.
Evaluators must obtain an activation key from Datalogics. Non-LM packages are
for licensed Datalogics customers and must come from their approved feed.
