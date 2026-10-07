using Overlayer.Module.ADOFAI.IO.File;
using Overlayer.Patch.Lazy;
using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Overlayer.Module.ADOFAI.Tag.Gameplay;

public static class Status {
    private static object Controller => GameAccess.Controller.Get(null);

    private static object Level() {
        var game = GameAccess.ScnGame.Get(null);
        if(game != null) {
            var level = GameAccess.GameLevelData.Get(game);
            if(level != null) return level;
        }
        var editor = GameAccess.ScnEditor.Get(null);
        return editor == null ? null : GameAccess.EditorLevelData.Get(editor);
    }

    [Tag(Desc = "Speed trial pitch")] public static double SpeedPitch => GameAccess.CurrentSpeedTrial.Get(null);
    [Tag(Desc = "Playing")] public static bool IsPlaying => Overlayer.ModuleAPI.PlaybackState.IsPlaying;
    [Tag(Desc = "Paused")] public static bool IsPaused => Overlayer.ModuleAPI.PlaybackState.IsPaused;
    [Tag(Desc = "Difficulty (localized)")] public static string Difficulty
        => LocalizedDifficulty(GameAccess.DifficultyValue.Get(null)?.ToString());
    [Tag(Desc = "Difficulty (raw)")] public static string DifficultyRaw
        => GameAccess.DifficultyValue.Get(null)?.ToString() ?? string.Empty;

    [Tag(Desc = "Playing started")] public static bool IsStarted
        => Controller != null && GameAccess.CurrentSeqID.Get(Controller) > GameAccess.CheckpointNum.Get(null);
    [Tag(Desc = "Autoplay enabled")] public static bool IsAutoEnabled => GameAccess.RDAuto.Get(GameAccess.RDData.Get(null));
    [Tag(Desc = "Practice mode enabled")] public static bool IsPracticeModeEnabled
        => GameAccess.PracticeMode.Get(null) || GameAccess.RDPractice.Get(GameAccess.RDData.Get(null));
    [Tag(Desc = "Old autoplay enabled")] public static bool IsOldAutoEnabled => GameAccess.RDOldAuto.Get(GameAccess.RDData.Get(null));
    [Tag(Desc = "No-fail enabled")] public static bool IsNoFailEnabled {
        get {
            var controller = Controller;
            if(controller != null) return GameAccess.NoFail.Get(controller);
            return GameAccess.UseNoFail.Get(null);
        }
    }
    [Tag(Desc = "Current tile is auto")] public static bool IsAutoTile {
        get {
            var floors = Progress.Floors();
            int idx = Progress.CurTile;
            return floors != null && idx >= 0 && idx < floors.Count && GameAccess.FloorAuto.Get(floors[idx]);
        }
    }
    [Tag(Desc = "Speed trial mode")] public static bool IsSpeedTrialEnabled => GameAccess.SpeedTrialMode.Get(null);
    [Tag(Desc = "Level editor open")] public static bool IsLevelEditor => GameAccess.IsLevelEditorFlag.Get(null);
    [Tag(Desc = "Official level")] public static bool IsOfficialLevel => GameAccess.IsOfficialLevelFlag.Get(null);
    [Tag(Desc = "In game world")] public static bool IsGameWorld {
        get {
            var conductor = GameAccess.Conductor.Get(null);
            return conductor != null && GameAccess.IsGameWorldFlag.Get(conductor);
        }
    }
    [Tag(Desc = "Attempts")] public static int Attempts {
        get {
            var scnGame = GameAccess.ScnGame.Get(null);
            if(scnGame == null) {
                var c = Controller;
                var cond = GameAccess.Conductor.Get(null);
                bool official = c != null && cond != null
                    && (GameAccess.SceneName.Get(null) ?? string.Empty).Contains("-")
                    && !GameAccess.NoFail.Get(c)
                    && GameAccess.IsGameWorldFlag.Get(cond);
                if(!official) return 0;
                return GameAccess.WorldAttemptsFn.TryInvoke(null, out object result, GameAccess.CurrentWorld.Get(c))
                    && result is int count ? count : 0;
            }
            var editor = GameAccess.ScnEditor.Get(null);
            if(editor != null) return 0;
            var level = Level();
            if(level == null) return 0;
            return GameAccess.CustomAttemptsFn.TryInvoke(null, out object custom, GameAccess.LevelHash.Get(level))
                && custom is int attempts ? attempts : 0;
        }
    }
    [Tag(Desc = "Session attempts (volatile)")]
    [NeedsPatch(typeof(Patch.SP_SessionAttemptLoad), typeof(Patch.SP_SessionAttemptPlay))]
    public static int SessionAttempts => SessionAttemptState.Count;
    [Tag(Desc = "[File] Attempts")]
    [NeedsPatch(typeof(Patch.SP_FileAttemptLoad), typeof(Patch.SP_FileAttemptPlay))]
    public static int FileAttempts => FileStoreState.Current.Data.Attempts;
    [Tag(Desc = "[File] Attempts for a tile, current tile if -1")]
    [NeedsPatch(typeof(Patch.SP_FileAttemptLoad), typeof(Patch.SP_FileAttemptPlay))]
    public static int FileTileAttempts(int tile = -1)
        => FileStoreState.Current.Data.GetTileAttempts(tile < 0 ? Progress.CurTile : tile);
    [Tag(Desc = "Tile of the last fail (session)")]
    [NeedsPatch(typeof(Patch.SP_FailHistoryLoad), typeof(Patch.SP_FailHistoryRecord))]
    public static int LastFailTile => Patch.FailHistoryState.LastTile;
    [Tag(Desc = "Progress of the last fail (0-1, session)")]
    [NeedsPatch(typeof(Patch.SP_FailHistoryLoad), typeof(Patch.SP_FailHistoryRecord))]
    public static double LastFailProgress => Patch.FailHistoryState.LastProgress;
    [Tag(Desc = "Progress of the last fail (%, session)")]
    [NeedsPatch(typeof(Patch.SP_FailHistoryLoad), typeof(Patch.SP_FailHistoryRecord))]
    public static double LastFailProgressPercent => Patch.FailHistoryState.LastProgress * 100d;
    [Tag(Desc = "Total fails (session)")]
    [NeedsPatch(typeof(Patch.SP_FailHistoryLoad), typeof(Patch.SP_FailHistoryRecord))]
    public static int SessionFails => Patch.FailHistoryState.Count;
    [Tag(Desc = "Fails on a tile (session), current tile if -1")]
    [NeedsPatch(typeof(Patch.SP_FailHistoryLoad), typeof(Patch.SP_FailHistoryRecord))]
    public static int FailCountAtTile(int tile = -1)
        => Patch.FailHistoryState.GetTileFails(tile < 0 ? Progress.CurTile : tile);
    [Tag(Desc = "[File] Runs started from tile 0 that reached a tile, current tile if -1")]
    [NeedsPatch(typeof(Patch.SP_FileAttemptLoad), typeof(Patch.SP_FileAttemptPlay), typeof(Patch.SP_FileRunReach), typeof(Patch.SP_FileRunFail), typeof(Patch.SP_FileRunWin))]
    public static int FileRunToHere(int tile = -1)
        => FileStoreState.Current.Data.GetReachCount(tile < 0 ? Progress.CurTile : tile);
    [Tag(Desc = "New map (no record)")]
    [NeedsPatch(typeof(Patch.SP_FileAttemptLoad), typeof(Patch.SP_FileAttemptPlay))]
    public static bool IsNewMap => !FileStoreState.Current.HasRecord && !FileStore.HasGameRecord();

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Last hit timing (ms)")]
    [NeedsPatch(typeof(Patch.SP_RecordTiming), typeof(Patch.SP_ResetTagState))]
    public static double TimingMs => GameplayState.Timing;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Average hit timing (ms)")]
    [NeedsPatch(typeof(Patch.SP_RecordTiming), typeof(Patch.SP_ResetTagState))]
    public static double TimingAvgMs => GameplayState.TimingAverage;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Timing Window Scale")] public static double MarginScale {
        get {
            var controller = Controller;
            var floor = controller == null ? null : GameAccess.CurrFloor.Get(controller);
            return floor == null ? 1d : GameAccess.FloorMarginScale.Get(floor, 1d);
        }
    }

    private static string LocalizedDifficulty(string name) {
        if(string.IsNullOrEmpty(name)) return string.Empty;
        if(GameAccess.RDStringGet.TryInvoke(null, out object result, $"enum.Difficulty.{name}")
            && result is string text) {
            return text;
        }
        return name;
    }

    internal static class GameplayState {
        internal static double Timing;
        internal static double BestProgress;
        internal static readonly List<double> Timings = new();
        internal static double TimingSum;

        internal static void RecordTiming(double value) {
            Timing = value;
            Timings.Add(value);
            TimingSum += value;
        }

        internal static double TimingAverage => Timings.Count == 0 ? 0 : TimingSum / Timings.Count;

        internal static void Reset() {
            Timing = 0;
            BestProgress = 0;
            TimingSum = 0;
            Timings.Clear();
        }
    }
}
