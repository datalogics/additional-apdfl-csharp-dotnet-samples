using System.Globalization;
using System.Text;
using System.Text.Json;
using Datalogics.PDFL;

namespace FieldServiceInspectionReport;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Paths paths = Paths.From(args);
            InspectionReport report = JsonSerializer.Deserialize<InspectionReport>(File.ReadAllText(paths.Json), JsonOptions)
                ?? throw new InvalidDataException("The JSON report is empty.");
            Validate(report, paths);
            List<Finding> findings = CsvReader.Read(paths.Csv);

            string? licenseKey = Environment.GetEnvironmentVariable("APDFL_LICENSE_KEY");
            if (!string.IsNullOrWhiteSpace(licenseKey))
                Library.LicenseKey = licenseKey.Trim();

            using Library library = new();
            new PdfInspectionRenderer().Render(report, findings, paths.Images, paths.Output);

            Console.WriteLine($"Created {paths.Output}");

            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or JsonException or LibraryException or ApplicationException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine("Usage: dotnet run -- [report.json findings.csv images-directory output.pdf]");
            return 1;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static void Validate(InspectionReport r, Paths paths)
    {
        if (string.IsNullOrWhiteSpace(r.ReportNumber) || string.IsNullOrWhiteSpace(r.Title) ||
            string.IsNullOrWhiteSpace(r.Summary) || r.Inspector is null || r.Customer is null || r.Site is null)
            throw new InvalidDataException("JSON must include reportNumber, title, summary, inspector, customer, and site.");
        if (!DateOnly.TryParseExact(r.InspectionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new InvalidDataException("inspectionDate must use yyyy-MM-dd.");
        if (!new[] { "Operational", "Attention Required", "Critical" }.Contains(r.OverallStatus, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("overallStatus must be Operational, Attention Required, or Critical.");
        if (r.Photographs is null) return;
        if (r.Photographs.Select(p => p.File).Distinct(StringComparer.OrdinalIgnoreCase).Count() != r.Photographs.Count)
            throw new InvalidDataException("photographs contains duplicate file references.");
        foreach (Photo photo in r.Photographs)
        {
            string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(paths.Images, photo.File));
            if (!full.StartsWith(System.IO.Path.GetFullPath(paths.Images), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Photograph path escapes the images directory: {photo.File}");
            if (!File.Exists(full)) throw new FileNotFoundException($"Photograph not found: {photo.File}", full);
        }
    }

    private sealed record Paths(string Json, string Csv, string Images, string Output)
    {
        public static Paths From(string[] args)
        {
            if (args.Length is not (0 or 4)) throw new ArgumentException("Provide either no arguments or JSON, CSV, image directory, and output paths.");
            string root = AppContext.BaseDirectory;
            return args.Length == 0
                ? new(System.IO.Path.Combine(root, "SampleData", "report.json"), System.IO.Path.Combine(root, "SampleData", "findings.csv"), System.IO.Path.Combine(root, "SampleData", "images"), System.IO.Path.Combine(Environment.CurrentDirectory, "field-service-inspection-report.pdf"))
                : new(System.IO.Path.GetFullPath(args[0]), System.IO.Path.GetFullPath(args[1]), System.IO.Path.GetFullPath(args[2]), System.IO.Path.GetFullPath(args[3]));
        }
    }
}

internal sealed record InspectionReport(
    string ReportNumber, string Title, string InspectionDate, string OverallStatus, string Summary,
    Inspector Inspector, Party Customer, Site Site, string? CertificationText, List<Photo>? Photographs);
internal sealed record Inspector(string Name, string Role);
internal sealed record Party(string Name, string Contact);
internal sealed record Site(string Name, string Address, string EquipmentId);
internal sealed record Photo(string File, string Caption);
internal sealed record Finding(string Id, string Category, string Location, string Severity, string Description, string RecommendedAction, string ResponsibleParty, string TargetDate, string Status);

internal static class CsvReader
{
    public static List<Finding> Read(string path)
    {
        using StreamReader reader = File.OpenText(path);
        string? header = reader.ReadLine();
        if (header is null) throw new InvalidDataException("Findings CSV is empty.");
        string[] columns = Parse(header);
        string[] expected = { "id", "category", "location", "severity", "description", "recommendedAction", "responsibleParty", "targetDate", "status" };
        if (!columns.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException("Findings CSV header does not match the documented contract.");
        List<Finding> result = new();
        for (int line = 2; !reader.EndOfStream; line++)
        {
            string[] values = Parse(reader.ReadLine() ?? string.Empty);
            if (values.Length != expected.Length) throw new InvalidDataException($"Findings CSV line {line} has {values.Length} fields; expected {expected.Length}.");
            result.Add(new Finding(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8]));
        }
        if (result.Count == 0) throw new InvalidDataException("Findings CSV contains no findings.");
        return result;
    }

    private static string[] Parse(string line)
    {
        List<string> fields = new(); StringBuilder field = new(); bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; continue; }
            if (c == '"') { quoted = !quoted; continue; }
            if (c == ',' && !quoted) { fields.Add(field.ToString()); field.Clear(); continue; }
            field.Append(c);
        }
        if (quoted) throw new InvalidDataException("Findings CSV contains an unterminated quoted field.");
        fields.Add(field.ToString()); return fields.ToArray();
    }
}
