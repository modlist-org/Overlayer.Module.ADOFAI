using Overlayer.Module.ADOFAI;
using Overlayer.Utility.Access;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Overlayer.Module.ADOFAI.Tests;

public sealed class GameAccessTests {
    private readonly ITestOutputHelper _output;

    public GameAccessTests(ITestOutputHelper output) {
        _output = output;
    }

    private static string _gameDir;
    private static Assembly _gameAssembly;
    private static readonly object _gate = new();
    private static bool _ready;
    private static string _skipReason;

    private static void EnsureGame() {
        lock(_gate) {
            if(_ready) return;
            _ready = true;
            string managed = FindManagedDir();
            if(managed == null) {
                _skipReason = "game DLL not found (set GAME_MANAGED_DIR or check ADOFAI/Directory.Build.props)";
                return;
            }
            string dir = managed;
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) => {
                string name = new AssemblyName(args.Name).Name + ".dll";
                string candidate = Path.Combine(dir, name);
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };
            string dll = Path.Combine(managed, "Assembly-CSharp.dll");
            if(!File.Exists(dll)) {
                _skipReason = $"Assembly-CSharp.dll not found in {managed}";
                return;
            }
            _gameDir = managed;
            _gameAssembly = Assembly.LoadFrom(dll);
            GameAccess.Init(_gameAssembly);
        }
    }

    private static string FindManagedDir() {
        string env = Environment.GetEnvironmentVariable("GAME_MANAGED_DIR");
        if(!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;
        string dir = AppContext.BaseDirectory;
        for(int i = 0; i < 8 && dir != null; i++) {
            string props = Path.Combine(dir, "ADOFAI", "Directory.Build.props");
            if(File.Exists(props)) {
                string text = File.ReadAllText(props);
                string gamePath = Regex.Match(text, @"<GamePath>(.*?)</GamePath>").Groups[1].Value;
                string gameData = Regex.Match(text, @"<GameData>(.*?)</GameData>").Groups[1].Value;
                if(!string.IsNullOrEmpty(gamePath) && !string.IsNullOrEmpty(gameData)) {
                    string managed = Path.Combine(gamePath, gameData, "Managed");
                    if(Directory.Exists(managed)) return managed;
                }
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    [Fact]
    public void AllRegisteredMembers_ResolveAgainstRealGame() {
        EnsureGame();
        if(_gameAssembly == null) {
            _output.WriteLine($"SKIPPED: {_skipReason}");
            return;
        }

        var missing = new List<string>();
        foreach(var field in typeof(GameAccess).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if(!field.FieldType.IsGenericType ||
                field.FieldType.GetGenericTypeDefinition() != typeof(SafeMember<>)) {
                continue;
            }
            var member = (SafeMemberBase)field.GetValue(null);
            if(member != null && !member.IsResolved) {
                missing.Add($"{field.Name} -> {member.DisplayName}");
            }
        }

        Assert.True(missing.Count == 0,
            "Unresolved game members:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void HitMarginType_Resolves() {
        EnsureGame();
        if(_gameAssembly == null) {
            _output.WriteLine($"SKIPPED: {_skipReason}");
            return;
        }

        Assert.NotNull(GameAccess.HitMarginType);
        Assert.NotNull(GameAccess.ParseHitMargin("XPerfect"));
        Assert.Null(GameAccess.ParseHitMargin("NoSuchMargin_xyz"));
    }
}
