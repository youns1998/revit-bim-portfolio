using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using BimInspector.Core;
using BimInspector.UI;
using System.Windows.Interop;

namespace BimInspector.Addin;

[Transaction(TransactionMode.ReadOnly)]
public sealed class InspectCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument is null || uiDocument.Document.IsFamilyDocument)
        {
            TaskDialog.Show("BIM Inspector", "점검할 Revit 프로젝트를 먼저 여세요.");
            return Result.Cancelled;
        }
        try
        {
            var window = new InspectionWindow(Collect(uiDocument.Document));
            new WindowInteropHelper(window).Owner = commandData.Application.MainWindowHandle;
            if (window.ShowDialog() == true && window.RequestedIssue is { } issue)
            {
                var id = new ElementId(issue.ElementId);
                if (uiDocument.Document.GetElement(id) is not null)
                {
                    uiDocument.Selection.SetElementIds(new List<ElementId> { id });
                    var element = uiDocument.Document.GetElement(id);
                    if (element is ViewSheet sheet) uiDocument.ActiveView = sheet;
                    else if (element is Room { Location: null }) TaskDialog.Show("BIM Inspector", "배치되지 않은 실을 선택했습니다. 모델에 표시할 위치는 없습니다.");
                    else uiDocument.ShowElements(id);
                }
            }
            return Result.Succeeded;
        }
        catch (Exception error)
        {
            message = "BIM 점검을 완료하지 못했습니다: " + error.Message;
            return Result.Failed;
        }
    }

    public static ModelSnapshot Collect(Document document)
    {
        string LevelName(Element element) => document.GetElement(element.LevelId)?.Name ?? "";
        var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
            .OfType<Room>().Select(room => new RoomRecord(room.Id.Value, room.Number ?? "", room.Name ?? "", LevelName(room),
                UnitUtils.ConvertFromInternalUnits(room.Area, UnitTypeId.SquareMeters), room.Location is not null)).ToList();
        var components = new List<ComponentRecord>();
        foreach (var pair in new[] { (BuiltInCategory.OST_Doors, "문"), (BuiltInCategory.OST_Windows, "창") })
            components.AddRange(new FilteredElementCollector(document).OfCategory(pair.Item1).WhereElementIsNotElementType()
                .Select(element => new ComponentRecord(element.Id.Value, pair.Item2,
                    element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "", element.Name ?? "", LevelName(element))));
        var schedules = new FilteredElementCollector(document).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>()
            .Where(schedule => !schedule.IsTitleblockRevisionSchedule).GroupBy(schedule => schedule.OwnerViewId.Value).ToDictionary(group => group.Key, group => group.Count());
        var sheets = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
            .Select(sheet => new SheetRecord(sheet.Id.Value, sheet.SheetNumber, sheet.Name,
                sheet.GetAllPlacedViews().Count, schedules.GetValueOrDefault(sheet.Id.Value), sheet.IsPlaceholder)).ToList();
        return new(document.Title, rooms, components, sheets);
    }
}
