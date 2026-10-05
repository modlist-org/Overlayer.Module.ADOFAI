using Overlayer.Tag.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag.Gameplay;

public static class Spectrum {
    private static readonly Dictionary<int, (float[] buf, int frame)> buffers = new();
    private static readonly object lockObj = new();

    private static int ClampSize(int size) {
        int s = 64;
        while(s < size && s < 2048) {
            s *= 2;
        }
        return Math.Max(64, Math.Min(2048, s));
    }

    private static float[] Read(int size) {
        var conductor = GameAccess.Conductor.Get(null);
        var song = conductor == null ? null : GameAccess.Song.Get(conductor) as AudioSource;
        if(song == null) {
            return null;
        }
        size = ClampSize(size);
        int frame = UnityEngine.Time.frameCount;
        float[] buf;
        lock(lockObj) {
            if(buffers.TryGetValue(size, out var cached) && cached.frame == frame) {
                return cached.buf;
            }
            buf = cached.buf;
            if(buf == null) {
                buf = new float[size];
            }
            try {
                song.GetSpectrumData(buf, 0, FFTWindow.BlackmanHarris);
            } catch {
                return null;
            }
            buffers[size] = (buf, frame);
            return buf;
        }
    }

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Spectrum magnitude of a band (raw)")]
    public static double SpectrumBand(int band, int size = 256) {
        var buf = Read(size);
        if(buf == null || band < 0 || band >= buf.Length) {
            return 0;
        }
        return buf[band];
    }

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Average spectrum magnitude (raw)")]
    public static double SpectrumAvg(int size = 256) {
        var buf = Read(size);
        if(buf == null || buf.Length == 0) {
            return 0;
        }
        double sum = 0;
        for(int i = 0; i < buf.Length; i++) {
            sum += buf[i];
        }
        return sum / buf.Length;
    }

    [Tag(TagType = TagType.BlockOnNotPlaying, Desc = "Max spectrum magnitude (raw)")]
    public static double SpectrumMax(int size = 256) {
        var buf = Read(size);
        if(buf == null || buf.Length == 0) {
            return 0;
        }
        float max = 0;
        for(int i = 0; i < buf.Length; i++) {
            if(buf[i] > max) {
                max = buf[i];
            }
        }
        return max;
    }
}
