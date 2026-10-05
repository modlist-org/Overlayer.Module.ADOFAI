using HarmonyLib;
using Overlayer.Patch.Safe;
using Overlayer.UI;
using O5Kit.Control;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Patch;

public sealed class SP_BlockLegacyInput() : SafeConditionalPatch(nameof(SP_BlockLegacyInput)) {
    protected override bool ShouldApply() => Core.Config.BlockInputWhenOpened;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("RDInputType_Keyboard", "CheckKeyState", allowStatic: true);

    protected override HarmonyMethod Prefix() => new(typeof(SP_BlockLegacyInput)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static bool PrefixImpl(KeyCode key, ref bool __result) {
        if(!InputBlocker.IsOpen || (!O5InputBlocker.IsEditing && !IsMouseButton(key))) return true;
        __result = false;
        return false;
    }

    private static bool IsMouseButton(KeyCode key)
        => key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
}

public sealed class SP_BlockInputMethod(string typeName, string methodName)
    : SafeConditionalPatch($"{nameof(SP_BlockInputMethod)}.{typeName}.{methodName}") {
    protected override bool ShouldApply() => Core.Config.BlockInputWhenOpened;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe(typeName, methodName);
    protected override HarmonyMethod Prefix() => new(typeof(SP_BlockInputMethod)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static bool PrefixImpl() => !InputBlocker.IsOpen;
}

public sealed class SP_BlockDirectInput(string typeName, string methodName)
    : SafeConditionalPatch($"{nameof(SP_BlockDirectInput)}.{typeName}.{methodName}") {
    protected override bool ShouldApply() => Core.Config.BlockInputWhenOpened;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe(typeName, methodName);
    protected override HarmonyMethod Transpiler() => new(typeof(SP_BlockDirectInput)
        .GetMethod(nameof(TranspilerImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static IEnumerable<CodeInstruction> TranspilerImpl(IEnumerable<CodeInstruction> instructions) {
        foreach(CodeInstruction instruction in instructions) {
            if(instruction.operand is MethodInfo method &&
               method.DeclaringType == typeof(Input) &&
               InputBlocker.Replacements.TryGetValue(method, out MethodInfo replacement)) {
                instruction.operand = replacement;
            }
            yield return instruction;
        }
    }
}

internal static class InputBlocker {
    internal static bool IsOpen => UICore.CanvasObj != null && UICore.CanvasObj.activeInHierarchy;

    private static readonly object gate = new();
    private static Dictionary<MethodInfo, MethodInfo> replacements;

    internal static Dictionary<MethodInfo, MethodInfo> Replacements {
        get {
            lock(gate) {
                if(replacements == null) {
                    replacements = new Dictionary<MethodInfo, MethodInfo>();
                    AddReplacement(nameof(Input.GetKey), nameof(GetKey));
                    AddReplacement(nameof(Input.GetKeyDown), nameof(GetKeyDown));
                    AddReplacement(nameof(Input.GetKeyUp), nameof(GetKeyUp));
                }
                return replacements;
            }
        }
    }

    private static void AddReplacement(string inputName, string replacementName) {
        try {
            var original = typeof(Input).GetMethod(inputName, BindingFlags.Public | BindingFlags.Static, null, [typeof(KeyCode)], null);
            var replacement = typeof(InputBlocker).GetMethod(replacementName, BindingFlags.Static | BindingFlags.NonPublic);
            if(original == null || replacement == null) {
                Core.Logger.Wrn($"[InputBlocker] Skipping {inputName}: method not found, input blocking partially disabled.");
                return;
            }
            replacements[original] = replacement;
        } catch(Exception e) {
            Core.Logger.Wrn($"[InputBlocker] Skipping {inputName}: {e.Message}");
        }
    }

    private static bool GetKey(KeyCode key) => !IsOpen && Input.GetKey(key);
    private static bool GetKeyDown(KeyCode key) => !IsOpen && Input.GetKeyDown(key);
    private static bool GetKeyUp(KeyCode key) => !IsOpen && Input.GetKeyUp(key);
}
