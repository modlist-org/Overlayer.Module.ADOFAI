using Overlayer.Utility.Access;

namespace Overlayer.Module.ADOFAI;

public static class GameAccess {

    // Root
    public static readonly SafeMember<object> Controller = new(new("scrController", "instance"));
    public static readonly SafeMember<object> Conductor = new(new("scrConductor", "instance"));
    public static readonly SafeMember<object> LevelMaker = new(new("scrLevelMaker", "instance"));
    public static readonly SafeMember<object> ScnGame = new(new("scnGame", "instance"));
    public static readonly SafeMember<object> ScnEditor = new(new("scnEditor", "instance"));
    public static readonly SafeMember<object> UIController = new(new("scrUIController", "instance"));
    public static readonly SafeMember<bool> EditorPausedInPlayMode = new(new("scnEditor", "pausedInPlayMode"));
    public static readonly SafeMember<object> Cam = new(new("scrCamera", "instance"));
    public static readonly SafeMember<object> VfxPlus = new(new("scrVfxPlus", "instance"));
    public static readonly SafeMember<object> Vfx = new(new("scrVfx", "instance"));


    // Controller
    public static readonly SafeMember<object> PlayerOne = new(new("scrController", "playerOne"));
    public static readonly SafeMember<object> ControllerStateMachine = new(new("scrController", "stateMachine"));
    public static readonly SafeMember<object> CurrFloor = new(new("scrController", "currFloor"));
    public static readonly SafeMember<int> CurrentSeqID = new(new("scrController", "currentSeqID"));
    public static readonly SafeMember<float> PercentComplete = new(new("scrController", "percentComplete"));
    public static readonly SafeMember<bool> NoFail = new(new("scrController", "noFail"));
    public static readonly SafeMember<bool> Paused = new(new("scrController", "paused"));
    public static readonly SafeMember<object> TxtLevelName = new(new("scrController", "txtLevelName"));
    public static readonly SafeMember<int> CheckpointsUsedCount = new(new("scrController", "checkpointsUsed"));
    public static readonly SafeMember<int> CurrentWorld = new(new("scrController", "currentWorld"));


    // Player / tracker
    public static readonly SafeMember<object> MarginTracker = new(new("scrPlayer", "marginTracker"));
    public static readonly SafeMember<object> PlanetarySystem = new(new("scrPlayer", "planetarySystem"));
    public static readonly SafeMember<float> PercentAcc = new(new("scrMarginTracker", "percentAcc"));
    public static readonly SafeMember<float> PercentXAcc = new(new("scrMarginTracker", "percentXAcc"));
    public static readonly SafeMember<int> XScoreValue = new(new("scrMarginTracker", "xScore"));
    public static readonly SafeMember<int> MaxXScoreValue = new(new("scrMarginTracker", "maxXScore"));
    public static readonly SafeMember<int> LastXScoreValue = new(new("scrMarginTracker", "lastXScore"));
    public static readonly SafeMember<object> HitMargins = new(new("scrMarginTracker", "hitMargins"));
    public static readonly SafeMember<double> PlanetSpeed = new(new("PlanetarySystem", "speed"));


    // Conductor / song
    public static readonly SafeMember<double> ConductorBpm = new(new("scrConductor", "bpm"));
    public static readonly SafeMember<double> SongPosition = new(new("scrConductor", "songposition_minusi"));
    public static readonly SafeMember<object> Song = new(new("scrConductor", "song"));
    public static readonly SafeMember<bool> IsGameWorldFlag = new(new("scrConductor", "isGameWorld"));
    public static readonly SafeMember<double> SongTime = new(new("UnityEngine.AudioSource", "time"));
    public static readonly SafeMember<float> SongPitch = new(new("UnityEngine.AudioSource", "pitch"));
    public static readonly SafeMember<object> SongClip = new(new("UnityEngine.AudioSource", "clip"));
    public static readonly SafeMember<float> ClipLength = new(new("UnityEngine.AudioClip", "length"));


    // Floor
    public static readonly SafeMember<object> FloorList = new(new("scrLevelMaker", "listFloors"));
    public static readonly SafeMember<double> EntryTime = new(new("scrFloor", "entryTime"));
    public static readonly SafeMember<double> FloorSpeed = new(new("scrFloor", "speed"));
    public static readonly SafeMember<int> SeqID = new(new("scrFloor", "seqID"));
    public static readonly SafeMember<double> FloorMarginScale = new(new("scrFloor", "marginScale"));
    public static readonly SafeMember<bool> FloorAuto = new(new("scrFloor", "auto"));
    public static readonly SafeMember<object> NextFloor = new(new("scrFloor", "nextfloor"));
    public static readonly SafeMember<bool> FloorIsCCW = new(new("scrFloor", "isCCW"));
    public static readonly SafeMember<object> PlanetConductor = new(new("scrPlanet", "conductor"));
    public static readonly SafeMember<double> PlanetAngle = new(new("scrPlanet", "angle"));
    public static readonly SafeMember<double> PlanetTargetExitAngle = new(new("scrPlanet", "targetExitAngle"));


    // Level
    public static readonly SafeMember<object> GameLevelData = new(new("scnGame", "levelData"));
    public static readonly SafeMember<object> EditorLevelData = new(new("scnEditor", "levelData"));
    public static readonly SafeMember<string> LevelPath = new(new("scnGame", "levelPath"));
    public static readonly SafeMember<string> LevelSong = new(new("LevelData", "song"));
    public static readonly SafeMember<string> LevelArtist = new(new("LevelData", "artist"));
    public static readonly SafeMember<string> LevelAuthor = new(new("LevelData", "author"));
    public static readonly SafeMember<double> LevelPitch = new(new("LevelData", "pitch"));
    public static readonly SafeMember<double> LevelBpm = new(new("LevelData", "bpm"));
    public static readonly SafeMember<string> LevelHash = new(new("LevelData", "Hash"));
    public static readonly SafeMember<object> DefaultTextColorValue = new(new("LevelData", "defaultTextColor"));
    public static readonly SafeMember<object> DefaultTextShadowValue = new(new("LevelData", "defaultTextShadowColor"));


    // Camera / VFX
    public static readonly SafeMember<object> CamObj = new(new("scrCamera", "camobj"));
    public static readonly SafeMember<float> CamSizeNormal = new(new("scrCamera", "camsizenormal"));
    public static readonly SafeMember<object> CamTransform = new(new("scrCamera", "transform"));
    public static readonly SafeMember<float> OrthoSize = new(new("UnityEngine.Camera", "orthographicSize"));
    public static readonly SafeMember<object> CamPosition = new(new("UnityEngine.Transform", "position"));
    public static readonly SafeMember<string> UIText = new(new("UnityEngine.UI.Text", "text"));
    public static readonly SafeMember<float> CamAngle = new(new("scrVfxPlus", "camAngle"));
    public static readonly SafeMember<object> ColourScheme = new(new("scrVfx", "currentColourScheme"));
    public static readonly SafeMember<object> ColourText = new(new("ColourScheme", "colourText"));
    public static readonly SafeMember<object> ColourTextShadow = new(new("ColourScheme", "colourTextShadow"));


    // GCS
    public static readonly SafeMember<object> DifficultyValue = new(new("GCS", "difficulty"));
    public static readonly SafeMember<int> CheckpointNum = new(new("GCS", "checkpointNum"));
    public static readonly SafeMember<float> CurrentSpeedTrial = new(new("GCS", "currentSpeedTrial"));
    public static readonly SafeMember<bool> PracticeMode = new(new("GCS", "practiceMode"));
    public static readonly SafeMember<bool> UseNoFail = new(new("GCS", "useNoFail"));
    public static readonly SafeMember<bool> SpeedTrialMode = new(new("GCS", "speedTrialMode"));
    public static readonly SafeMember<bool> DontShowTitles = new(new("GCS", "d_dontShowTitles"));
    public static readonly SafeMember<bool> NoHud = new(new("RDC", "noHud"));
    public static readonly SafeMember<bool> NoAutoHud = new(new("RDC", "noAutoHud"));
    public static readonly SafeMember<object> EditorControlsTip = new(new("scnEditor", "controlsTip"));


    // Misc
    public static readonly SafeMember<string> SceneName = new(new("ADOBase", "sceneName"));
    public static readonly SafeMember<bool> IsOfficialLevelFlag = new(new("ADOBase", "isOfficialLevel"));
    public static readonly SafeMember<bool> IsLevelEditorFlag = new(new("ADOBase", "isLevelEditor"));
    public static readonly SafeMember<object> RDData = new(new("RDConstants", "data"));
    public static readonly SafeMember<bool> RDAuto = new(new("RDConstants", "auto"));
    public static readonly SafeMember<bool> RDPractice = new(new("RDConstants", "practice"));
    public static readonly SafeMember<bool> RDOldAuto = new(new("RDConstants", "useOldAuto"));


    public static readonly SafeMember<object> ClearKeysFn = new(new("AsyncInputManager", "ClearKeys"));
    public static readonly SafeMember<object> FrameKeyMask = new(new("AsyncInputManager", "frameDependentKeyMask"));
    public static readonly SafeMember<object> FrameKeyDownMask = new(new("AsyncInputManager", "frameDependentKeyDownMask"));
    public static readonly SafeMember<object> FrameKeyUpMask = new(new("AsyncInputManager", "frameDependentKeyUpMask"));


    public static readonly SafeMember<object> RDStringGet = new(new("RDString", "Get"));
    public static readonly SafeMember<object> FontDataForLanguage = new(new("RDString", "GetFontDataForLanguage"));
    public static readonly SafeMember<object> RemoveRichTagsFn = new(new("RDUtils", "RemoveRichTags"));
    public static readonly SafeMember<object> WorldAttemptsFn = new(new("Persistence", "GetWorldAttempts") { MethodArgs = [typeof(int)] });
    public static readonly SafeMember<object> CustomAttemptsFn = new(new("Persistence", "GetCustomWorldAttempts"));


    public static readonly SafeMember<object> MinTimes = new(new("scrMisc", "GetMinimumTimes"));
    public static readonly SafeMember<object> AngleToTimeFn = new(new("scrMisc", "AngleToTime"));
    public static readonly SafeMember<object> TimeToAngleFn = new(new("scrMisc", "TimeToAngleInRad"));
    public static readonly SafeMember<object> AngleBoundsFn = new(new("scrMisc", "GetAdjustedAngleBoundaryInDeg"));

    private static System.Type _hitMarginType;
    public static System.Type HitMarginType
        => _hitMarginType ??= SafeAccess.FindType("HitMargin");

    public static object ParseHitMargin(string name) {
        var type = HitMarginType;
        if(type == null || string.IsNullOrEmpty(name)) {
            return null;
        }
        try {
            return System.Enum.Parse(type, name, true);
        } catch {
            return null;
        }
    }

    public static void Init(System.Reflection.Assembly gameAssembly) {
        SafeAccess.Init(gameAssembly);
    }
}
