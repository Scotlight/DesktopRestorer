using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

#if NET8_0_OR_GREATER
#nullable disable
#endif

namespace DesktopRestorer
{
    public partial class SystemShortcutsWindow : Window
    {
        private readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();

        public SystemShortcutsWindow()
        {
            InitializeComponent();
            InitializeIcons();
        }

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            string shortcutName = "";
            string targetPath = "";
            string arguments = "";
            string iconLocation = "";

            if (button.Name == "ThisPCButton")
            {
                shortcutName = "此电脑";
                targetPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            }
            else if (button.Name == "RecycleBinButton")
            {
                shortcutName = "回收站";
                targetPath = "::{645FF040-5081-101B-9F08-00AA002F954E}";
            }
            else if (button.Name == "ControlPanelButton")
            {
                shortcutName = "控制面板";
                targetPath = "::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}";
            }
            else if (button.Name == "NetworkButton")
            {
                shortcutName = "网络";
                targetPath = "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
            }
            else if (button.Name == "UsersButton")
            {
                shortcutName = "用户文件夹";
                targetPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (button.Name == "DocumentsButton")
            {
                shortcutName = "文档";
                targetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if (!string.IsNullOrEmpty(shortcutName) && !string.IsNullOrEmpty(targetPath))
            {
                try
                {
                    CreateShortcut(shortcutName, targetPath, arguments, iconLocation);
                    System.Windows.MessageBox.Show($"‘{shortcutName}’ shortcut created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating shortcut: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateShortcut(string shortcutName, string targetPath, string arguments, string iconLocation)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutLocation = Path.Combine(desktopPath, shortcutName + ".lnk");

            string script = $@"
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut(""{shortcutLocation}"")
$sc.TargetPath = ""{targetPath}""
$sc.Save()
";
            
            var plainTextBytes = System.Text.Encoding.Unicode.GetBytes(script);
            string encodedCommand = Convert.ToBase64String(plainTextBytes);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"PowerShell script failed: {stderr}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InitializeIcons()
        {
            SetButtonIcon(ThisPCButton);
            SetButtonIcon(RecycleBinButton);
            SetButtonIcon(ControlPanelButton);
            SetButtonIcon(NetworkButton);
            SetButtonIcon(UsersButton);
            SetButtonIcon(DocumentsButton);
        }

        private void SetButtonIcon(System.Windows.Controls.Button button)
        {
            if (button == null)
            {
                return;
            }

            var resourcePath = button.Tag as string;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            var icon = GetIcon(resourcePath);
            if (icon == null)
            {
                return;
            }

            var stack = button.Content as StackPanel;
            if (stack == null)
            {
                return;
            }

            foreach (var child in stack.Children)
            {
                var image = child as System.Windows.Controls.Image;
                if (image != null)
                {
                    image.Source = icon;
                    break;
                }
            }
        }

        private System.Windows.Media.ImageSource GetIcon(string resourcePath)
        {
            if (_iconCache.TryGetValue(resourcePath, out var cached))
            {
                return cached;
            }

            var icon = LoadSvg(resourcePath);
            if (icon != null)
            {
                icon.Freeze();
                _iconCache[resourcePath] = icon;
            }

            return icon;
        }

        private DrawingImage LoadSvg(string resourcePath)
        {
            try
            {
                var resource = System.Windows.Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
                if (resource == null)
                {
                    return null;
                }

                using (var stream = resource.Stream)
                {
                    var document = XDocument.Load(stream);
                    if (document.Root == null)
                    {
                        return null;
                    }

                    var group = new DrawingGroup();
                    foreach (var element in document.Root.Elements())
                    {
                        var drawing = CreateDrawing(element);
                        if (drawing != null)
                        {
                            group.Children.Add(drawing);
                        }
                    }

                    return new DrawingImage(group);
                }
            }
            catch
            {
                return null;
            }
        }

        private Drawing CreateDrawing(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "rect":
                    return CreateRectangleDrawing(element);
                case "circle":
                    return CreateCircleDrawing(element);
                case "line":
                    return CreateLineDrawing(element);
                case "path":
                    return CreatePathDrawing(element);
                default:
                    return null;
            }
        }

        private Drawing CreateRectangleDrawing(XElement element)
        {
            double x = ParseDouble(element.Attribute("x")?.Value);
            double y = ParseDouble(element.Attribute("y")?.Value);
            double width = ParseDouble(element.Attribute("width")?.Value);
            double height = ParseDouble(element.Attribute("height")?.Value);
            double radiusX = ParseDouble(element.Attribute("rx")?.Value);
            double radiusY = ParseDouble(element.Attribute("ry")?.Value);

            var geometry = new RectangleGeometry(new Rect(x, y, width, height), radiusX, radiusY);
            ApplyTransform(geometry, element.Attribute("transform")?.Value);

            return new GeometryDrawing
            {
                Geometry = geometry,
                Brush = CreateBrush(element.Attribute("fill")?.Value),
                Pen = CreatePen(element)
            };
        }

        private Drawing CreateCircleDrawing(XElement element)
        {
            double cx = ParseDouble(element.Attribute("cx")?.Value);
            double cy = ParseDouble(element.Attribute("cy")?.Value);
            double r = ParseDouble(element.Attribute("r")?.Value);

            var geometry = new EllipseGeometry(new System.Windows.Point(cx, cy), r, r);
            ApplyTransform(geometry, element.Attribute("transform")?.Value);

            return new GeometryDrawing
            {
                Geometry = geometry,
                Brush = CreateBrush(element.Attribute("fill")?.Value),
                Pen = CreatePen(element)
            };
        }

        private Drawing CreateLineDrawing(XElement element)
        {
            double x1 = ParseDouble(element.Attribute("x1")?.Value);
            double y1 = ParseDouble(element.Attribute("y1")?.Value);
            double x2 = ParseDouble(element.Attribute("x2")?.Value);
            double y2 = ParseDouble(element.Attribute("y2")?.Value);

            var geometry = new LineGeometry(new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            ApplyTransform(geometry, element.Attribute("transform")?.Value);

            return new GeometryDrawing
            {
                Geometry = geometry,
                Pen = CreatePen(element)
            };
        }

        private Drawing CreatePathDrawing(XElement element)
        {
            var data = element.Attribute("d")?.Value;
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var geometry = Geometry.Parse(data);
            ApplyTransform(geometry, element.Attribute("transform")?.Value);

            return new GeometryDrawing
            {
                Geometry = geometry,
                Brush = CreateBrush(element.Attribute("fill")?.Value),
                Pen = CreatePen(element)
            };
        }

        private void ApplyTransform(Geometry geometry, string transformValue)
        {
            if (geometry == null || string.IsNullOrWhiteSpace(transformValue))
            {
                return;
            }

            transformValue = transformValue.Trim();
            if (transformValue.StartsWith("rotate", StringComparison.OrdinalIgnoreCase))
            {
                var parameters = transformValue.Substring(transformValue.IndexOf('(') + 1);
                parameters = parameters.TrimEnd(')');
                var parts = parameters.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    double angle = ParseDouble(parts[0]);
                    double centerX = parts.Length > 1 ? ParseDouble(parts[1]) : 0;
                    double centerY = parts.Length > 2 ? ParseDouble(parts[2]) : 0;
                    geometry.Transform = new RotateTransform(angle, centerX, centerY);
                }
            }
        }

        private System.Windows.Media.Pen CreatePen(XElement element)
        {
            var strokeValue = element.Attribute("stroke")?.Value;
            if (string.IsNullOrWhiteSpace(strokeValue) || strokeValue.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var brush = CreateBrush(strokeValue);
            if (brush == null)
            {
                return null;
            }

            double thickness = ParseDouble(element.Attribute("stroke-width")?.Value, 1);
            var pen = new System.Windows.Media.Pen(brush, thickness);

            var lineCap = element.Attribute("stroke-linecap")?.Value;
            if (!string.IsNullOrWhiteSpace(lineCap))
            {
                PenLineCap cap = PenLineCap.Flat;
                if (string.Equals(lineCap, "round", StringComparison.OrdinalIgnoreCase))
                {
                    cap = PenLineCap.Round;
                }
                else if (string.Equals(lineCap, "square", StringComparison.OrdinalIgnoreCase))
                {
                    cap = PenLineCap.Square;
                }

                pen.StartLineCap = cap;
                pen.EndLineCap = cap;
            }

            return pen;
        }

        private System.Windows.Media.Brush CreateBrush(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(value);
        }

        private double ParseDouble(string value, double defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}

#if NET8_0_OR_GREATER
#nullable restore
#endif