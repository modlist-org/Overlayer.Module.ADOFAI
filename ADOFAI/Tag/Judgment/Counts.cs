using Overlayer.Tag.Core;
using Overlayer.Utility.Access;

namespace Overlayer.Module.ADOFAI.Tag.Judgment;

public static class Counts {
    private static int snapshotFrame = -1;
    private static object snapshotTracker;
    private static readonly System.Collections.Generic.Dictionary<string, int> snapshotCounts = new(System.StringComparer.Ordinal);

    private static object Tracker() {
        var controller = GameAccess.Controller.Get(null);
        if(controller == null) return null;
        var player = GameAccess.PlayerOne.Get(controller);
        return player == null ? null : GameAccess.MarginTracker.Get(player);
    }

    private static object SnapshotTracker() {
        int frame = UnityEngine.Time.frameCount;
        if(snapshotFrame != frame) {
            snapshotFrame = frame;
            snapshotTracker = Tracker();
            snapshotCounts.Clear();
        }
        return snapshotTracker;
    }

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Too Early")]     public static int TE => CurrentCount("TooEarly");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Very Early")]    public static int VE => CurrentCount("VeryEarly");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Early Perfect")] public static int EP => CurrentCount("EarlyPerfect");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Perfect Minus")] public static int PM => CurrentCount("PerfectMinus");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "XPerfect")]      public static int XP => CurrentCount("XPerfect") + A;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Perfect Plus")]  public static int PP => CurrentCount("PerfectPlus");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Late Perfect")]  public static int LP => CurrentCount("LatePerfect");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Very Late")]     public static int VL => CurrentCount("VeryLate");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Too Late")]      public static int TL => CurrentCount("TooLate");

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Auto")]                           public static int A => CurrentCount("Auto");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Pure XPerfect (excluding Auto)")] public static int PXP => CurrentCount("XPerfect");

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Perfect (XP + IP)")]                  public static int P => XP + IP;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Fast (TE + VE + EP + PM)")]           public static int Fast => TE + VE + EP + PM;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Slow (PP + LP + VL + TL)")]           public static int Slow => PP + LP + VL + TL;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Inner Perfects (PM + PP)")]           public static int IP => PM + PP;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Outer Perfects (EP + LP)")]           public static int OP => EP + LP;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Very Early & Very Late (VE + VL)")]   public static int V => VE + VL;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Too Early & Too Late (TE + TL)")]     public static int T => TE + TL;

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Number of Misses")]       public static int Miss => CurrentCount("FailMiss");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Number of Overloads")]    public static int Overload => CurrentCount("FailOverload");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Total Deaths/Fails")]     public static int Fail => Deaths();
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Number of Multipresses")] public static int Multipress => CurrentCount("Multipress");
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Number of OverPress")]    public static int OverPress => CurrentCount("OverPress");

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Accuracy (0-1)")]  public static double Accuracy => GameAccess.PercentAcc.Get(SnapshotTracker(), float.NaN);
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "XAccuracy (0-1)")] public static double XAccuracy => GameAccess.PercentXAcc.Get(SnapshotTracker(), float.NaN);
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Accuracy (%)")]  public static double AccuracyPercent => Accuracy * 100d;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "XAccuracy (%)")] public static double XAccuracyPercent => XAccuracy * 100d;

    // The game's own maxPossibleXAcc, with the same checkpoint penalty percentXAcc gets outside the editor.
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max reachable XAccuracy if every remaining tile is XPerfect (0-1)")]
    public static double MaxXAccuracy {
        get {
            double max = GameAccess.MaxPossibleXAcc.Get(SnapshotTracker(), float.NaN);
            if(GameAccess.ScnEditor.Get(null) != null) return max;
            var controller = GameAccess.Controller.Get(null);
            int checkpoints = controller == null ? 0 : GameAccess.CheckpointsUsedCount.Get(controller);
            return max * System.Math.Pow(0.9875, checkpoints);
        }
    }
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max reachable XAccuracy (%)")] public static double MaxXAccuracyPercent => MaxXAccuracy * 100d;

    // Mirrors scrMarginTracker.CalculatePercentAcc with every remaining tile counted as a Perfect.
    // ponytail: remaining hits = LeftTile, so holds/midspins can skew it slightly; read the tracker's floor counts if that matters.
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max reachable Accuracy if every remaining tile is Perfect (0-1)")]
    public static double MaxAccuracy {
        get {
            var tracker = SnapshotTracker();
            if(tracker == null) return float.NaN;
            int perfect = PXP + PM + PP + A + CurrentCount("Midspin") + Multipress;
            int counted = perfect + EP + LP;
            int total = (GameAccess.HitMargins.Get(tracker) is System.Collections.ICollection hits ? hits.Count : 0) + Deaths();
            int left = Gameplay.Progress.LeftTile;
            double ratio = counted + left == total + left ? 1d : (double)(counted + left) / (total + left);
            return ratio + (perfect + left - CurrentCount("FailedFloor")) * 0.0001;
        }
    }
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max reachable Accuracy (%)")] public static double MaxAccuracyPercent => MaxAccuracy * 100d;

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "X-Score")]       public static int XScore => GameAccess.XScoreValue.Get(SnapshotTracker(), 0);
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max X-Score")]   public static int MaxXScore => GameAccess.MaxXScoreValue.Get(SnapshotTracker(), 0);
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Last X-Score")]  public static int LastXScore => GameAccess.LastXScoreValue.Get(SnapshotTracker(), 0);

    private static int CurrentCount(string margin) {
        var tracker = SnapshotTracker();
        if(tracker == null) return 0;
        if(!snapshotCounts.TryGetValue(margin, out int count)) {
            var value = GameAccess.ParseHitMargin(margin);
            count = value == null ? 0
                : SafeAccess.TryCall(tracker, "GetHits", out object result, value) && result is int c ? c : 0;
            snapshotCounts[margin] = count;
        }
        return count;
    }

    private static int Deaths() {
        var tracker = SnapshotTracker();
        if(tracker == null) return 0;
        return SafeAccess.TryCall(tracker, "GetDeaths", out object result) && result is int count
            ? count
            : 0;
    }
}
