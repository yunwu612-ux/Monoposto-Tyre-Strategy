using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IOPath = System.IO.Path;

namespace MonopostoTyreStrategy;

public partial class MainWindow : Window
{
    private List<StrategyResult> _results = [];
    private StrategyResult? _recommended;
    private readonly string _dataDirectory = IOPath.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonopostoTyreStrategy");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(_dataDirectory);
        Loaded += (_, _) => Calculate();
        SizeChanged += (_, _) => DrawWearChart();
    }

    private RaceSettings ReadSettings()
    {
        string compound = (StartingCompoundBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "中胎";
        return new RaceSettings
        {
            TrackName = string.IsNullOrWhiteSpace(TrackNameBox.Text) ? "自定义赛道" : TrackNameBox.Text.Trim(),
            RaceLaps = ReadInt(RaceLapsBox, 35, 1, 200),
            BaseLapSeconds = ReadDouble(BaseLapBox, 90, 20, 300),
            PitLossSeconds = ReadDouble(PitLossBox, 22, 0, 120),
            TargetWearPercent = ReadDouble(TargetWearBox, 80, 1, 100),
            StartingCompound = compound,
            SoftWearPerLap = ReadDouble(SoftWearBox, 13, 0.1, 100),
            SoftPaceDeltaSeconds = ReadDouble(SoftPaceBox, -0.45, -20, 20),
            MediumWearPerLap = ReadDouble(MediumWearBox, 9, 0.1, 100),
            MediumPaceDeltaSeconds = ReadDouble(MediumPaceBox, 0, -20, 20),
            HardWearPerLap = ReadDouble(HardWearBox, 6, 0.1, 100),
            HardPaceDeltaSeconds = ReadDouble(HardPaceBox, 0.55, -20, 20)
        };
    }

    private static int ReadInt(TextBox box, int fallback, int min, int max)
    {
        if (!int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            value = fallback;
        value = Math.Clamp(value, min, max);
        box.Text = value.ToString(CultureInfo.InvariantCulture);
        return value;
    }

    private static double ReadDouble(TextBox box, double fallback, double min, double max)
    {
        if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
            !double.TryParse(box.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            value = fallback;
        value = Math.Clamp(value, min, max);
        box.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        return value;
    }

    private void Calculate_Click(object sender, RoutedEventArgs e) => Calculate();

    private void Calculate()
    {
        try
        {
            RaceSettings settings = ReadSettings();
            _results = StrategyEngine.Compare(settings);
            _recommended = _results.FirstOrDefault();
            ResultsGrid.ItemsSource = _results.Select(r => new StrategyDisplay(r)).ToList();

            if (_recommended is not null)
            {
                RecommendedStrategyText.Text = _recommended.StrategyName;
                RecommendedTimeText.Text = _recommended.TotalTimeText;
                RecommendedStopsText.Text = _recommended.Stops.ToString();
            }
            DrawWearChart();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"计算失败：{ex.Message}", "计算错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DrawWearChart()
    {
        if (WearChart is null) return;
        WearChart.Children.Clear();
        double width = WearChart.ActualWidth;
        double height = WearChart.ActualHeight;
        if (width < 100 || height < 80) return;

        const double left = 42, right = 14, top = 12, bottom = 28;
        double plotW = width - left - right;
        double plotH = height - top - bottom;
        var settings = SafeReadSettings();
        if (settings is null) return;

        for (int percent = 0; percent <= 100; percent += 20)
        {
            double y = top + plotH * (1 - percent / 100.0);
            WearChart.Children.Add(new Line
            {
                X1 = left, X2 = width - right, Y1 = y, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(55, 66, 82)),
                StrokeThickness = 1
            });
            AddChartText(percent.ToString(), 4, y - 8, Brushes.LightGray);
        }

        int laps = settings.RaceLaps;
        for (int lap = 0; lap <= laps; lap += Math.Max(1, laps / 5))
        {
            double x = left + plotW * lap / laps;
            WearChart.Children.Add(new Line
            {
                X1 = x, X2 = x, Y1 = top, Y2 = height - bottom,
                Stroke = new SolidColorBrush(Color.FromRgb(38, 47, 61)),
                StrokeThickness = 1
            });
            AddChartText(lap.ToString(), x - 8, height - bottom + 5, Brushes.LightGray);
        }

        DrawSeries(settings, "软胎", settings.SoftWearPerLap, Color.FromRgb(255, 77, 77), left, top, plotW, plotH, laps);
        DrawSeries(settings, "中胎", settings.MediumWearPerLap, Color.FromRgb(255, 196, 66), left, top, plotW, plotH, laps);
        DrawSeries(settings, "硬胎", settings.HardWearPerLap, Color.FromRgb(82, 190, 125), left, top, plotW, plotH, laps);
        AddChartText("圈数 →", width - 55, height - 19, Brushes.White);
        AddChartText("磨损 %", 5, 0, Brushes.White);
    }

    private void DrawSeries(RaceSettings s, string name, double wearRate, Color color,
        double left, double top, double plotW, double plotH, int laps)
    {
        var points = new PointCollection();
        for (int lap = 0; lap <= laps; lap++)
        {
            double wear = Math.Min(100, lap * wearRate);
            points.Add(new Point(left + plotW * lap / laps, top + plotH * (1 - wear / 100.0)));
        }
        WearChart.Children.Add(new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        });
        int legendIndex = name switch { "软胎" => 0, "中胎" => 1, _ => 2 };
        double legendX = 62 + legendIndex * 90;
        WearChart.Children.Add(new Line
        {
            X1 = legendX, X2 = legendX + 17, Y1 = 10, Y2 = 10,
            Stroke = new SolidColorBrush(color), StrokeThickness = 3
        });
        AddChartText(name, legendX + 21, 2, Brushes.White);
    }

    private void AddChartText(string text, double x, double y, Brush brush)
    {
        var label = new TextBlock { Text = text, FontSize = 10, Foreground = brush };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        WearChart.Children.Add(label);
    }

    private RaceSettings? SafeReadSettings()
    {
        try { return ReadSettings(); }
        catch { return null; }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = new SavedProfile
            {
                Name = ReadSettings().TrackName,
                Settings = ReadSettings(),
                SavedAt = DateTime.Now
            };
            var dialog = new SaveFileDialog
            {
                Title = "保存策略配置",
                Filter = "策略配置 (*.json)|*.json",
                FileName = SafeFileName(profile.Name) + ".json",
                InitialDirectory = _dataDirectory
            };
            if (dialog.ShowDialog() != true) return;
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(profile, _jsonOptions), Encoding.UTF8);
            MessageBox.Show("配置已保存。建议将 JSON 文件备份到其他位置。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "载入策略配置",
            Filter = "策略配置 (*.json)|*.json",
            InitialDirectory = _dataDirectory
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var profile = JsonSerializer.Deserialize<SavedProfile>(File.ReadAllText(dialog.FileName));
            if (profile?.Settings is null) throw new InvalidDataException("配置文件内容无效。");
            ApplySettings(profile.Settings);
            Calculate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"载入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplySettings(RaceSettings s)
    {
        TrackNameBox.Text = s.TrackName;
        RaceLapsBox.Text = s.RaceLaps.ToString(CultureInfo.InvariantCulture);
        BaseLapBox.Text = s.BaseLapSeconds.ToString(CultureInfo.InvariantCulture);
        PitLossBox.Text = s.PitLossSeconds.ToString(CultureInfo.InvariantCulture);
        TargetWearBox.Text = s.TargetWearPercent.ToString(CultureInfo.InvariantCulture);
        SoftWearBox.Text = s.SoftWearPerLap.ToString(CultureInfo.InvariantCulture);
        SoftPaceBox.Text = s.SoftPaceDeltaSeconds.ToString(CultureInfo.InvariantCulture);
        MediumWearBox.Text = s.MediumWearPerLap.ToString(CultureInfo.InvariantCulture);
        MediumPaceBox.Text = s.MediumPaceDeltaSeconds.ToString(CultureInfo.InvariantCulture);
        HardWearBox.Text = s.HardWearPerLap.ToString(CultureInfo.InvariantCulture);
        HardPaceBox.Text = s.HardPaceDeltaSeconds.ToString(CultureInfo.InvariantCulture);
        StartingCompoundBox.SelectedIndex = s.StartingCompound switch { "软胎" => 0, "硬胎" => 2, _ => 1 };
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        Calculate();
        if (_results.Count == 0) return;
        var dialog = new SaveFileDialog
        {
            Title = "导出策略报告",
            Filter = "文本报告 (*.txt)|*.txt",
            FileName = "Monoposto-Strategy-Report.txt",
            InitialDirectory = _dataDirectory
        };
        if (dialog.ShowDialog() != true) return;

        var settings = ReadSettings();
        var sb = new StringBuilder();
        sb.AppendLine("MONOPOSTO TYRE STRATEGY V1.0");
        sb.AppendLine($"赛道/备注：{settings.TrackName}");
        sb.AppendLine($"比赛圈数：{settings.RaceLaps}");
        sb.AppendLine($"基准单圈：{settings.BaseLapSeconds:0.00} 秒");
        sb.AppendLine($"进站损失：{settings.PitLossSeconds:0.0} 秒");
        sb.AppendLine($"起始轮胎：{settings.StartingCompound}");
        sb.AppendLine($"轮胎磨损（软/中/硬）：{settings.SoftWearPerLap:0.##}% / {settings.MediumWearPerLap:0.##}% / {settings.HardWearPerLap:0.##}% 每圈");
        sb.AppendLine();
        foreach (var result in _results)
        {
            sb.AppendLine($"{result.StrategyName} | 预计总用时 {result.TotalTimeText} | 进站 {result.Stops} 次");
            foreach (var stint in result.Stints)
                sb.AppendLine($"  第 {stint.Stint} 段：第 {stint.StartLap}-{stint.EndLap} 圈，{stint.Compound}，段末磨损估算 {stint.EndWear:0}%");
            sb.AppendLine();
        }
        sb.AppendLine("注意：此报告为简化模型估算，实际结果取决于游戏版本、赛道、天气、设置和驾驶表现。");
        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("策略报告已导出。", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string SafeFileName(string name)
    {
        foreach (char c in IOPath.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "Strategy" : name;
    }

    private sealed class StrategyDisplay
    {
        public string StrategyName { get; }
        public string TotalTimeText { get; }
        public int Stops { get; }
        public string Stints { get; }

        public StrategyDisplay(StrategyResult result)
        {
            StrategyName = result.StrategyName;
            TotalTimeText = result.TotalTimeText;
            Stops = result.Stops;
            Stints = string.Join(" → ", result.Stints.Select(s => $"{s.Compound} {s.StartLap}-{s.EndLap}圈"));
        }
    }
}
