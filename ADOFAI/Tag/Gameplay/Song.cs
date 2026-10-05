using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag.Gameplay;

public static class Song {
    private static object Controller => GameAccess.Controller.Get(null);

    private static object Level() {
        var game = GameAccess.ScnGame.Get(null);
        if(game != null) {
            var level = GameAccess.GameLevelData.Get(game);
            if(level != null) return level;
        }
        var editor = GameAccess.ScnEditor.Get(null);
        return editor == null ? null : GameAccess.EditorLevelData.Get(editor);
    }

    [Tag(Desc = "Song title")] public static string Title => Strip(Level() == null ? null : GameAccess.LevelSong.Get(Level()));
    [Tag(Desc = "Song artist")] public static string Artist => Strip(Level() == null ? null : GameAccess.LevelArtist.Get(Level()));
    [Tag(Desc = "Song author")] public static string Author => Strip(Level() == null ? null : GameAccess.LevelAuthor.Get(Level()));
    [Tag(Desc = "Song title (raw)")] public static string TitleRaw => Level() == null ? string.Empty : GameAccess.LevelSong.Get(Level()) ?? string.Empty;
    [Tag(Desc = "Song artist (raw)")] public static string ArtistRaw => Level() == null ? string.Empty : GameAccess.LevelArtist.Get(Level()) ?? string.Empty;
    [Tag(Desc = "Song author (raw)")] public static string AuthorRaw => Level() == null ? string.Empty : GameAccess.LevelAuthor.Get(Level()) ?? string.Empty;

    [Tag(Desc = "Level name text")] public static string LevelNameText {
        get {
            var controller = Controller;
            var label = controller == null ? null : GameAccess.TxtLevelName.Get(controller);
            return Strip(label == null ? null : GameAccess.UIText.Get(label));
        }
    }
    [Tag(Desc = "Level name text (raw)")] public static string LevelNameTextRaw {
        get {
            var controller = Controller;
            var label = controller == null ? null : GameAccess.TxtLevelName.Get(controller);
            return label == null ? string.Empty : GameAccess.UIText.Get(label) ?? string.Empty;
        }
    }
    [Tag(Desc = "Default text color")] public static string DefaultTextColor(bool noAlpha = false)
        => ToHex(ReadColor(Level() == null ? null : GameAccess.DefaultTextColorValue.Get(Level()), UnityEngine.Color.white), noAlpha);
    [Tag(Desc = "Default text shadow color")] public static string DefaultTextShadowColor(bool noAlpha = false)
        => ToHex(ReadColor(Level() == null ? null : GameAccess.DefaultTextShadowValue.Get(Level()), UnityEngine.Color.black), noAlpha);
    [Tag(Desc = "Level name text color")] public static string LevelNameTextColor(bool noAlpha = false)
        => ToHex(ReadSchemeColor("colourText", UnityEngine.Color.white), noAlpha);
    [Tag(Desc = "Level name text shadow color")] public static string LevelNameTextShadowColor(bool noAlpha = false)
        => ToHex(ReadSchemeColor("colourTextShadow", UnityEngine.Color.black), noAlpha);

    [Tag(Desc = "Editor pitch")] public static double EditorPitch => (Level() == null ? 100 : GameAccess.LevelPitch.Get(Level())) / 100d;
    [Tag(Desc = "Tile BPM (with pitch)")] public static double TileBpm => BaseBpm * CurrentSpeed * SongPitch;
    [Tag(Desc = "Current BPM (with pitch)")] public static double CurBpm => RealBpm * SongPitch;
    [Tag(Desc = "Keys per second (from BPM)")] public static double BpmKps => CurBpm / 60d;
    [Tag(Desc = "Tile BPM (pitch excluded)")] public static double TileBpmWithoutPitch => BaseBpm * CurrentSpeed;
    [Tag(Desc = "Current BPM (pitch excluded)")] public static double CurBpmWithoutPitch => RealBpm;
    [Tag(Desc = "Keys per second (from BPM, pitch excluded)")] public static double BpmKpsWithoutPitch => CurBpmWithoutPitch / 60d;

    private static double BaseBpm {
        get {
            var level = Level();
            if(level != null) return GameAccess.LevelBpm.Get(level);
            var conductor = GameAccess.Conductor.Get(null);
            return conductor == null ? 0 : GameAccess.ConductorBpm.Get(conductor);
        }
    }

    private static double CurrentSpeed {
        get {
            var controller = Controller;
            var floor = controller == null ? null : GameAccess.CurrFloor.Get(controller);
            return floor == null ? 0 : GameAccess.FloorSpeed.Get(floor);
        }
    }

    private static double SongPitch {
        get {
            var conductor = GameAccess.Conductor.Get(null);
            var song = conductor == null ? null : GameAccess.Song.Get(conductor);
            return song == null ? 1 : GameAccess.SongPitch.Get(song, 1f);
        }
    }

    private static double RealBpm {
        get {
            var controller = Controller;
            var floor = controller == null ? null : GameAccess.CurrFloor.Get(controller);
            var next = floor == null ? null : GameAccess.NextFloor.Get(floor);
            if(next == null) return BaseBpm * CurrentSpeed;
            double duration = GameAccess.EntryTime.Get(next) - GameAccess.EntryTime.Get(floor);
            return duration <= 0 ? BaseBpm * CurrentSpeed : 60d / duration;
        }
    }

    private static UnityEngine.Color ReadSchemeColor(string member, UnityEngine.Color fallback) {
        var vfx = GameAccess.Vfx.Get(null);
        var scheme = vfx == null ? null : GameAccess.ColourScheme.Get(vfx);
        object value = scheme == null ? null
            : member == "colourText" ? GameAccess.ColourText.Get(scheme) : GameAccess.ColourTextShadow.Get(scheme);
        return ReadColor(value, fallback);
    }

    private static UnityEngine.Color ReadColor(object value, UnityEngine.Color fallback) {
        if(value == null) return fallback;
        if(value is UnityEngine.Color direct) return direct;
        return SafeAccess.TryRead(value, "r", out object r) && r is float fr &&
               SafeAccess.TryRead(value, "g", out object g) && g is float fg &&
               SafeAccess.TryRead(value, "b", out object b) && b is float fb
            ? new UnityEngine.Color(fr, fg, fb, SafeAccess.TryRead(value, "a", out object a) && a is float fa ? fa : 1f)
            : fallback;
    }

    private static string Strip(string value) => string.IsNullOrEmpty(value) ? string.Empty : RemoveTags(value);

    private static string RemoveTags(string value) {
        if(GameAccess.RemoveRichTagsFn.TryInvoke(null, out object result, value) && result is string text) {
            return text;
        }
        return value;
    }

    private static string ToHex(UnityEngine.Color color, bool noAlpha)
        => noAlpha ? ColorUtility.ToHtmlStringRGB(color) : ColorUtility.ToHtmlStringRGBA(color);
}
