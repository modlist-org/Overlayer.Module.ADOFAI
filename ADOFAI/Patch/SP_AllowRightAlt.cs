using HarmonyLib;
using Overlayer.Patch.Safe;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Patch;

// RDInputType_Keyboard.SpecialKeys contains RightAlt, so CountSpecialInput strips it from hit input.
public sealed class SP_AllowRightAlt() : SafeConditionalPatch(nameof(SP_AllowRightAlt)) {
    protected override bool ShouldApply() => Core.Config.AllowRightAlt;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("RDInputType_Keyboard", "CountSpecialInput");

    protected override HarmonyMethod Postfix() => new(typeof(SP_AllowRightAlt)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl(List<KeyCode> __result) => __result?.Remove(KeyCode.RightAlt);
}
