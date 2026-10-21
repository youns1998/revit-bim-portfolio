namespace BimInspector.Core;

public sealed record RoomRecord(long Id, string Number, string Name, string Level, double AreaSquareMeters, bool IsPlaced);
public sealed record ComponentRecord(long Id, string Category, string Mark, string Name, string Level);
public sealed record SheetRecord(long Id, string Number, string Name, int ViewCount, int ScheduleCount, bool IsPlaceholder);
public sealed record ModelSnapshot(string Project, List<RoomRecord> Rooms, List<ComponentRecord> Components, List<SheetRecord> Sheets);
public sealed record InspectionIssue(string Code, string Category, long ElementId, string Name, string Detail);

public static class InspectionEngine
{
    public static IReadOnlyList<InspectionIssue> Inspect(ModelSnapshot model)
    {
        var issues = new List<InspectionIssue>();
        foreach (var room in model.Rooms.OrderBy(room => room.Id))
        {
            if (!room.IsPlaced)
                issues.Add(new("ROOM_UNPLACED", "실", room.Id, room.Name, "배치되지 않은 실입니다."));
            else if (room.AreaSquareMeters <= 0)
                issues.Add(new("ROOM_ZERO_AREA", "실", room.Id, room.Name, "면적이 0인 실입니다. 경계와 배치를 확인하세요."));
        }
        foreach (var group in model.Rooms.Where(room => !string.IsNullOrWhiteSpace(room.Number))
                     .GroupBy(room => room.Number.Trim(), StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            foreach (var room in group.OrderBy(room => room.Id))
                issues.Add(new("ROOM_DUPLICATE_NUMBER", "실", room.Id, room.Name, $"중복 실 번호: {group.Key}"));

        foreach (var component in model.Components.OrderBy(component => component.Id))
            if (string.IsNullOrWhiteSpace(component.Mark))
                issues.Add(new("COMPONENT_EMPTY_MARK", component.Category, component.Id, component.Name, "Mark가 비어 있습니다."));
        foreach (var group in model.Components.Where(component => !string.IsNullOrWhiteSpace(component.Mark))
                     .GroupBy(component => (component.Category, Mark: component.Mark.Trim().ToUpperInvariant())).Where(group => group.Count() > 1))
            foreach (var component in group.OrderBy(component => component.Id))
                issues.Add(new("COMPONENT_DUPLICATE_MARK", component.Category, component.Id, component.Name, $"같은 분류 내 중복 Mark: {component.Mark.Trim()}"));

        foreach (var sheet in model.Sheets.Where(sheet => !sheet.IsPlaceholder && sheet.ViewCount == 0 && sheet.ScheduleCount == 0).OrderBy(sheet => sheet.Id))
            issues.Add(new("SHEET_EMPTY", "시트", sheet.Id, $"{sheet.Number} · {sheet.Name}", "배치된 뷰와 일람표가 없습니다."));
        return issues;
    }
}
