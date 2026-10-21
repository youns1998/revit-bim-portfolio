using System.IO;
using BimInspector.Core;
using BimInspector.UI;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var input = Path.Combine(AppContext.BaseDirectory, "model-snapshot.json");
            var inputIndex = Array.IndexOf(args, "--input");
            if (inputIndex >= 0) input = args[inputIndex + 1];
            var snapshot = ReportWriter.ReadSnapshot(input);
            var exportIndex = Array.IndexOf(args, "--export");
            if (exportIndex >= 0)
            {
                Directory.CreateDirectory(args[exportIndex + 1]);
                ReportWriter.WriteCsv(Path.Combine(args[exportIndex + 1], "inspection.csv"), InspectionEngine.Inspect(snapshot));
                ReportWriter.WriteSnapshot(Path.Combine(args[exportIndex + 1], "model-snapshot.json"), snapshot);
                Console.WriteLine($"샘플 점검 완료: {InspectionEngine.Inspect(snapshot).Count}건");
                return 0;
            }
            var application = new Application();
            var window = new InspectionWindow(snapshot, demo: true);
            var screenshotIndex = Array.IndexOf(args, "--screenshot");
            if (screenshotIndex >= 0)
            {
                // Render the actual UI tree without displaying or activating a window.
                window.Content = null;
                var surface = new Border { Child = window.Surface, Background = window.Background, Width = window.Width, Height = window.Height };
                surface.Measure(new Size(window.Width, window.Height));
                surface.Arrange(new Rect(0, 0, window.Width, window.Height));
                surface.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                var image = new RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, PixelFormats.Pbgra32);
                image.Render(surface);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                using var stream = File.Create(args[screenshotIndex + 1]); encoder.Save(stream);
                window.Close();
                return 0;
            }
            application.Run(window);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
