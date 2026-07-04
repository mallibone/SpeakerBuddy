using System.Text.Json;

namespace StageBuddy.App.EndToEndTests;

/// <summary>Wrapper over `xcrun simctl` for the booted iOS simulator.</summary>
static class Simulator
{
    public const string BundleId = "com.mallibone.stagebuddy";

    public static string? BootedUdid()
    {
        var result = Cli.RunChecked("xcrun", ["simctl", "list", "devices", "booted", "-j"]);
        using var doc = JsonDocument.Parse(result.StdOut);
        foreach (var runtime in doc.RootElement.GetProperty("devices").EnumerateObject())
        {
            foreach (var device in runtime.Value.EnumerateArray())
            {
                if (device.GetProperty("state").GetString() == "Booted")
                    return device.GetProperty("udid").GetString();
            }
        }
        return null;
    }

    /// <summary>Boots the newest available iPhone simulator and waits for it to finish booting.</summary>
    public static string BootPreferredIPhone()
    {
        var result = Cli.RunChecked("xcrun", ["simctl", "list", "devices", "available", "-j"]);
        using var doc = JsonDocument.Parse(result.StdOut);

        string? udid = null;
        string? pickedRuntime = null;
        foreach (var runtime in doc.RootElement.GetProperty("devices").EnumerateObject())
        {
            if (!runtime.Name.Contains("iOS", StringComparison.Ordinal))
                continue;
            foreach (var device in runtime.Value.EnumerateArray())
            {
                var name = device.GetProperty("name").GetString() ?? "";
                if (!name.StartsWith("iPhone", StringComparison.Ordinal))
                    continue;
                // runtimes sort lexicographically well enough within a major era; prefer the last one seen
                if (pickedRuntime is null || string.CompareOrdinal(runtime.Name, pickedRuntime) >= 0)
                {
                    pickedRuntime = runtime.Name;
                    udid = device.GetProperty("udid").GetString();
                }
            }
        }

        if (udid is null)
            throw new InvalidOperationException("No available iPhone simulator found (xcrun simctl list devices available).");

        Cli.Run("xcrun", ["simctl", "boot", udid]); // exit 149 if already booted — tolerated
        Cli.RunChecked("xcrun", ["simctl", "bootstatus", udid, "-b"], timeout: TimeSpan.FromMinutes(3));
        return udid;
    }

    public static void Install(string appPath)
        => Cli.RunChecked("xcrun", ["simctl", "install", "booted", appPath]);

    public static void Uninstall()
        => Cli.Run("xcrun", ["simctl", "uninstall", "booted", BundleId]); // tolerated if not installed

    public static void Launch()
        => Cli.RunChecked("xcrun", ["simctl", "launch", "booted", BundleId]);

    public static void Terminate()
        => Cli.Run("xcrun", ["simctl", "terminate", "booted", BundleId]); // tolerated if not running

    public static string DataContainer()
        => Cli.RunChecked("xcrun", ["simctl", "get_app_container", "booted", BundleId, "data"]).StdOut.Trim();

    public static string Pasteboard()
        => Cli.RunChecked("xcrun", ["simctl", "pbpaste", "booted"]).StdOut.TrimEnd('\n');

    public static void SetPasteboard(string text)
        => Cli.RunChecked("xcrun", ["simctl", "pbcopy", "booted"], stdin: text);
}
