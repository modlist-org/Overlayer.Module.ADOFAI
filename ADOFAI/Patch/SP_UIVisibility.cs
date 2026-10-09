using HarmonyLib;
using Overlayer.Module.ADOFAI.UI;
using Overlayer.Patch.Safe;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public sealed class SP_HideBuildText() : SafeConditionalPatch(nameof(SP_HideBuildText)) {
    protected override bool ShouldApply() => Core.Config.HideBuildText;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrEnableIfBeta", "Awake");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HideBuildText).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.HideBuildText(__instance);
}

public sealed class SP_HideEditorIcons() : SafeConditionalPatch(nameof(SP_HideEditorIcons)) {
    protected override bool ShouldApply() => Core.Config.HideEditorIcons;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scnEditor", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HideEditorIcons).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.ApplyEditorIcons(__instance);
}

public sealed class SP_HidePause() : SafeConditionalPatch(nameof(SP_HidePause)) {
    protected override bool ShouldApply() => Core.Config.HidePause;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrUIController", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HidePause).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.ApplyPause(__instance);
}

public sealed class SP_HidePauseHint() : SafeConditionalPatch(nameof(SP_HidePauseHint)) {
    protected override bool ShouldApply() => Core.Config.HidePause;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scnEditor", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HidePauseHint).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.HidePauseHint(__instance);
}

public sealed class SP_HidePauseButton() : SafeConditionalPatch(nameof(SP_HidePauseButton)) {
    protected override bool ShouldApply() => Core.Config.HidePause;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrUIController", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HidePauseButton).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.ApplyPause(__instance);
}

public sealed class SP_HideAllGameUI() : SafeConditionalPatch(nameof(SP_HideAllGameUI)) {
    protected override bool ShouldApply() => Core.Config.HideAll;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrUIController", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HideAllGameUI).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.ApplyAllGameUI(__instance);
}

public sealed class SP_HideAllEditorUI() : SafeConditionalPatch(nameof(SP_HideAllEditorUI)) {
    protected override bool ShouldApply() => Core.Config.HideAll;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scnEditor", "Update");
    protected override HarmonyMethod Postfix() => new(typeof(SP_HideAllEditorUI).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(object __instance) => UIVisibility.ApplyAllGameUI(__instance);
}
