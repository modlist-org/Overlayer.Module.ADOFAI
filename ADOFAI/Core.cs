using Overlayer.Compat;
using Overlayer.Core;
using Overlayer.IO;
using Overlayer.Localization;
using Overlayer.Module.ADOFAI.IO;
using Overlayer.Module.ADOFAI.Patch;
using Overlayer.Module.ADOFAI.UI;
using Overlayer.ModuleAPI;
using Overlayer.Patch.Safe;
using Overlayer.Resource;
using Overlayer.UI;
using Overlayer.UI.Factory;
using Overlayer.Utility.Access;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Overlayer.Module.ADOFAI;

public class Core : OverlayerModule {
    private IDisposable? playbackStateRegistration;
    private IDisposable? pausedStateRegistration;
    private IDisposable? textFontRegistration;
    private static TMP_FontAsset? defaultTextFont;
    public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
    public static OverlayerLogger Logger { get; } = new(MainCore.Host.OverlayerLogger, "ADOFAI Module");
    public static SettingsFile<ADOFAISettings> ConfigFile { get; } = new(Path.Combine(MainCore.Paths.ModulePath, "ADOFAI/Settings.json"));
    public static ADOFAISettings Config => ConfigFile.Data;
    public static Translator Tr { get; private set; } = new();

    public static ResourceManager Res { get; } = new(Assembly, "Overlayer.Module.ADOFAI.Resource.Embedded.");
    public static SpriteManager Spr { get; } = new(Res);

    private static void LoadTr()
        => _ = Tr.Load(new(Path.Combine(MainCore.Paths.ModulePath, "ADOFAI/Lang")));

    private void OnLanguageChanged(string lang)
        => Tr.Language = lang;

    private const string SupportedGameVersionPrefix = "3.4";

    private static void CheckGameVersion() {
        try {
            string gameVersion = UnityEngine.Application.version;
            if(string.IsNullOrWhiteSpace(gameVersion)
                || !gameVersion.StartsWith(SupportedGameVersionPrefix + ".", StringComparison.Ordinal)) {
                Logger.Wrn($"Untested game version '{gameVersion}' (supports {SupportedGameVersionPrefix}.x). Tags may misbehave after a game update.");
            } else {
                Logger.Msg($"Game version: {gameVersion}");
            }
        } catch {
        }
    }

    public static bool IsPlaying {
        get {
            var cdt = GameAccess.Conductor.Get(null);
            var ctrl = GameAccess.Controller.Get(null);
            var edt = GameAccess.ScnEditor.Get(null);

            if(cdt == null || !GameAccess.IsGameWorldFlag.Get(cdt)) {
                return false;
            }

            if(ctrl == null) {
                return false;
            }

            if(!GameAccess.Paused.Get(ctrl)) {
                return true;
            }

            return edt != null && GameAccess.EditorPausedInPlayMode.Get(edt);
        }
    }

    public override void OnInitialize() {
        Tr.SetLog(Logger.Msg);

        MainCore.Tr.OnLoadStart += LoadTr;
        MainCore.Tr.OnLanguageChanged += OnLanguageChanged;

        LoadTr();
        Tr.Language = MainCore.Tr.Language;

        ConfigFile.Load();
        UIVisibility.CaptureOriginals();

        CheckGameVersion();

        SafeAccess.ModeOverride = Config.LazyAccess ? SafeResolveMode.Lazy : null;
        playbackStateRegistration = PlaybackState.Register(() => {
            return IsPlaying;
        });
        pausedStateRegistration = PlaybackState.RegisterPaused(() => {
            var controller = GameAccess.Controller.Get(null);
            return controller != null && GameAccess.Paused.Get(controller);
        });
        textFontRegistration = TextFontProvider.Register(() => {
            if(defaultTextFont == null) {
                object fontData = null;
                if(GameAccess.FontDataForLanguage.TryInvoke(null, out object result, UnityEngine.SystemLanguage.English)) {
                    fontData = result;
                }
                Font sourceFont = fontData != null && SafeAccess.TryRead(fontData, "font", out object fontObj)
                    ? fontObj as Font
                    : null;
                if(sourceFont == null) {
                    return defaultTextFont;
                }
                defaultTextFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    100,
                    10,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024
                );
            }
            return defaultTextFont;
        });

        GameAccess.DontShowTitles.TrySet(null, Config.HideTitle);

        SafePatchController.Add(new SP_BlockAsyncInput());
        SafePatchController.Add(new SP_BlockLegacyInput());
        SafePatchController.Add(new SP_BlockInputMethod("OptionsPanelsCLS", "CheckInputs"));
        SafePatchController.Add(new SP_BlockDirectInput("scnLevelSelect", "Update"));
        SafePatchController.Add(new SP_BlockDirectInput("scnLevelSelectTaro", "Update"));
        SafePatchController.Add(new SP_BlockDirectInput("scnTaroMenu2", "Update"));
        SafePatchController.Add(new SP_BlockDirectInput("scnTaroMenu3", "Update"));
        SafePatchController.Add(new SP_LinuxTMPKeyInput());
        SafePatchController.Add(new SP_LinuxLegacyKeyInput());
        SafePatchController.Add(new SP_ShowAutoJudgment());
        SafePatchController.Add(new SP_AllowRightAlt());
        SafePatchController.Add(new SP_ResetTagState());
        SafePatchController.Add(new SP_RecordTiming());
        SafePatchController.Add(new SP_SessionAttemptLoad());
        SafePatchController.Add(new SP_SessionAttemptPlay());
        SafePatchController.Add(new SP_FileAttemptLoad());
        SafePatchController.Add(new SP_FileAttemptPlay());
        SafePatchController.Add(new SP_HideBuildText());
        SafePatchController.Add(new SP_HideEditorIcons());
        SafePatchController.Add(new SP_HidePause());
        SafePatchController.Add(new SP_HidePauseHint());
        SafePatchController.Add(new SP_HidePauseButton());
        SafePatchController.Add(new SP_HideAllGameUI());
        SafePatchController.Add(new SP_HideAllEditorUI());
        if(!Config.LazyPatches) {
            SafePatchController.ApplyAll();
        }
        // Linux input fix is never lazy: no tag triggers it, so lazy mode
        // would leave it off until the toggle is cycled. Apply() is
        // self-guarded (platform + config) and idempotent under ApplyAll.
        foreach(var patch in SafePatchController.Get<SP_LinuxTMPKeyInput>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_LinuxLegacyKeyInput>()) patch.Apply();
        // Same for config-gated non-tag patches: toggles only ApplyState on
        // change, so a saved true would start unpatched in lazy mode.
        // Apply() no-ops when its config is off.
        foreach(var patch in SafePatchController.Get<SP_BlockAsyncInput>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_BlockLegacyInput>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_BlockInputMethod>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_BlockDirectInput>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_ShowAutoJudgment>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_AllowRightAlt>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_FileAttemptLoad>()) patch.Apply();
        foreach(var patch in SafePatchController.Get<SP_FileAttemptPlay>()) patch.Apply();
        ApplyUiVisibility();

        Overlayer.Package.PresetStore.Register(new Overlayer.Package.PackagePreset {
            Id = "module:ADOFAI:Moon",
            Name = "Moon",
            Module = Info.Name,
            ReadBytes = ReadMoonPreset,
        });

        try { Tag.Input.Key.EnsureFeed(); } catch { }

        MainCore.Cam.CustomCameraProvider = () => {
            var cam = GameAccess.Cam.Get(null);
            var camobj = cam == null ? null : GameAccess.CamObj.Get(cam);
            return camobj as UnityEngine.Camera;
        };

        MainUI.CreateInputBlocker(UICore.CanvasObj.transform);
        MainUI.CreateMenu(UICore.MenuContent);
        MainUI.CreatePage(PageFactory.CreatePageBase(100));
    }

    public override void OnDispose() {
        Overlayer.Package.PresetStore.UnregisterByModule(Info.Name);

        GameAccess.DontShowTitles.TrySet(null, false);
        UIVisibility.RestoreAll();

        RemoveModulePatches();

        try {
            if(MainCore.Cam != null) MainCore.Cam.CustomCameraProvider = null;
        } catch { }
        if(defaultTextFont != null) {
            try { UnityEngine.Object.Destroy(defaultTextFont); } catch { }
            defaultTextFont = null;
        }

        Tag.Input.Key.Shutdown();

        playbackStateRegistration?.Dispose();
        playbackStateRegistration = null;
        pausedStateRegistration?.Dispose();
        pausedStateRegistration = null;
        textFontRegistration?.Dispose();
        textFontRegistration = null;

        Spr.Dispose();
        Res.Dispose();

        MainCore.Tr.OnLoadStart -= LoadTr;
        MainCore.Tr.OnLanguageChanged -= OnLanguageChanged;

        ConfigFile.Save();
    }

    public static void ApplyUiVisibility() {
        UIVisibility.ApplyNative();
        UIVisibility.ApplyBuildTextNow();
        ApplyState(SafePatchController.Get<SP_HideBuildText>(), Config.HideBuildText);
        ApplyState(SafePatchController.Get<SP_HideEditorIcons>(), Config.HideEditorIcons);
        ApplyState(SafePatchController.Get<SP_HidePause>(), Config.HidePause);
        ApplyState(SafePatchController.Get<SP_HidePauseHint>(), Config.HidePause);
        ApplyState(SafePatchController.Get<SP_HidePauseButton>(), Config.HidePause);
        ApplyState(SafePatchController.Get<SP_HideAllGameUI>(), Config.HideAll);
        ApplyState(SafePatchController.Get<SP_HideAllEditorUI>(), Config.HideAll);
        if(!Config.HideAll) UIVisibility.ApplyAllGameUI(null);
        if(!Config.HideBuildText) UIVisibility.RestoreBuildText();
        if(!Config.HideEditorIcons) UIVisibility.RestoreEditorIcons();
        if(!Config.HidePause) {
            UIVisibility.RestorePauseButtons();
            UIVisibility.RestorePauseHints();
        }
    }

    private static void ApplyState<T>(T[] patches, bool enable) where T : SafeConditionalPatch {
        foreach(var patch in patches) {
            if(enable) patch.Apply();
            else patch.Remove();
        }
    }

    private static void RemoveModulePatches() {
        var types = new System.Type[] {
            typeof(Patch.SP_BlockAsyncInput),
            typeof(Patch.SP_BlockLegacyInput),
            typeof(Patch.SP_BlockInputMethod),
            typeof(Patch.SP_BlockDirectInput),
            typeof(Patch.SP_LinuxTMPKeyInput),
            typeof(Patch.SP_LinuxLegacyKeyInput),
            typeof(Patch.SP_ShowAutoJudgment),
            typeof(Patch.SP_AllowRightAlt),
            typeof(Patch.SP_ResetTagState),
            typeof(Patch.SP_RecordTiming),
            typeof(Patch.SP_SessionAttemptLoad),
            typeof(Patch.SP_SessionAttemptPlay),
            typeof(Patch.SP_FileAttemptLoad),
            typeof(Patch.SP_FileAttemptPlay),
            typeof(Patch.SP_HideBuildText),
            typeof(Patch.SP_HideEditorIcons),
            typeof(Patch.SP_HidePause),
            typeof(Patch.SP_HidePauseHint),
            typeof(Patch.SP_HidePauseButton),
            typeof(Patch.SP_HideAllGameUI),
            typeof(Patch.SP_HideAllEditorUI),
        };
        foreach(var type in types) {
            Overlayer.Patch.Safe.SafeConditionalPatch patch;
            while((patch = Overlayer.Patch.Safe.SafePatchController.Find(type)) != null) {
                try { Overlayer.Patch.Safe.SafePatchController.Remove(patch); } catch { break; }
            }
        }
    }

    private static byte[] ReadMoonPreset() {
        using var stream = Assembly.GetManifestResourceStream(
            "Overlayer.Module.ADOFAI.Resource.Embedded.Presets.Moon.o5cp")
            ?? throw new InvalidOperationException("Moon preset missing from module resources.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public override string Name => Info.Name;
    public override string Author => Info.Author;
    public override string Version => Info.Version;
}
