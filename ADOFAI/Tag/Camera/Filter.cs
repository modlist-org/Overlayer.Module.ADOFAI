using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag.Camera;

public static class Filter {
    [Tag(Desc = "TV Wide Screen Horizontal letterbox size (0-0.8, higher = more covered)")]
    public static float FilterLetterboxY => FilterSize("CameraFilterPack_TV_WideScreenHorizontal");

    [Tag(Desc = "TV Wide Screen Vertical letterbox size (0-0.8, higher = more covered)")]
    public static float FilterLetterboxX => FilterSize("CameraFilterPack_TV_WideScreenVertical");

    private static float FilterSize(string typeName) {
        try {
            var cam = GameAccess.Cam.Get(null);
            if(cam == null) return 0f;
            float foreground = ComponentSize(cam, "camobj", typeName);
            if(foreground > 0f) return foreground;
            return ComponentSize(cam, "BGcam", typeName);
        } catch {
            return 0f;
        }
    }

    private static float ComponentSize(object cam, string camField, string typeName) {
        try {
            if(!SafeAccess.TryRead(cam, camField, out object camObj) || camObj == null) return 0f;
            GameObject go = camObj switch {
                GameObject g => g,
                Component c => c.gameObject,
                _ => null,
            };
            if(go == null) return 0f;
            var type = SafeAccess.FindType(typeName);
            if(type == null) return 0f;
            var component = go.GetComponent(type) as Behaviour;
            if(component == null || !component.enabled) return 0f;
            var field = type.GetField("Size",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if(field == null) return 0f;
            float value = Convert.ToSingle(field.GetValue(component));
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value);
        } catch {
            return 0f;
        }
    }
}
