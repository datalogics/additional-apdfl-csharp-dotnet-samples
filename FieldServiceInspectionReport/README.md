# CreateFieldServiceInspectionReport

This sample creates a multi-page field-service inspection report from scratch
with the Datalogics Adobe PDF Library (APDFL). It reads report metadata from
JSON, findings from CSV, and local images with captions. It demonstrates APDFL
document, page, text, path, font, and image APIs alongside text wrapping,
tables, pagination, headers, footers, and page numbering.

The included HVAC images are fictional illustrations and do not depict real
equipment or an actual inspection site.

## Prerequisites

1. Windows, macOS, or Linux supported by the selected Datalogics APDFL package.
2. The .NET 10 SDK.
3. Select the APDFL .NET package for your licensing:
   - **Evaluation:** use the LM package from NuGet.org. On first run, APDFL
     prompts for an activation key; obtain a trial key from Datalogics.
   - **Licensed Datalogics customer:** use the non-LM package from your
     approved package feed.

The project defaults to LM and selects packages with the `APDFLPackage` build
property (`LM` or `NonLM`). The package reference uses the newest available
21.x version. APDFL is restored from a package source and is not stored in
this sample directory.

For installation and activation details, see Datalogics' [.NET getting
started guide](https://dev.datalogics.com/adobe-pdf-library/dot-net/getting-started)
and [APDFL NuGet page](https://www.datalogics.com/adobe-pdf-library-nuget).

## Restore, build, and run

Run the commands from this directory. Restore and build with the same package
selection.

### Evaluation with LM

```powershell
$project = ".\CreateFieldServiceInspectionReport.csproj"
dotnet msbuild $project -target:Restore `
  -property:RestoreSources=https://api.nuget.org/v3/index.json `
  -property:APDFLPackage=LM
dotnet build $project --configuration Release --no-restore -p:APDFLPackage=LM
dotnet run --configuration Release --no-build
```

On first run, enter the activation key obtained from Datalogics when APDFL
prompts for it.

### Licensed deployment with non-LM

Set `$nonLmFeed` to the approved feed supplied for your licensed deployment:

```powershell
$project = ".\CreateFieldServiceInspectionReport.csproj"
$nonLmFeed = "<path-to-approved-non-LM-feed>"
dotnet msbuild $project -target:Restore `
  "-property:RestoreSources=$nonLmFeed" `
  -property:APDFLPackage=NonLM
dotnet build $project --configuration Release --no-restore -p:APDFLPackage=NonLM
dotnet run --configuration Release --no-build
```

The default run reads `SampleData\report.json`, `SampleData\findings.csv`,
and `SampleData\images\`, then creates `field-service-inspection-report.pdf`
in the current directory.

To specify input and output paths explicitly:

```powershell
dotnet run --configuration Release --no-build -- `
  SampleData\report.json `
  SampleData\findings.csv `
  SampleData\images `
  inspection-report.pdf
```

The run uses the package selected for the last restore and build. Repeat the
restore and build steps with the other `APDFLPackage` value to switch modes.

## Source layout

- `Program.cs` — command-line paths, JSON/CSV parsing, and input validation.
- `PdfInspectionRenderer.cs` — report layout and direct APDFL text, path, font,
  and image operations.
- `PdfDocument.cs` — APDFL document/page lifecycle, layout bounds, wrapping,
  and RGB color helper.
- `SampleData` — fictional JSON, CSV, and image inputs.

## Licensing and distribution

This sample contains source code and fictional input data. APDFL packages and
activation keys are obtained separately from Datalogics. LM is for evaluation;
the non-LM package is for licensed Datalogics customers and must come from
their approved package feed.
