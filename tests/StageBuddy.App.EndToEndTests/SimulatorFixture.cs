namespace StageBuddy.App.EndToEndTests;

static class E2E
{
    public static bool Enabled => Environment.GetEnvironmentVariable("STAGEBUDDY_E2E") == "1";

    public static void SkipUnlessEnabled()
        => Assert.SkipUnless(Enabled,
            "Simulator e2e tests are opt-in: set STAGEBUDDY_E2E=1 (requires macOS, Xcode simulators, and the maui CLI).");
}

/// <summary>
/// Builds the iOS app once, ensures a booted simulator and a running DevFlow broker.
/// Individual tests call <see cref="FreshInstall"/> for a clean app-data container.
/// Does nothing unless STAGEBUDDY_E2E=1.
/// </summary>
public sealed class SimulatorFixture : IAsyncLifetime
{
    private string _appBundlePath = string.Empty;

    internal DevFlow DevFlow { get; } = new();
    internal string RepoRoot { get; private set; } = string.Empty;

    public ValueTask InitializeAsync()
    {
        if (!E2E.Enabled)
            return ValueTask.CompletedTask;

        RepoRoot = FindRepoRoot();

        // Build-only, never -t:Run (see docs/devflow-playbook.md); explicit install happens per test.
        Cli.RunChecked(
            "dotnet", ["build", "src/StageBuddy.App", "-f", "net10.0-ios"],
            workingDirectory: RepoRoot, timeout: TimeSpan.FromMinutes(10));

        _appBundlePath = FindAppBundle();

        if (Simulator.BootedUdid() is null)
            Simulator.BootPreferredIPhone();

        // A running Mac helper carries its own DevFlow agent on the shared host port and can block
        // the simulator app's agent from registering (observed: agent list stuck at MacCatalyst-only).
        Cli.Run("osascript", ["-e", "quit app \"Stage Buddy Helper\""]);

        DevFlow.EnsureBrokerRunning();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (E2E.Enabled)
            Simulator.Terminate();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Reinstalls the app (clean data container), seeds the given talk files into
    /// {container}/Library/talks/, launches, and waits until the DevFlow agent responds.
    /// Talk ids in the library equal the seeded file names (FileTalkRepository derives ids from file names).
    /// </summary>
    internal void FreshInstall(params (string TalkId, string Json)[] seedTalks)
    {
        Simulator.Terminate();
        Simulator.Uninstall();
        Simulator.Install(_appBundlePath);

        if (seedTalks.Length > 0)
        {
            var talksDirectory = Path.Combine(Simulator.DataContainer(), "Library", "talks");
            Directory.CreateDirectory(talksDirectory);
            foreach (var (talkId, json) in seedTalks)
                File.WriteAllText(Path.Combine(talksDirectory, $"{talkId}.json"), json);
        }

        Simulator.Launch();
        DevFlow.WaitUntil(DevFlow.IsIosAgentConnected, timeoutSeconds: 60, "iOS DevFlow agent did not register after launch");
    }

    internal static string TestData(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StageBuddy.slnx")))
                return directory.FullName;
            directory = directory.Parent!;
        }
        throw new InvalidOperationException("Could not locate the repo root (StageBuddy.slnx) above the test binary.");
    }

    private string FindAppBundle()
    {
        var binRoot = Path.Combine(RepoRoot, "src", "StageBuddy.App", "bin", "Debug", "net10.0-ios", "iossimulator-arm64");
        var bundle = Directory.Exists(binRoot)
            ? Directory.EnumerateDirectories(binRoot, "*.app").FirstOrDefault()
            : null;
        return bundle
            ?? throw new InvalidOperationException($"No .app bundle found under {binRoot} — did the build succeed?");
    }
}

[CollectionDefinition("Simulator")]
public sealed class SimulatorCollection : ICollectionFixture<SimulatorFixture>;
