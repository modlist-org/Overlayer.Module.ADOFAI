using Overlayer.Tag.Core;
using System;
using SkyHook;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Overlayer.Module.ADOFAI.Tag.Input;

public static class Key {
    private static readonly HashSet<KeyLabel> _held = new();
    private static readonly Dictionary<string, KeyLabel> _labels = BuildLabelCache();
    private static readonly object _lock = new();
    private static UnityAction<SkyHookEvent> _listener;
    private static bool _subscribed;

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
        lock(_lock) {
            if(ev.Type == EventType.KeyPressed) {
                if(_held.Add(ev.Label)) {
                    FeedKps();
                }
            } else if(ev.Type == EventType.KeyReleased) {
                // SkyHook is the source of truth here. Unity's input state can
                // diverge while multiple keys are held or game input is blocked.
                _held.Remove(ev.Label);
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
            _subscribed = false;
        }
    }

    [Tag(Desc = "[SkyHook] Returns true while the specified key is held down (OS-level hook, works even when Unity input is blocked)\nEx) {IsSkyHookKeyHeld:Space}, {IsSkyHookKeyHeld:LShift}")]
    public static bool IsSkyHookKeyHeld(string key) {
        EnsureSubscribed();
        if(string.IsNullOrWhiteSpace(key)) return false;
        if(!_labels.TryGetValue(key.Trim(), out KeyLabel label)) return false;
        lock(_lock) {
            return _held.Contains(label);
        }
    }
}
