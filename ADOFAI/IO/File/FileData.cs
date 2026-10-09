using Newtonsoft.Json.Linq;
using Overlayer.IO.Interface;
using System.Collections.Generic;

namespace Overlayer.Module.ADOFAI.IO.File;

public sealed class FileData : ISettingsFile {
    public const string Format = "o5af";
    public const int FormatVersion = 1;

    public int Attempts { get; set; }
    public Dictionary<int, int> TileAttempts { get; set; } = [];
    // Runs started from tile 0 that reached each tile.
    public Dictionary<int, int> ReachCounts { get; set; } = [];
    public JObject Extra { get; set; } = [];

    public int GetTileAttempts(int tile)
        => TileAttempts.TryGetValue(tile, out var count) ? count : 0;

    public int GetReachCount(int tile)
        => ReachCounts.TryGetValue(tile, out var count) ? count : 0;

    public void Reach(int tile) => ReachCounts[tile] = GetReachCount(tile) + 1;

    public T Get<T>(string key, T fallback) {
        var token = Extra[key];
        if(token == null) return fallback;
        try {
            return token.Value<T>()!;
        } catch {
            return fallback;
        }
    }

    public void Set<T>(string key, T value) {
        Extra[key] = value != null ? JToken.FromObject(value) : JValue.CreateNull();
    }

    public void Increase(int tile) {
        Attempts++;
        TileAttempts[tile] = GetTileAttempts(tile) + 1;
    }

    public JToken Serialize() {
        return new JObject {
            [nameof(Attempts)] = Attempts,
            [nameof(TileAttempts)] = JToken.FromObject(TileAttempts),
            [nameof(ReachCounts)] = JToken.FromObject(ReachCounts),
            [nameof(Extra)] = Extra,
            ["format"] = Format,
            ["formatVersion"] = FormatVersion,
        };
    }

    public void Deserialize(JToken token) {
        Attempts = token[nameof(Attempts)]?.Value<int>() ?? 0;
        TileAttempts = token[nameof(TileAttempts)]?.ToObject<Dictionary<int, int>>() ?? [];
        ReachCounts = token[nameof(ReachCounts)]?.ToObject<Dictionary<int, int>>() ?? [];
        Extra = token[nameof(Extra)] as JObject ?? [];
    }
}
