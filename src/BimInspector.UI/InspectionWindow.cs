using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using BimInspector.Core;

namespace BimInspector.UI;

public sealed class InspectionWindow : Window
{
    private readonly IReadOnlyList<InspectionIssue> issues;
    private readonly DataGrid table;
    private readonly ComboBox categories;
    private readonly TextBlock count;
    private readonly Button locate;
    private readonly ModelSnapshot snapshot;
    public InspectionIssue? RequestedIssue { get; private set; }
    public int VisibleIssueCount => table.Items.Count;
    public Grid Surface { get; }

    public InspectionWindow(ModelSnapshot model, bool demo = false)
    {
        snapshot = model;
        issues = InspectionEngine.Inspect(model);
        Title = "Revit BIM Inspector";
        Width = 1040; Height = 660; MinWidth = 760; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Malgun Gothic"); FontSize = 13;
        Background = Brush("#F5F7F3"); Foreground = Brush("#182421");
        Surface = new Grid { Margin = new Thickness(28), Background = Background };
        for (var index = 0; index < 5; index++) Surface.RowDefinitions.Add(new RowDefinition { Height = index == 3 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
        Content = Surface;
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 22) };
        heading.Children.Add(new TextBlock { Text = "REVIT / BIM INSPECTOR", Foreground = Brush("#526D5B"), FontSize = 12, FontWeight = FontWeights.SemiBold });
        heading.Children.Add(new TextBlock { Text = "모델·도면 점검", FontSize = 28, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 6) });
        heading.Children.Add(new TextBlock { Text = model.Project + (demo ? "  ·  샘플 데이터" : "  ·  현재 Revit 모델"), Foreground = Brush("#4F5E57") });
        Add(heading, 0);
        var summary = new TextBlock { Text = $"실 {model.Rooms.Count}개  ·  문·창 {model.Components.Count}개  ·  시트 {model.Sheets.Count}개  ·  검토 {issues.Count}건", Margin = new Thickness(0, 0, 0, 18), FontSize = 15 };
        Add(summary, 1);
        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 14), LastChildFill = true };
        categories = new ComboBox { Width = 140, Height = 34, VerticalContentAlignment = VerticalAlignment.Center, ItemsSource = new[] { "전체", "실", "문", "창", "시트" }, SelectedIndex = 0 };
        DockPanel.SetDock(categories, Dock.Left); toolbar.Children.Add(categories);
        var csv = Button("CSV 내보내기"); csv.Margin = new Thickness(10, 0, 0, 0); DockPanel.SetDock(csv, Dock.Right); toolbar.Children.Add(csv);
        var json = Button("모델 정보 저장"); json.Margin = new Thickness(10, 0, 0, 0); DockPanel.SetDock(json, Dock.Right); toolbar.Children.Add(json);
        count = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0), Foreground = Brush("#526D5B") };
        toolbar.Children.Add(count); Add(toolbar, 2);
        table = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, CanUserDeleteRows = false, SelectionMode = DataGridSelectionMode.Single,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, HeadersVisibility = DataGridHeadersVisibility.Column, Background = Brushes.White,
            BorderBrush = Brush("#DCE3DD"), RowHeight = 42, ColumnHeaderHeight = 38, HorizontalGridLinesBrush = Brush("#E8EDE5") };
        Column("분류", "Category", 70); Column("요소 ID", "ElementId", 85); Column("이름", "Name", 200); Column("검토 사항", "Detail", 1, true);
        Add(table, 3);
        var footer = new DockPanel { Margin = new Thickness(0, 18, 0, 0) };
        locate = Button(demo ? "샘플 · 요소 이동 불가" : "선택 요소 위치 보기"); locate.IsEnabled = false;
        DockPanel.SetDock(locate, Dock.Right); footer.Children.Add(locate);
        footer.Children.Add(new TextBlock { Text = "검토 후보를 표시합니다. 모델을 자동 수정하지 않습니다.", VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#4F5E57"), FontSize = 12 });
        Add(footer, 4);
        categories.SelectionChanged += (_, _) => Refresh();
        table.SelectionChanged += (_, _) => locate.IsEnabled = !demo && table.SelectedItem is InspectionIssue;
        locate.Click += (_, _) => { RequestedIssue = table.SelectedItem as InspectionIssue; DialogResult = true; };
        csv.Click += (_, _) => Save("CSV 보고서", "CSV 파일|*.csv", "bim-inspection.csv", path => ReportWriter.WriteCsv(path, FilteredIssues()));
        json.Click += (_, _) => Save("모델 정보", "JSON 파일|*.json", "model-snapshot.json", path => ReportWriter.WriteSnapshot(path, snapshot));
        Refresh();
    }

    public void FilterCategory(string category) => categories.SelectedItem = category;
    private IEnumerable<InspectionIssue> FilteredIssues() => categories.SelectedItem is string category && category != "전체" ? issues.Where(issue => issue.Category == category) : issues;
    private void Refresh() { var visible = FilteredIssues().ToList(); table.ItemsSource = visible; count.Text = $"{visible.Count}건 표시"; }
    private void Add(UIElement element, int row) { Grid.SetRow(element, row); Surface.Children.Add(element); }
    private void Column(string title, string property, double width, bool star = false) => table.Columns.Add(new DataGridTextColumn { Header = title, Binding = new Binding(property), MinWidth = star ? 260 : width, Width = new DataGridLength(width, star ? DataGridLengthUnitType.Star : DataGridLengthUnitType.Pixel) });
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    private static Button Button(string text) => new() { Content = text, Padding = new Thickness(14, 7, 14, 7), BorderBrush = Brush("#CAD5CB"), Background = Brushes.White, Foreground = Brush("#182421"), MinHeight = 34 };
    private void Save(string title, string filter, string filename, Action<string> write)
    {
        var dialog = new SaveFileDialog { Title = title, Filter = filter, FileName = filename };
        if (dialog.ShowDialog(this) != true) return;
        try { write(dialog.FileName); MessageBox.Show(this, "저장했습니다.", title, MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { MessageBox.Show(this, error.Message, "저장 실패", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
