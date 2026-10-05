using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag.Judgment;

public static class Margin {
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "XPerfect margin (ms)")] public static double MarginXPms => TimeBoundsCached().XPerfect * 1000d;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Inner Perfect margin (ms, +-30deg)")] public static double MarginIPms => TimeBoundsCached().Pure * 1000d;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Outer Perfect margin (ms)")] public static double MarginOPms => TimeBoundsCached().Perfect * 1000d;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Very margin (ms)")] public static double MarginVms => TimeBoundsCached().Counted * 1000d;

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "XPerfect margin (deg)")] public static double MarginXPdeg => AngleBoundsCached().XPerfect;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Inner Perfect margin (deg)")] public static double MarginIPdeg => AngleBoundsCached().Pure;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Outer Perfect margin (deg)")] public static double MarginOPdeg => AngleBoundsCached().Perfect;
    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Very margin (deg)")] public static double MarginVdeg => AngleBoundsCached().Counted;

    [Tag(Desc = "Manual margin (ms): judgment, bpm, speed = 1, scale = 1")]
    public static double MarginCalcMs(string judgment, double bpm, double speed = 1, double scale = 1)
        => PickManual(judgment, ManualTimeBounds(bpm, speed, scale)) * 1000d;

    [Tag(Desc = "Manual margin (deg): judgment, bpm, speed = 1, scale = 1")]
    public static double MarginCalcDeg(string judgment, double bpm, double speed = 1, double scale = 1)
        => PickManual(judgment, ManualAngleBounds(bpm, speed, scale));

    private struct Bounds {
        public readonly double Counted;
        public readonly double Perfect;
        public readonly double Pure;
        public readonly double XPerfect;

        public Bounds(double counted, double perfect, double pure, double xperfect) {
            Counted = counted;
            Perfect = perfect;
            Pure = pure;
            XPerfect = xperfect;
        }
    }

    private static double PickManual(string judgment, Bounds bounds)
        => judgment?.Trim() switch {
            "XPerfect" => bounds.XPerfect,
            "PerfectMinus" or "PerfectPlus" => bounds.Pure,
            "EarlyPerfect" or "LatePerfect" => bounds.Perfect,
            "VeryEarly" or "VeryLate" => bounds.Counted,
            "TooEarly" or "TooLate" => double.PositiveInfinity,
            _ => double.NaN,
        };

    private static Bounds ManualTimeBounds(double bpm, double speed, double scale) {
        var mins = ManualMinimums(speed);
        double effBpm = bpm * speed;
        double pitch = Pitch;
        return new Bounds(
            Math.Max(mins.Counted, CallAngleToTime(60d * Deg2Rad, effBpm) / pitch * scale),
            Math.Max(mins.Perfect, CallAngleToTime(45d * Deg2Rad, effBpm) / pitch * scale),
            Math.Max(mins.Pure, CallAngleToTime(30d * Deg2Rad, effBpm) / pitch * scale),
            Math.Max(XPerfectMin, CallAngleToTime(12.5d * Deg2Rad, effBpm) / pitch * scale)
        );
    }

    private static Bounds ManualAngleBounds(double bpm, double speed, double scale) {
        var mins = ManualMinimums(speed);
        double effBpm = bpm * speed;
        double pitch = Pitch;
        return new Bounds(
            Math.Max(60d * scale, CallTimeToAngle(mins.Counted, effBpm, pitch) * Rad2Deg),
            Math.Max(45d * scale, CallTimeToAngle(mins.Perfect, effBpm, pitch) * Rad2Deg),
            Math.Max(30d * scale, CallTimeToAngle(mins.Pure, effBpm, pitch) * Rad2Deg),
            Math.Max(12.5d * scale, CallTimeToAngle(XPerfectMin, effBpm, pitch) * Rad2Deg)
        );
    }

    private static Bounds ManualMinimums(double speed) {
        double countedBase = GameAccess.DifficultyValue.Get(null)?.ToString() switch {
            "Lenient" => 0.091,
            "Normal" => 0.065,
            "Strict" => 0.04,
            _ => 0.065,
        };
        return new Bounds(
            Math.Max(countedBase / speed, HardCap),
            Math.Max(0.03 / speed, HardCap),
            Math.Max(0.02 / speed, HardCap),
            XPerfectMin
        );
    }

    private static int timeBoundsFrame = -1;
    private static Bounds timeBoundsCache;
    private static int angleBoundsFrame = -1;
    private static Bounds angleBoundsCache;

    private static Bounds TimeBoundsCached() {
        int frame = UnityEngine.Time.frameCount;
        if(frame != timeBoundsFrame) {
            timeBoundsFrame = frame;
            timeBoundsCache = TimeBounds();
        }
        return timeBoundsCache;
    }

    private static Bounds AngleBoundsCached() {
        int frame = UnityEngine.Time.frameCount;
        if(frame != angleBoundsFrame) {
            angleBoundsFrame = frame;
            angleBoundsCache = AngleBounds();
        }
        return angleBoundsCache;
    }

    private static Bounds TimeBounds() {
        if(!GameAccess.MinTimes.TryInvoke(null, out object mins, GameAccess.DifficultyValue.Get(null))) {
            return default;
        }
        double bpm = BpmTimesSpeed;
        double pitch = Pitch;
        double mult = MarginMult;
        if(bpm <= 0 || pitch <= 0) return default;
        return new Bounds(
            Math.Max(ReadDouble(mins, "Counted"), CallAngleToTime(60d * Deg2Rad, bpm) / pitch * mult),
            Math.Max(ReadDouble(mins, "Perfect"), CallAngleToTime(45d * Deg2Rad, bpm) / pitch * mult),
            Math.Max(ReadDouble(mins, "Pure"), CallAngleToTime(30d * Deg2Rad, bpm) / pitch * mult),
            Math.Max(XPerfectMin, CallAngleToTime(12.5d * Deg2Rad, bpm) / pitch * mult)
        );
    }

    private static Bounds AngleBounds() {
        double bpm = BpmTimesSpeed;
        double pitch = Pitch;
        if(bpm <= 0 || pitch <= 0) return default;
        if(!GameAccess.AngleBoundsFn.TryInvoke(null, out object result,
            GameAccess.DifficultyValue.Get(null), BpmTimesSpeed, Pitch, MarginMult)) {
            return default;
        }
        return new Bounds(
            ReadDouble(result, "Counted"),
            ReadDouble(result, "Perfect"),
            ReadDouble(result, "Pure"),
            ReadDouble(result, "XPerfect")
        );
    }

    private const double Deg2Rad = 0.01745329238474369;
    private const double Rad2Deg = 57.295780181884766;
    private const double XPerfectMin = 0.01666666753590107;
    private const double HardCap = 0.025;

    private static double BpmTimesSpeed {
        get {
            double bpm = GameAccess.ConductorBpm.Get(GameAccess.Conductor.Get(null));
            var controller = GameAccess.Controller.Get(null);
            var player = controller == null ? null : GameAccess.PlayerOne.Get(controller);
            var system = player == null ? null : GameAccess.PlanetarySystem.Get(player);
            double speed = system == null ? 0 : GameAccess.PlanetSpeed.Get(system);
            return bpm * speed;
        }
    }

    private static double Pitch {
        get {
            var conductor = GameAccess.Conductor.Get(null);
            var song = conductor == null ? null : GameAccess.Song.Get(conductor);
            return song == null ? 0 : GameAccess.SongPitch.Get(song);
        }
    }

    private static double MarginMult {
        get {
            var controller = GameAccess.Controller.Get(null);
            var floor = controller == null ? null : GameAccess.CurrFloor.Get(controller);
            var next = floor == null ? null : GameAccess.NextFloor.Get(floor);
            return next == null ? 1d : GameAccess.FloorMarginScale.Get(next, 1d);
        }
    }

    private static double CallAngleToTime(double angle, double bpm)
        => GameAccess.AngleToTimeFn.TryInvoke(null, out object result, angle, bpm) && result is double d ? d : 0;

    private static double CallTimeToAngle(double time, double bpm, double pitch)
        => GameAccess.TimeToAngleFn.TryInvoke(null, out object result, time, bpm, pitch) && result is double d ? d : 0;

    private static double ReadDouble(object target, string member)
        => target != null && SafeAccess.TryRead(target, member, out object value) && value is IConvertible
            ? Convert.ToDouble(value)
            : 0;
}
