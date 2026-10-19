using HarmonyLib;
using Overlayer.Patch.Safe;
using SkyHook;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Patch;

// The game strips "special" keys from hit input:
// legacy RDInputType_Keyboard.CountSpecialInput (Alt, Win, Tab, End, F1-F12, PrtSc)
// async RDInputType_AsyncKeyboard.GetSpecialInput (LAlt, Super, F12, PrtSc).
// Removing a key from the returned list lets it count as a hit key.
public static class SpecialKeys {
    public sealed record Option(string Id, string Name, KeyCode[] Legacy, KeyLabel[] Async);

    public static readonly Option[] Options = [
        new("LeftAlt", "Left Alt", [KeyCode.LeftAlt], [KeyLabel.LAlt]),
        new("RightAlt", "Right Alt", [KeyCode.RightAlt], []),
        new("Windows", "Windows / Command", [KeyCode.LeftWindows, KeyCode.RightWindows, KeyCode.LeftCommand, KeyCode.RightCommand], [KeyLabel.Super]),
        new("Tab", "Tab", [KeyCode.Tab], []),
        new("End", "End", [KeyCode.End], []),
        new("F1_F11", "F1 - F11", [KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11], []),
        new("F12", "F12", [KeyCode.F12], [KeyLabel.F12]),
        new("PrintScreen", "Print Screen", [KeyCode.Print, KeyCode.SysReq], [KeyLabel.PrintScreen]),
    ];

    internal static readonly HashSet<KeyCode> Legacy = [];
    internal static readonly HashSet<KeyLabel> Async = [];

    public static bool Any => Legacy.Count > 0;

    public static void Refresh() {
        Legacy.Clear();
        Async.Clear();
        foreach(var o in Options.Where(o => Core.Config.AllowedSpecialKeys.Contains(o.Id))) {
            Legacy.UnionWith(o.Legacy);
            Async.UnionWith(o.Async);
        }
    }
}

public sealed class SP_AllowSpecialKeys() : SafeConditionalPatch(nameof(SP_AllowSpecialKeys)) {
    protected override bool ShouldApply() => SpecialKeys.Any;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("RDInputType_Keyboard", "CountSpecialInput");

    protected override HarmonyMethod Postfix() => new(typeof(SP_AllowSpecialKeys)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl(List<KeyCode> __result) {
        if(__result == null) return;
        for(int i = __result.Count - 1; i >= 0; i--) {
            if(SpecialKeys.Legacy.Contains(__result[i])) __result.RemoveAt(i);
        }
    }
}

public sealed class SP_AllowSpecialKeysAsync() : SafeConditionalPatch(nameof(SP_AllowSpecialKeysAsync)) {
    protected override bool ShouldApply() => SpecialKeys.Async.Count > 0;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("RDInputType_AsyncKeyboard", "GetSpecialInput");

    protected override HarmonyMethod Postfix() => new(typeof(SP_AllowSpecialKeysAsync)
        .GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static FieldInfo? _label;

    // void GetSpecialInput(List<AsyncKeyCode> keys) fills `keys` in place.
    // Assembly-CSharp isn't referenced, so read AsyncKeyCode.label reflectively.
    private static void PostfixImpl(object keys) {
        if(keys is not IList list) return;
        for(int i = list.Count - 1; i >= 0; i--) {
            object item = list[i];
            _label ??= item.GetType().GetField("label");
            if(_label?.GetValue(item) is KeyLabel label && SpecialKeys.Async.Contains(label)) list.RemoveAt(i);
        }
    }
}
