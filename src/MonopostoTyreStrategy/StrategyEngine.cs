namespace MonopostoTyreStrategy;

public static class StrategyEngine
{
    public static List<StrategyResult> Compare(RaceSettings s)
    {
        Validate(s);
        var results = new List<StrategyResult>();
        for (int stops = 0; stops <= 3; stops++)
            results.Add(Build(s, stops));
        return results.OrderBy(x => x.TotalSeconds).ToList();
    }

    public static StrategyResult Build(RaceSettings s, int stops)
    {
        Validate(s);
        stops = Math.Clamp(stops, 0, 3);
        int stints = stops + 1;
        int baseLaps = s.RaceLaps / stints;
        int remainder = s.RaceLaps % stints;
        var rows = new List<StintRow>();
        int startLap = 1;
        double total = 0;

        for (int i = 0; i < stints; i++)
        {
            int stintLaps = baseLaps + (i < remainder ? 1 : 0);
            string compound = ChooseCompound(s, i, stints, stintLaps);
            double wearRate = WearRate(s, compound);
            double paceDelta = PaceDelta(s, compound);
            double endWear = Math.Min(100, wearRate * stintLaps);
            // Linear degradation model: each 1% wear adds 0.012 sec/lap after the first lap.
            double stintTime = stintLaps * (s.BaseLapSeconds + paceDelta)
                               + (wearRate * stintLaps * Math.Max(0, stintLaps - 1) * 0.006);
            total += stintTime;
            rows.Add(new StintRow
            {
                Stint = i + 1,
                StartLap = startLap,
                EndLap = startLap + stintLaps - 1,
                Laps = stintLaps,
                Compound = compound,
                EndWear = endWear,
                EstimatedSeconds = stintTime
            });
            startLap += stintLaps;
        }

        total += stops * s.PitLossSeconds;
        return new StrategyResult { Stops = stops, TotalSeconds = total, Stints = rows };
    }

    public static double WearRate(RaceSettings s, string compound) => compound switch
    {
        "软胎" => s.SoftWearPerLap,
        "硬胎" => s.HardWearPerLap,
        _ => s.MediumWearPerLap
    };

    public static double PaceDelta(RaceSettings s, string compound) => compound switch
    {
        "软胎" => s.SoftPaceDeltaSeconds,
        "硬胎" => s.HardPaceDeltaSeconds,
        _ => s.MediumPaceDeltaSeconds
    };

    private static string ChooseCompound(RaceSettings s, int stintIndex, int stintCount, int stintLaps)
    {
        if (stintIndex == 0 && !string.IsNullOrWhiteSpace(s.StartingCompound))
            return s.StartingCompound;
        // Simple default: use medium for middle stints and hard for longer final stints.
        if (stintLaps >= 14) return "硬胎";
        if (stintLaps <= 8) return "软胎";
        return "中胎";
    }

    private static void Validate(RaceSettings s)
    {
        s.RaceLaps = Math.Clamp(s.RaceLaps, 1, 200);
        s.BaseLapSeconds = Math.Clamp(s.BaseLapSeconds, 20, 300);
        s.PitLossSeconds = Math.Clamp(s.PitLossSeconds, 0, 120);
        s.SoftWearPerLap = Math.Clamp(s.SoftWearPerLap, 0.1, 100);
        s.MediumWearPerLap = Math.Clamp(s.MediumWearPerLap, 0.1, 100);
        s.HardWearPerLap = Math.Clamp(s.HardWearPerLap, 0.1, 100);
    }
}
