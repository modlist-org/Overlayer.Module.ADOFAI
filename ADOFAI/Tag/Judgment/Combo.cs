using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Overlayer.Module.ADOFAI.Tag.Judgment;

public static class Combo {
    private static IList Current {
        get {
            var controller = GameAccess.Controller.Get(null);
            var player = controller == null ? null : GameAccess.PlayerOne.Get(controller);
            var tracker = player == null ? null : GameAccess.MarginTracker.Get(player);
            var list = tracker == null ? null : GameAccess.HitMargins.Get(tracker);
            return list as IList ?? Array.Empty<object>();
        }
    }

    public static int ComboValue => GetRuns("*", IsPerfect).tail;
    [Tag(Name = "Combo", TagType = TagType.BlockOnNotPlaying, Desc = "Current combo")] public static int ComboTag => ComboValue;
    [Tag(TagType = TagType.ProcessFormat | TagType.BlockOnNotPlaying, Desc = "Max combo")] public static int MaxCombo => GetRuns("*", IsPerfect).max;
    [Tag(TagType = TagType.ProcessFormat | TagType.BlockOnNotPlaying, Desc = "Current combo of a judgment")] public static int MarginCombo(string margin) => GetRuns("c:" + margin, Matches(ParseCached(margin))).tail;
    [Tag(TagType = TagType.ProcessFormat | TagType.BlockOnNotPlaying, Desc = "Max combo of a judgment")] public static int MarginMaxCombo(string margin) => GetRuns("c:" + margin, Matches(ParseCached(margin))).max;
    [Tag(TagType = TagType.ProcessFormat | TagType.BlockOnNotPlaying, Desc = "Current combo of judgments (a|b|...)")] public static int MarginCombos(string margins) => GetRuns("m:" + margins, Matches(ParseManyCached(margins))).tail;
    [Tag(TagType = TagType.ProcessFormat | TagType.BlockOnNotPlaying, Desc = "Max combo of judgments (a|b|...)")] public static int MarginMaxCombos(string margins) => GetRuns("m:" + margins, Matches(ParseManyCached(margins))).max;

    internal static bool IsMidspin(object margin) {
        return margin != null && margin.ToString() == "Midspin";
    }

    internal static int Tail(IList values, Func<object, bool> matches) {
        int count = 0;
        for(int i = values.Count - 1; i >= 0; i--) {
            if(IsMidspin(values[i])) continue;
            if(!matches(values[i])) break;
            count++;
        }
        return count;
    }

    internal static int MaxRun(IList values, Func<object, bool> matches) {
        int best = 0, current = 0;
        foreach(object value in values) {
            if(IsMidspin(value)) continue;
            current = matches(value) ? current + 1 : 0;
            if(current > best) best = current;
        }
        return best;
    }

    private sealed class RunCache {
        public IList List;
        public int Count;
        public int Tail;
        public int Max;
    }

    private static readonly Dictionary<string, RunCache> runCaches = new();

    private static (int tail, int max) GetRuns(string key, Func<object, bool> matches) {
        var list = Current;
        if(runCaches.TryGetValue(key, out var cached)
            && ReferenceEquals(cached.List, list)
            && cached.Count == list.Count) {
            return (cached.Tail, cached.Max);
        }
        int best = 0, current = 0;
        foreach(object value in list) {
            if(IsMidspin(value)) continue;
            current = matches(value) ? current + 1 : 0;
            if(current > best) best = current;
        }
        if(runCaches.Count > 256) runCaches.Clear();
        runCaches[key] = new RunCache { List = list, Count = list.Count, Tail = current, Max = best };
        return (current, best);
    }

    private static Func<object, bool> Matches(HashSet<object> set) => set.Contains;

    private static readonly Dictionary<string, HashSet<object>> parseCache = new(StringComparer.Ordinal);

    private static HashSet<object> ParseCached(string margin) {
        margin ??= string.Empty;
        if(!parseCache.TryGetValue(margin, out var set)) {
            if(parseCache.Count > 128) parseCache.Clear();
            set = Parse(margin);
            parseCache[margin] = set;
        }
        return set;
    }

    private static HashSet<object> ParseManyCached(string margins) {
        margins ??= string.Empty;
        string key = "m|" + margins;
        if(!parseCache.TryGetValue(key, out var set)) {
            if(parseCache.Count > 128) parseCache.Clear();
            set = ParseMany(margins);
            parseCache[key] = set;
        }
        return set;
    }

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase) {
        ["XP"] = "XPerfect",
        ["PM"] = "PerfectMinus",
        ["PP"] = "PerfectPlus",
        ["EP"] = "EarlyPerfect",
        ["LP"] = "LatePerfect",
        ["TE"] = "TooEarly",
        ["TL"] = "TooLate",
        ["VE"] = "VeryEarly",
        ["VL"] = "VeryLate",
        ["A"] = "Auto",
        ["FM"] = "FailMiss",
        ["FO"] = "FailOverload",
        ["MP"] = "Multipress",
        ["OP"] = "OverPress",
    };

    private static HashSet<object> Parse(string margin) {
        var set = new HashSet<object>();
        if(Aliases.TryGetValue(margin?.Trim() ?? string.Empty, out var alias)) margin = alias;
        var value = GameAccess.ParseHitMargin(margin);
        if(value != null) set.Add(value);
        return set;
    }

    private static HashSet<object> ParseMany(string margins) {
        var set = new HashSet<object>();
        foreach(string value in (margins ?? string.Empty).Split('|')) {
            var parsed = GameAccess.ParseHitMargin(value.Trim());
            if(parsed != null) set.Add(parsed);
        }
        return set;
    }

    private static bool IsPerfect(object margin) {
        if(margin == null) return false;
        string name = margin.ToString();
        return name is "PerfectMinus" or "PerfectPlus" or "XPerfect" or "Auto";
    }
}
