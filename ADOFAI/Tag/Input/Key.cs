using Overlayer.Tag.Core;
using System;
using SkyHook;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Overlayer.Module.ADOFAI.Tag.Input;

public static class Key {
    private static readonly HashSet<KeyLabel> _held = new();
    private static readonly Dictionary<KeyLabel, long> _suspect = new();
    private static readonly Dictionary<string, KeyLabel> _labels = BuildLabelCache();
    private static readonly Dictionary<KeyLabel, UnityEngine.KeyCode> _codes = BuildKeyCodeMap();
    private static readonly object _lock = new();
    private static UnityAction<SkyHookEvent> _listener;
    private static bool _subscribed;
    private const long ConfirmGraceTicks = 150 * 10000L;

    private static Dictionary<string, KeyLabel> BuildLabelCache() {
        var cache = new Dictionary<string, KeyLabel>(StringComparer.OrdinalIgnoreCase);
        foreach(KeyLabel label in Enum.GetValues(typeof(KeyLabel))) {
            cache.TryAdd(label.ToString(), label);
        }
        return cache;
    }

    private static Dictionary<KeyLabel, UnityEngine.KeyCode> BuildKeyCodeMap() {
        var map = new Dictionary<KeyLabel, UnityEngine.KeyCode>();
        foreach(KeyLabel label in Enum.GetValues(typeof(KeyLabel))) {
            string name = label.ToString();
            if(Enum.TryParse(name, true, out UnityEngine.KeyCode direct)) {
                map[label] = direct;
                continue;
            }
            UnityEngine.KeyCode? alias = name switch {
                "ArrowUp" => UnityEngine.KeyCode.UpArrow,
                "ArrowDown" => UnityEngine.KeyCode.DownArrow,
                "ArrowLeft" => UnityEngine.KeyCode.LeftArrow,
                "ArrowRight" => UnityEngine.KeyCode.RightArrow,
                "Dot" => UnityEngine.KeyCode.Period,
                "Apostrophe" => UnityEngine.KeyCode.Quote,
                "Grave" => UnityEngine.KeyCode.BackQuote,
                "LeftBrace" => UnityEngine.KeyCode.LeftBracket,
                "RightBrace" => UnityEngine.KeyCode.RightBracket,
                "LShift" => UnityEngine.KeyCode.LeftShift,
                "RShift" => UnityEngine.KeyCode.RightShift,
                "LControl" => UnityEngine.KeyCode.LeftControl,
                "RControl" => UnityEngine.KeyCode.RightControl,
                "LAlt" => UnityEngine.KeyCode.LeftAlt,
                "RAlt" => UnityEngine.KeyCode.RightAlt,
                "Enter" => UnityEngine.KeyCode.Return,
                _ => null,
            };
            if(alias.HasValue) {
                map[label] = alias.Value;
            }
        }
        return map;
    }

    private static void EnsureSubscribed() {
        if(_subscribed) return;
        lock(_lock) {
            if(_subscribed) return;
            var manager = SkyHookManager.Instance;
            if(manager == null) return;
            _listener = OnKeyEvent;
            try {
                SkyHookManager.KeyUpdated.AddListener(_listener);
                _subscribed = true;
            } catch {
                try { SkyHookManager.KeyUpdated.RemoveListener(_listener); } catch { }
                _listener = null;
            }
        }
    }

    private static void OnKeyEvent(SkyHookEvent ev) {
        lock(_lock) {
            if(ev.Type == EventType.KeyPressed) {
                _suspect.Remove(ev.Label);
                if(_held.Add(ev.Label)) {
                    FeedKps();
                }
            } else if(ev.Type == EventType.KeyReleased) {
                if(_held.Contains(ev.Label)) {
                    _suspect[ev.Label] = DateTime.UtcNow.Ticks;
                }
            }
        }
    }

    private static void FeedKps() {
        try {
            var tracker = Overlayer.TagImpl.KpsTracker.Instance;
            if(tracker == null) return;
            tracker.SuppressUnityKeys = true;
            tracker.ReportKeyPress();
        } catch { }
    }

    internal static void EnsureFeed() => EnsureSubscribed();

    internal static void Shutdown() {
        lock(_lock) {
            try {
                var tracker = Overlayer.TagImpl.KpsTracker.Instance;
                if(tracker != null) tracker.SuppressUnityKeys = false;
            } catch { }
            if(!_subscribed) return;
            try {
                SkyHookManager.KeyUpdated.RemoveListener(_listener);
            } catch { }
            _listener = null;
            _held.Clear();
            _suspect.Clear();
            _subscribed = false;
        }
    }

    [Tag(Desc = "[SkyHook] Returns true while the specified key is held down (OS-level hook, works even when Unity input is blocked)\nEx) {IsSkyHookKeyHeld:Space}, {IsSkyHookKeyHeld:LShift}")]
    public static bool IsSkyHookKeyHeld(string key) {
        EnsureSubscribed();
        if(string.IsNullOrWhiteSpace(key)) return false;
        if(!_labels.TryGetValue(key.Trim(), out KeyLabel label)) return false;
        lock(_lock) {
            if(!_held.Contains(label)) {
                return false;
            }
            if(_suspect.TryGetValue(label, out long releasedAt)) {
                bool stillDown = false;
                if(_codes.TryGetValue(label, out var code)) {
                    try {
                        stillDown = UnityEngine.Input.GetKey(code);
                    } catch { }
                }
                long ageMs = (DateTime.UtcNow.Ticks - releasedAt) / 10000L;
                if(!stillDown || ageMs * 10000L > ConfirmGraceTicks) {
                    _held.Remove(label);
                    _suspect.Remove(label);
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
