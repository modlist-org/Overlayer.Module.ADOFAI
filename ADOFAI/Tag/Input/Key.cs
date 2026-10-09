using Overlayer.Async;
using Overlayer.Tag.Core;
using System;
using SkyHook;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Overlayer.Module.ADOFAI.Tag.Input;

public static class Key {
    private static readonly HashSet<KeyLabel> _held = new();
    // Raw codes of events SkyHook couldn't label. On Windows with a Korean
    // layout, right Alt/Ctrl arrive as VK_HANGUL/VK_HANJA, not VK_RMENU/VK_RCONTROL.
    private static readonly HashSet<ushort> _heldUnknown = new();
    private static readonly Dictionary<KeyLabel, ushort[]> _unknownAliases = new() {
        [KeyLabel.RAlt] = new ushort[] { 0x15, 0xA5 },     // VK_HANGUL, VK_RMENU
        [KeyLabel.RControl] = new ushort[] { 0x19, 0xA3 }, // VK_HANJA, VK_RCONTROL
    };
    private static readonly Dictionary<string, KeyLabel> _labels = BuildLabelCache();
    private static readonly object _lock = new();
    private static UnityAction<SkyHookEvent> _listener;
    private static bool _subscribed;
    private static readonly Dictionary<KeyLabel, long> _suspectRelease = new();
    private static readonly Dictionary<ushort, long> _suspectUnknownRelease = new();
    private const long ReleaseConfirmTicks = 100 * 10000L;
    private static bool _lastFocus = true;

    private static Dictionary<string, KeyLabel> BuildLabelCache() {
        var cache = new Dictionary<string, KeyLabel>(StringComparer.OrdinalIgnoreCase);
        foreach(KeyLabel label in Enum.GetValues(typeof(KeyLabel))) {
            cache.TryAdd(label.ToString(), label);
        }
        return cache;
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
        // NOTE: SkyHook invokes this on its native hook thread, NOT the Unity
        // main thread. Keep only lock-protected state here; the KPS feed
        // touches Unity-only APIs (Time) and the tracker's queue, so it is
        // marshalled to the main thread below.
        bool pressed = false;
        lock(_lock) {
            bool unknown = ev.Label == KeyLabel.Unknown;
            if(ev.Type == SkyHook.EventType.KeyPressed) {
                if(unknown) {
                    _suspectUnknownRelease.Remove(ev.Key);
                    pressed = _heldUnknown.Add(ev.Key);
                } else {
                    _suspectRelease.Remove(ev.Label);
                    pressed = _held.Add(ev.Label);
                }
            } else if(ev.Type == SkyHook.EventType.KeyReleased) {
                long now = DateTime.UtcNow.Ticks;
                if(unknown) {
                    if(_heldUnknown.Contains(ev.Key)) {
                        _suspectUnknownRelease[ev.Key] = now;
                    }
                } else if(_held.Contains(ev.Label)) {
                    _suspectRelease[ev.Label] = now;
                }
            }
        }
        if(pressed) {
            MainThread.Enqueue(FeedKps);
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
            _heldUnknown.Clear();
            _suspectRelease.Clear();
            _suspectUnknownRelease.Clear();
            _lastFocus = true;
            _subscribed = false;
        }
    }

    [Tag(Desc = "[SkyHook] Returns true while the specified key is held down (OS-level hook, works even when Unity input is blocked)\nEx) {IsSkyHookKeyHeld:Space}, {IsSkyHookKeyHeld:LShift}")]
    public static bool IsSkyHookKeyHeld(string key) {
        EnsureSubscribed();
        if(string.IsNullOrWhiteSpace(key)) return false;
        if(!_labels.TryGetValue(key.Trim(), out KeyLabel label)) return false;
        long now = DateTime.UtcNow.Ticks;
        lock(_lock) {
            SyncFocusLocked();
            SweepExpiredLocked(now);
            if(_held.Contains(label)) return true;
            if(_heldUnknown.Count == 0 || !_unknownAliases.TryGetValue(label, out ushort[] raws)) return false;
            foreach(ushort raw in raws) {
                if(_heldUnknown.Contains(raw)) return true;
            }
            return false;
        }
    }

    private static void SyncFocusLocked() {
        bool focused = true;
        try {
            focused = Application.isFocused;
        } catch {
            return;
        }
        if(focused && !_lastFocus) {
            _held.Clear();
            _heldUnknown.Clear();
            _suspectRelease.Clear();
            _suspectUnknownRelease.Clear();
        }
        _lastFocus = focused;
    }

    private static void SweepExpiredLocked(long now) {
        if(_suspectRelease.Count > 0) {
            var done = new List<KeyLabel>();
            foreach(var pair in _suspectRelease) {
                if(now - pair.Value >= ReleaseConfirmTicks) {
                    done.Add(pair.Key);
                }
            }
            foreach(var label in done) {
                _suspectRelease.Remove(label);
                _held.Remove(label);
            }
        }
        if(_suspectUnknownRelease.Count > 0) {
            var done = new List<ushort>();
            foreach(var pair in _suspectUnknownRelease) {
                if(now - pair.Value >= ReleaseConfirmTicks) {
                    done.Add(pair.Key);
                }
            }
            foreach(var raw in done) {
                _suspectUnknownRelease.Remove(raw);
                _heldUnknown.Remove(raw);
            }
        }
    }
}
