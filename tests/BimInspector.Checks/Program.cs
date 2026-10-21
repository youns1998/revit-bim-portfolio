using System.IO;
using BimInspector.Core;
using BimInspector.UI;
using System.Text;

internal static class Program
{
    [STAThread]
    public static int Main()
    {
        var checks = new List<(string, Action)>();
        var sample = ReportWriter.ReadSnapshot(Path.Combine(AppContext.BaseDirectory, "model-snapshot.json"));
        checks.Add(("Sample detects eight review items", () => Equal(8, InspectionEngine.Inspect(sample).Count)));
        checks.Add(("Unplaced room does not also report zero area", () => Equal(1, InspectionEngine.Inspect(sample).Count(issue => issue.ElementId == 104 && issue.Code.StartsWith("ROOM_UNPLACED")))));
        checks.Add(("Zero area room is reported", () => True(InspectionEngine.Inspect(sample).Any(issue => issue.ElementId == 102 && issue.Code == "ROOM_ZERO_AREA"))));
        checks.Add(("Duplicate room numbers identify both elements", () => Equal(2, InspectionEngine.Inspect(sample).Count(issue => issue.Code == "ROOM_DUPLICATE_NUMBER"))));
        checks.Add(("Duplicate component marks identify both elements", () => Equal(2, InspectionEngine.Inspect(sample).Count(issue => issue.Code == "COMPONENT_DUPLICATE_MARK"))));
        checks.Add(("Blank window mark is reported", () => True(InspectionEngine.Inspect(sample).Any(issue => issue.ElementId == 203 && issue.Code == "COMPONENT_EMPTY_MARK"))));
        checks.Add(("Schedules and placeholders do not count as empty sheets", () => Equal(302L, InspectionEngine.Inspect(sample).Single(issue => issue.Code == "SHEET_EMPTY").ElementId)));
        checks.Add(("Different categories may reuse a mark", () =>
        {
            var clean = new ModelSnapshot("clean", [], [new(1, "문", "01", "Door", "1F"), new(2, "창", "01", "Window", "1F")], []);
            Equal(0, InspectionEngine.Inspect(clean).Count);
        }));
        checks.Add(("Marks are compared without case and surrounding whitespace", () =>
        {
            var duplicate = new ModelSnapshot("duplicate", [], [new(1, "문", " d01 ", "Door", "1F"), new(2, "문", "D01", "Door", "1F")], []);
            Equal(2, InspectionEngine.Inspect(duplicate).Count);
        }));
        checks.Add(("CSV protects formulas and quotes multiline fields", () =>
        {
            Equal("\"'=1+1\"", ReportWriter.Escape("=1+1"));
            Equal("\"a,\"\"b\"\"\nnext\"", ReportWriter.Escape("a,\"b\"\nnext"));
        }));
        checks.Add(("JSON roundtrip preserves inspection results", () => WithFile(path =>
        {
            ReportWriter.WriteSnapshot(path, sample);
            True(InspectionEngine.Inspect(sample).SequenceEqual(InspectionEngine.Inspect(ReportWriter.ReadSnapshot(path))));
        })));
        checks.Add(("CSV is Excel-readable UTF8 with all rows", () => WithFile(path =>
        {
            ReportWriter.WriteCsv(path, InspectionEngine.Inspect(sample));
            True(File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }));
            Equal(9, File.ReadAllLines(path, Encoding.UTF8).Length);
        })));
        checks.Add(("WPF filters the same inspection results by category", () =>
        {
            var window = new InspectionWindow(sample, demo: true);
            Equal(8, window.VisibleIssueCount);
            window.FilterCategory("실"); Equal(4, window.VisibleIssueCount);
            window.FilterCategory("문"); Equal(2, window.VisibleIssueCount);
            window.FilterCategory("창"); Equal(1, window.VisibleIssueCount);
            window.FilterCategory("시트"); Equal(1, window.VisibleIssueCount);
            window.FilterCategory("전체"); Equal(8, window.VisibleIssueCount);
            window.Close();
        }));
        var failures = 0;
        foreach (var (name, run) in checks)
        {
            try { run(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        Console.WriteLine($"{checks.Count - failures}/{checks.Count} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    private static void WithFile(Action<string> run)
    {
        var path = Path.Combine(Path.GetTempPath(), "bim-inspector-" + Guid.NewGuid() + ".tmp");
        try { run(path); } finally { if (File.Exists(path)) File.Delete(path); }
    }
}
