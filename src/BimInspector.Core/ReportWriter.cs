using System.Text;
using System.Text.Json;

namespace BimInspector.Core;

public static class ReportWriter
{
    public static void WriteCsv(string path, IEnumerable<InspectionIssue> issues)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("규칙,분류,요소 ID,이름,검토 사항");
        foreach (var issue in issues)
            writer.WriteLine(string.Join(",", new[] { issue.Code, issue.Category, issue.ElementId.ToString(System.Globalization.CultureInfo.InvariantCulture), issue.Name, issue.Detail }.Select(Escape)));
    }

    public static void WriteSnapshot(string path, ModelSnapshot snapshot) =>
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));

    public static ModelSnapshot ReadSnapshot(string path) =>
        JsonSerializer.Deserialize<ModelSnapshot>(File.ReadAllText(path)) ?? throw new InvalidDataException("모델 정보가 비어 있습니다.");

    public static string Escape(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
