using HarmonyLib;
using Overlayer.Module.ADOFAI.Tag.Gameplay;
using Overlayer.Patch.Safe;
using System.Collections.Generic;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public static class FailHistoryState {
    public static int LastTile { get; private set; }
    public static double LastProgress { get; private set; }
    public static int Count { get; private set; }
    private static readonly Dictionary<int, int> TileFails = [];

    public static int GetTileFails(int tile) => TileFails.TryGetValue(tile, out var count) ? count : 0;

    public static void Record(int tile, double progress) {
        LastTile = tile;
        LastProgress = progress;
        Count++;
        TileFails[tile] = GetTileFails(tile) + 1;
    }

    public static void Reset() {
        LastTile = 0;
        LastProgress = 0;
        Count = 0;
        TileFails.Clear();
    }
}

public class SP_FailHistoryLoad() : SafeConditionalPatch(nameof(SP_FailHistoryLoad)) {
    protected override bool ShouldApply() => true;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scnGame", "LoadLevel");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FailHistoryLoad)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl() => FailHistoryState.Reset();
}

public class SP_FailHistoryRecord() : SafeConditionalPatch(nameof(SP_FailHistoryRecord)) {
    protected override bool ShouldApply() => true;

    // Every death (miss, overload, multipress, hitbox) ends up in FailAction.
    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrController", "FailAction");

    protected override HarmonyMethod Prefix() => new HarmonyMethod(typeof(SP_FailHistoryRecord)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PrefixImpl() {
        FailHistoryState.Record(Progress.CurTile, Progress.TileProgress);
        var conductor = GameAccess.Conductor.Get(null);
        if(conductor != null) Status.GameplayState.DeathSongPosition ??= GameAccess.SongPosition.Get(conductor);
    }
}
