using HarmonyLib;
using Overlayer.Module.ADOFAI.IO.File;
using Overlayer.Module.ADOFAI.Tag.Gameplay;
using Overlayer.Patch.Safe;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public class SP_FileAttemptLoad() : SafeConditionalPatch(nameof(SP_FileAttemptLoad)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scnGame", "LoadLevel");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FileAttemptLoad)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl() => FileStoreState.Current.Load();
}

public class SP_FileAttemptPlay() : SafeConditionalPatch(nameof(SP_FileAttemptPlay)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scnGame", "Play");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FileAttemptPlay)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl(int seqID) {
        FileStoreState.Current.Data.Increase(seqID);
        FileRunState.Start(seqID);
        FileStoreState.Current.Save();
    }
}

public static class FileRunState {
    private static bool active;
    private static int lastTile;
    private static int unsavedTiles;

    public static void Start(int seqID) {
        active = seqID == 0;
        lastTile = 0;
        unsavedTiles = 0;
        if(active) FileStoreState.Current.Data.Reach(0);
    }

    // Counts every tile passed since the last call, so skipped hits (multi-tile frames) still register.
    public static void CatchUp() {
        if(!active) return;
        int cur = Progress.CurTile;
        for(int t = lastTile + 1; t <= cur; t++) FileStoreState.Current.Data.Reach(t);
        if(cur > lastTile) {
            unsavedTiles += cur - lastTile;
            lastTile = cur;
        }
        // Quitting to menu skips FailAction/Won_Enter, so persist periodically.
        if(unsavedTiles >= 32) {
            unsavedTiles = 0;
            FileStoreState.Current.Save();
        }
    }

    public static void End() {
        if(!active) return;
        CatchUp();
        active = false;
        unsavedTiles = 0;
        FileStoreState.Current.Save();
    }
}

public class SP_FileRunReach() : SafeConditionalPatch(nameof(SP_FileRunReach)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrPlanet", "SwitchChosen");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FileRunReach)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl() => FileRunState.CatchUp();
}

public class SP_FileRunFail() : SafeConditionalPatch(nameof(SP_FileRunFail)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrController", "FailAction");

    protected override HarmonyMethod Prefix() => new HarmonyMethod(typeof(SP_FileRunFail)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PrefixImpl() => FileRunState.End();
}

public class SP_FileRunFail2() : SafeConditionalPatch(nameof(SP_FileRunFail2)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrController", "Fail2Action");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FileRunFail2)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl() => FileRunState.End();
}

public class SP_FileRunWin() : SafeConditionalPatch(nameof(SP_FileRunWin)) {
    protected override bool ShouldApply() => Core.Config.FileFeature;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrController", "Won_Enter");

    protected override HarmonyMethod Postfix() => new HarmonyMethod(typeof(SP_FileRunWin)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl() => FileRunState.End();
}
