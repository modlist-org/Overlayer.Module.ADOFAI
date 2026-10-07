using Overlayer.Tag.Core;
using System;
using System.Collections;

namespace Overlayer.Module.ADOFAI.Tag.Gameplay;

public static class Time {
    private static object Song() {
        var conductor = GameAccess.Conductor.Get(null);
        return conductor == null ? null : GameAccess.Song.Get(conductor);
    }

    private static IList Floors() {
        var maker = GameAccess.LevelMaker.Get(null);
        return maker == null ? null : GameAccess.FloorList.Get(maker) as IList;
    }

    [Tag(Desc = "Song time (days)")] public static int SongDay => SongTime.Days;
    [Tag(Desc = "Song time (hours)")] public static int SongHour => SongTime.Hours;
    [Tag(Desc = "Song time (minutes)")] public static int SongMinute => SongTime.Minutes;
    [Tag(Desc = "Song time (seconds)")] public static int SongSecond => SongTime.Seconds;
    [Tag(Desc = "Song time (milliseconds)")] public static int SongMilliSecond => SongTime.Milliseconds;
    [Tag(Desc = "Song length (days)")] public static int TotalDay => SongLength.Days;
    [Tag(Desc = "Song length (hours)")] public static int TotalHour => SongLength.Hours;
    [Tag(Desc = "Song length (minutes)")] public static int TotalMinute => SongLength.Minutes;
    [Tag(Desc = "Song length (seconds)")] public static int TotalSecond => SongLength.Seconds;
    [Tag(Desc = "Song length (milliseconds)")] public static int TotalMilliSecond => SongLength.Milliseconds;

    [Tag(Desc = "Map length (seconds)")] public static double MapLength => MapSpan.TotalSeconds;
    [Tag(Desc = "Map length (hours)")] public static int MapTotalHour => MapSpan.Hours;
    [Tag(Desc = "Map length (minutes)")] public static int MapTotalMinute => MapSpan.Minutes;
    [Tag(Desc = "Map length (seconds)")] public static int MapTotalSecond => MapSpan.Seconds;
    [Tag(Desc = "Map length (milliseconds)")] public static int MapTotalMilliSecond => MapSpan.Milliseconds;
    [Tag(Desc = "Map time (hours)")] public static int MapHour => MapElapsed.Hours;
    [Tag(Desc = "Map time (minutes)")] public static int MapMinute => MapElapsed.Minutes;
    [Tag(Desc = "Map time (seconds)")] public static int MapSecond => MapElapsed.Seconds;
    [Tag(Desc = "Map time (milliseconds)")] public static int MapMilliSecond => MapElapsed.Milliseconds;

    private static TimeSpan SongTime {
        get {
            var song = Song();
            double seconds = song == null ? 0 : GameAccess.SongTime.Get(song);
            return TimeSpan.FromSeconds(Math.Max(0, seconds));
        }
    }

    private static TimeSpan SongLength {
        get {
            var song = Song();
            var clip = song == null ? null : GameAccess.SongClip.Get(song);
            float length = clip == null ? 0 : GameAccess.ClipLength.Get(clip);
            return TimeSpan.FromSeconds(Math.Max(0, length));
        }
    }

    private static TimeSpan MapSpan {
        get {
            var floors = Floors();
            if(floors == null || floors.Count == 0) return TimeSpan.Zero;
            double span = GameAccess.EntryTime.Get(floors[floors.Count - 1]) - GameAccess.EntryTime.Get(floors[0]);
            return TimeSpan.FromSeconds(Math.Max(0, span));
        }
    }

    private static TimeSpan MapElapsed {
        get {
            var floors = Floors();
            var conductor = GameAccess.Conductor.Get(null);
            if(floors == null || floors.Count == 0 || conductor == null) return TimeSpan.Zero;
            double elapsed = GameAccess.SongPosition.Get(conductor) - GameAccess.EntryTime.Get(floors[0]);
            return TimeSpan.FromSeconds(Math.Max(0, elapsed));
        }
    }
}
