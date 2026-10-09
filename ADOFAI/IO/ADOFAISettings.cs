using Newtonsoft.Json.Linq;
using Overlayer.IO.Interface;

namespace Overlayer.Module.ADOFAI.IO;

public sealed class ADOFAISettings : ISettingsFile {
    public bool ShowAutoplayJudgment = false;
    public bool LinuxTextInputFix = true;
    public bool HideTitle = false;
    public bool BlockInputWhenOpened = true;
    public bool AllowRightAlt = true;
    public bool FileFeature = false;
    public bool LazyPatches = true;
    public bool LazyAccess = true;
    public bool HideAll = false;
    public bool HideAutoplay = false;
    public bool HideBuildText = false;
    public bool HidePause = false;
    public bool HideEditorIcons = false;

    public JToken Serialize() {
        return new JObject {
            [nameof(ShowAutoplayJudgment)] = ShowAutoplayJudgment,
            [nameof(LinuxTextInputFix)] = LinuxTextInputFix,
            [nameof(HideTitle)] = HideTitle,
            [nameof(BlockInputWhenOpened)] = BlockInputWhenOpened,
            [nameof(AllowRightAlt)] = AllowRightAlt,
            [nameof(FileFeature)] = FileFeature,
            [nameof(LazyPatches)] = LazyPatches,
            [nameof(LazyAccess)] = LazyAccess,
            [nameof(HideAll)] = HideAll,
            [nameof(HideAutoplay)] = HideAutoplay,
            [nameof(HideBuildText)] = HideBuildText,
            [nameof(HidePause)] = HidePause,
            [nameof(HideEditorIcons)] = HideEditorIcons,
        };
    }

    public void Deserialize(JToken token) {
        ShowAutoplayJudgment = Read(token, nameof(ShowAutoplayJudgment), ShowAutoplayJudgment);
        LinuxTextInputFix = Read(token, nameof(LinuxTextInputFix), LinuxTextInputFix);
        HideTitle = Read(token, nameof(HideTitle), HideTitle);
        BlockInputWhenOpened = Read(token, nameof(BlockInputWhenOpened), BlockInputWhenOpened);
        AllowRightAlt = Read(token, nameof(AllowRightAlt), AllowRightAlt);
        FileFeature = Read(token, nameof(FileFeature), FileFeature);
        LazyPatches = Read(token, nameof(LazyPatches), LazyPatches);
        LazyAccess = Read(token, nameof(LazyAccess), LazyAccess);
        HideAll = Read(token, nameof(HideAll), HideAll);
        HideAutoplay = Read(token, nameof(HideAutoplay), HideAutoplay);
        HideBuildText = Read(token, nameof(HideBuildText), HideBuildText);
        HidePause = Read(token, nameof(HidePause), HidePause);
        HideEditorIcons = Read(token, nameof(HideEditorIcons), HideEditorIcons);
    }

    private static T? Read<T>(JToken token, string key, T fallback) {
        var value = token[key];

        if(value == null) {
            return fallback;
        }

        try {
            return value.Value<T>();
        } catch {
            return fallback;
        }
    }
}
