using Overlayer.Utility.Access;
using System.Collections.Generic;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.UI;

internal static class UIVisibility {
    private static bool captured;
    private static bool originalNoHud;
    private static bool originalNoAutoHud;

    private static readonly Dictionary<int, (GameObject Object, bool Active)> hiddenBuildTexts = [];
    private static readonly Dictionary<int, (GameObject Object, bool Active)> hiddenEditorIcons = [];
    private static readonly Dictionary<int, (GameObject Object, bool Active)> hiddenPauseButtons = [];
    private static readonly Dictionary<int, (GameObject Object, bool Active)> hiddenPauseHints = [];
    private static readonly Dictionary<int, (Canvas Canvas, bool Enabled)> hiddenGameCanvases = [];

    public static void CaptureOriginals() {
        if(captured) return;
        GameAccess.NoHud.TryGet(null, out originalNoHud);
        GameAccess.NoAutoHud.TryGet(null, out originalNoAutoHud);
        captured = true;
    }

    public static void ApplyNative() {
        CaptureOriginals();
        GameAccess.NoHud.TrySet(null, originalNoHud || Core.Config.HideAll);
        GameAccess.NoAutoHud.TrySet(null, originalNoAutoHud || Core.Config.HideAutoplay);
    }

    public static void ApplyBuildTextNow() {
        if(!Core.Config.HideBuildText) {
            RestoreBuildText();
            return;
        }
        try {
            var type = SafeAccess.FindType("scrEnableIfBeta");
            if(type == null) return;
            foreach(var value in Resources.FindObjectsOfTypeAll(type)) {
                if(value is not Component component || !component || !component.gameObject.scene.IsValid()) continue;
                Hide(component.gameObject, hiddenBuildTexts);
            }
        } catch { }
    }

    public static void RestoreAll() {
        if(captured) {
            GameAccess.NoHud.TrySet(null, originalNoHud);
            GameAccess.NoAutoHud.TrySet(null, originalNoAutoHud);
        }
        Restore(hiddenBuildTexts);
        Restore(hiddenEditorIcons);
        Restore(hiddenPauseButtons);
        Restore(hiddenPauseHints);
        RestoreCanvases();
        captured = false;
    }

    public static void ApplyAllGameUI(object instance) {
        if(!Core.Config.HideAll) {
            RestoreCanvases();
            return;
        }
        HideCanvases(GameObjectOf(instance));
    }

    private static void HideCanvases(GameObject root) {
        if(!root) return;
        try {
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)) {
                if(!canvas) continue;
                int id = canvas.GetInstanceID();
                if(!hiddenGameCanvases.ContainsKey(id)) {
                    hiddenGameCanvases[id] = (canvas, canvas.enabled);
                }
                canvas.enabled = false;
            }
        } catch { }
    }

    private static void RestoreCanvases() {
        foreach(var entry in hiddenGameCanvases.Values) {
            try {
                if(entry.Canvas) entry.Canvas.enabled = entry.Enabled;
            } catch { }
        }
        hiddenGameCanvases.Clear();
    }

    public static void RestoreEditorIcons() => Restore(hiddenEditorIcons);

    public static void RestorePauseButtons() => Restore(hiddenPauseButtons);
    public static void RestorePauseHints() => Restore(hiddenPauseHints);

    public static void HideBuildText(object instance) {
        if(!Core.Config.HideBuildText || instance == null) return;
        try {
            if(!SafeAccess.TryRead(instance, "gameObject", out object value) || value is not GameObject go || !go) return;
            Hide(go, hiddenBuildTexts);
        } catch { }
    }

    public static void RestoreBuildText() => Restore(hiddenBuildTexts);

    public static void ApplyEditorIcons(object instance) {
        if(instance == null) return;
        if(!Core.Config.HideEditorIcons) {
            Restore(hiddenEditorIcons);
            return;
        }
        try {
            HideMember(instance, "editorDifficultySelector", hiddenEditorIcons);
            HideMember(instance, "buttonAuto", hiddenEditorIcons);
            HideMember(instance, "autoImage", hiddenEditorIcons);
            HideMember(instance, "buttonNoFail", hiddenEditorIcons);
            HideMember(instance, "buttonUnlockKeyLimiter", hiddenEditorIcons);
        } catch { }
    }

    public static void ApplyPause(object instance) {
        if(instance == null) return;
        try {
            if(SafeAccess.TryRead(instance, "pauseButton", out object value)) {
                var go = GameObjectOf(value);
                if(Core.Config.HidePause) {
                    if(go) Hide(go, hiddenPauseButtons);
                } else {
                    Restore(hiddenPauseButtons);
                }
            }
        } catch { }
    }

    public static void HidePauseHint(object instance) {
        if(instance == null) return;
        try {
            if(GameAccess.EditorControlsTip.TryGet(instance, out object tip) && tip != null) {
                var go = GameObjectOf(tip);
                if(Core.Config.HidePause) {
                    if(go) Hide(go, hiddenPauseHints);
                } else {
                    Restore(hiddenPauseHints);
                }
            }
        } catch { }
    }

    private static void HideMember(object instance, string member, Dictionary<int, (GameObject Object, bool Active)> cache) {
        if(!SafeAccess.TryRead(instance, member, out object value)) return;
        var go = GameObjectOf(value);
        if(go) Hide(go, cache);
    }

    private static GameObject GameObjectOf(object value) {
        if(value is GameObject go) return go;
        if(value is Component component) return component.gameObject;
        if(value != null && SafeAccess.TryRead(value, "gameObject", out object gameObject)) return gameObject as GameObject;
        return null;
    }

    private static void Hide(GameObject go, Dictionary<int, (GameObject Object, bool Active)> cache) {
        if(!go) return;
        int id = go.GetInstanceID();
        if(!cache.ContainsKey(id)) cache[id] = (go, go.activeSelf);
        if(go.activeSelf) go.SetActive(false);
    }

    private static void Restore(Dictionary<int, (GameObject Object, bool Active)> cache) {
        foreach(var entry in cache.Values) {
            try {
                if(entry.Object) entry.Object.SetActive(entry.Active);
            } catch { }
        }
        cache.Clear();
    }
}
