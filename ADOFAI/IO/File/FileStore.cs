using System;
using System.IO;

namespace Overlayer.Module.ADOFAI.IO.File;

public sealed class FileStore {
    private const string FileName = "Overlayer_File.json";
    private const string LegacyFileName = "Overlayer_Attempts.json";

    public FileData Data { get; private set; } = new();
    public bool HasRecord { get; private set; }

    public void Load() {
        Data = new();
        HasRecord = false;
        string? path = GetPath();
        if(path != null && System.IO.File.Exists(path)) {
            try {
                Data.Deserialize(Newtonsoft.Json.Linq.JToken.Parse(System.IO.File.ReadAllText(path)));
                HasRecord = true;
            } catch {
                Data = new();
                HasRecord = false;
            }
        }
        if(!HasRecord) {
            string? legacy = GetLegacyPath();
            if(legacy != null && System.IO.File.Exists(legacy)) {
                try {
                    Data.Deserialize(Newtonsoft.Json.Linq.JToken.Parse(System.IO.File.ReadAllText(legacy)));
                    HasRecord = true;
                    Save();
                    System.IO.File.Delete(legacy);
                } catch {
                    Data = new();
                    HasRecord = false;
                }
            }
        }
        if(!HasRecord && HasGameRecord()) {
            HasRecord = true;
        }
    }

    public void Save() {
        string? path = GetPath();
        if(path == null) return;
        try {
            System.IO.File.WriteAllText(path, Data.Serialize().ToString(Newtonsoft.Json.Formatting.None));
        } catch(Exception e) {
            try { Core.Logger.Err($"[FileStore] Save failed: {e.Message}"); } catch { }
        }
    }

    internal static bool HasGameRecord() {
        if(GameAccess.IsOfficialLevelFlag.Get(null)) {
            try {
                var controller = GameAccess.Controller.Get(null);
                int world = controller == null ? -1 : GameAccess.CurrentWorld.Get(controller, -1);
                if(world < 0) return false;
                return GameAccess.WorldAttemptsFn.TryInvoke(null, out object result, world)
                    && result is int count && count > 0;
            } catch {
                return false;
            }
        }
        var scnGame = GameAccess.ScnGame.Get(null);
        var level = scnGame == null ? null : GameAccess.GameLevelData.Get(scnGame);
        if(level == null) return false;
        string hash = GameAccess.LevelHash.Get(level);
        if(string.IsNullOrEmpty(hash)) return false;
        return GameAccess.CustomAttemptsFn.TryInvoke(null, out object attempts, hash)
            && attempts is int n && n > 0;
    }

    private static string? GetPath() {
        var scnGame = GameAccess.ScnGame.Get(null);
        string level = scnGame == null ? null : GameAccess.LevelPath.Get(scnGame);
        if(string.IsNullOrEmpty(level)) return null;
        string? dir = System.IO.Path.GetDirectoryName(level);
        return string.IsNullOrEmpty(dir) ? null : System.IO.Path.Combine(dir, FileName);
    }

    private static string? GetLegacyPath() {
        var scnGame = GameAccess.ScnGame.Get(null);
        string level = scnGame == null ? null : GameAccess.LevelPath.Get(scnGame);
        if(string.IsNullOrEmpty(level)) return null;
        string? dir = System.IO.Path.GetDirectoryName(level);
        return string.IsNullOrEmpty(dir) ? null : System.IO.Path.Combine(dir, LegacyFileName);
    }
}

public static class FileStoreState {
    public static readonly FileStore Current = new();
}

public static class SessionAttemptState {
    public static int Count { get; private set; }

    public static void Reset() => Count = 0;
    public static void Increase() => Count++;
}
