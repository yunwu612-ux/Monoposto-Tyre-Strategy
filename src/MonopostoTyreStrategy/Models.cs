using System.Text.Json.Serialization;

namespace MonopostoTyreStrategy;

public sealed class RaceSettings
{
    public string TrackName { get; set; } = "自定义赛道";
    public int RaceLaps { get; set; } = 35;
    public double BaseLapSeconds { get; set; } = 90.0;
    public double PitLossSeconds { get; set; } = 22.0;
    public double TargetWearPercent { get; set; } = 80.0;
    public double SoftWearPerLap { get; set; } = 13.0;
    public double MediumWearPerLap { get; set; } = 9.0;
    public double HardWearPerLap { get; set; } = 6.0;
    // Positive means slower than baseline. Negative means faster.
    public double SoftPaceDeltaSeconds { get; set; } = -0.45;
    public double MediumPaceDeltaSeconds { get; set; } = 0.0;
    public double HardPaceDeltaSeconds { get; set; } = 0.55;
    public string StartingCompound { get; set; } = "中胎";
}

public sealed class StintRow
{
    public int Stint { get; set; }
    public int StartLap { get; set; }
    public int EndLap { get; set; }
    public int Laps { get; set; }
    public string Compound { get; set; } = "";
    public double EndWear { get; set; }
    public double EstimatedSeconds { get; set; }
    public string EndWearText => $"{EndWear:0}%";
    public string EstimatedTimeText => TimeSpan.FromSeconds(Math.Max(0, EstimatedSeconds)).ToString(@"hh\:mm\:ss");
}

public sealed class StrategyResult
{
    public int Stops { get; init; }
    public double TotalSeconds { get; init; }
    public List<StintRow> Stints { get; init; } = [];
    public string TotalTimeText => TimeSpan.FromSeconds(Math.Max(0, TotalSeconds)).ToString(@"hh\:mm\:ss");
    public string StrategyName => Stops switch { 0 => "零停站", 1 => "一停", 2 => "二停", _ => $"{Stops} 停" };
}

public sealed class SavedProfile
{
    public string Name { get; set; } = "我的策略";
    public RaceSettings Settings { get; set; } = new();
    public DateTime SavedAt { get; set; } = DateTime.Now;
}
