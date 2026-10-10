using Overlayer.Tag.Core;
using Overlayer.Utility.Access;
using System;

namespace Overlayer.Module.ADOFAI.Tag.Camera;

public static class Camera {
    [Tag(Desc = "Camera X position")] public static float CamX => ReadAxis(CamPosition(), "x");
    [Tag(Desc = "Camera Y position")] public static float CamY => ReadAxis(CamPosition(), "y");

    [Tag(Desc = "Camera rotation in degrees (0-359)")]
    public static float CamRot => NormalizedRotation;

    [Tag(Desc = "Camera rotation in radians (0-Tau)")]
    public static float CamRotRad => NormalizedRotation * UnityEngine.Mathf.PI / 180f;

    [Tag(Desc = "Camera zoom (1 = normal)")]
    public static float CamZoom {
        get {
            try {
                var cam = GameAccess.Cam.Get(null);
                var camobj = cam == null ? null : GameAccess.CamObj.Get(cam);
                if(camobj == null) {
                    return 0f;
                }
                float size = GameAccess.OrthoSize.Get(camobj, -1f);
                float normal = GameAccess.CamSizeNormal.Get(cam, -1f);
                if(size <= 0f || normal <= 0f || float.IsNaN(size) || float.IsInfinity(size) ||
                    float.IsNaN(normal) || float.IsInfinity(normal)) {
                    return 0f;
                }
                return normal / size;
            } catch {
                return 0f;
            }
        }
    }

    private static object CamPosition() {
        try {
            var cam = GameAccess.Cam.Get(null);
            var t = cam == null ? null : GameAccess.CamTransform.Get(cam);
            return t == null ? null : GameAccess.CamPosition.Get(t);
        } catch {
            return null;
        }
    }

    private static float ReadAxis(object position, string axis) {
        if(position == null) {
            return 0f;
        }
        return SafeAccess.TryRead(position, axis, out object value) && value is float f ? f : 0f;
    }

    private static float NormalizedRotation {
        get {
            try {
                var vfx = GameAccess.VfxPlus.Get(null);
                if(vfx == null) {
                    return 0f;
                }
                float z = GameAccess.CamAngle.Get(vfx);
                if(float.IsNaN(z) || float.IsInfinity(z)) {
                    return 0f;
                }
                z %= 360f;
                if(z < 0f) {
                    z += 360f;
                }
                return z;
            } catch {
                return 0f;
            }
        }
    }
}
