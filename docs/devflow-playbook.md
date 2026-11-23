# MAUI DevFlow Playbook (Stage Buddy)

Working reference for driving the Stage Buddy apps via Microsoft's MAUI DevFlow CLI during debugging sessions. All commands below were run and verified on this machine (2026-07-03; e2e suite added 2026-07-04). The agent is permanently wired (Debug-only) into **both** heads: `StageBuddy.MacHelper` and `StageBuddy.App`.

## Automated simulator e2e suite

`tests/StageBuddy.App.EndToEndTests` drives the real iOS app on the simulator through this CLI — import journeys, a full live talk run in rehearsal mode, the copy-failure/QR path, and the placeholder sheet. It is opt-in (skips without the env var) because it needs macOS + Xcode simulators + the `maui` CLI:

```bash
STAGEBUDDY_E2E=1 dotnet test tests/StageBuddy.App.EndToEndTests
```

The fixture builds the app, boots a simulator if none is running, quits a running Mac helper (its agent interferes — see gotchas), reinstalls the app per test for a clean data container, and seeds talk files straight into `{container}/Library/talks/` where useful. Tests address elements by `AutomationId` only, so UI label/style changes don't break them. CLI syntax notes learned the hard way:

- `ui fill` needs **both positionals**: `maui devflow -p ios ui fill <elementId> <text>` — the element id equals the AutomationId when one is set. The `--automationId` option alone leaves the text argument unbound ("Required argument missing").
- `ui query --automationId X --format compact` is the reliable existence check; `--wait-until exists` also works.
- Never rely on the native nav-bar back chevron (unreachable); every test flow exits forward through in-app buttons (`SettingsDoneButton`, `SummaryDoneButton`, import auto-pop).

## One-time setup (per project)

1. **Package version** in `Directory.Packages.props` (repo uses Central Package Management):
   ```xml
   <PackageVersion Include="Microsoft.Maui.DevFlow.Agent" Version="0.1.0-preview.14.26567.2" />
   ```
   - GOTCHA: the agent requires `Microsoft.Maui.Controls >= 10.0.20` (the repo is on 11.0.10 since the .NET 11 migration). It only ships net10 builds, which the net11 heads consume. After bumping, verify: `dotnet build StageBuddy.slnx` (0 errors) and `dotnet test tests/StageBuddy.Core.Tests` (91 tests passing).
2. **Reference** in `src/StageBuddy.MacHelper/StageBuddy.MacHelper.csproj`, inside the existing Debug-only ItemGroup (alongside `Reactor.Maui.HotReload`) so it never ships in Release:
   ```xml
   <ItemGroup Condition="'$(Configuration)'=='Debug'">
     <PackageReference Include="Microsoft.Maui.DevFlow.Agent" />
   </ItemGroup>
   ```
3. **Wire up** in `src/StageBuddy.MacHelper/MauiProgram.cs`: `using Microsoft.Maui.DevFlow.Agent;` at top, then after the `.UseMauiReactorApp<...>()` builder configuration, before `builder.Services.AddSingleton(...)`:
   ```csharp
   #if DEBUG
       builder.AddMauiDevFlowAgent();
   #endif
   ```
4. **CLI tool** (global, already installed here): `dotnet tool install -g Microsoft.Maui.Cli --prerelease`. Verify with `maui devflow --help`.

## Build + run: NEVER combine build and run

`dotnet build ... -t:Run` in one invocation is unreliable in this SDK/environment: it sometimes skips compiling new code (incremental-build quirk) or tries to `open` the app before it exists ("The file wasn't found" / MSB3073). Cost 3 failed attempts. Always split:

```bash
# Step 1: build only — confirm "Build succeeded", 0 errors
dotnet build src/StageBuddy.MacHelper -f net11.0-maccatalyst

# Step 2: launch separately
open -a "/Users/mallibone/Work/Playground/StageBuddy/src/StageBuddy.MacHelper/bin/Debug/net11.0-maccatalyst/maccatalyst-arm64/Stage Buddy Helper.app"
```

Same issue applies to iOS. For **physical iOS devices**, skip `-t:Run`/mlaunch entirely (mlaunch failed to see the physical iPad over classic USB pairing even when trusted); use devicectl after a plain build:

```bash
xcrun devicectl device install app --device <udid> <path-to>.app
xcrun devicectl device process launch --device <udid> <bundle-id>
```

## Verify the agent is live

```bash
maui devflow diagnose
```
Expect: `broker_running: true`, `agent_count: 1`, an `agents[]` entry with `platform: "MacCatalyst"`, `appName: "Stage Buddy Helper"`, `port: 9223`, and `projects[]` listing DevFlow-enabled csproj files (discovered by scanning for the PackageReference). Also useful: `maui devflow list` (just the agents; can be `[]` even when a project is DevFlow-enabled if the agent isn't reachable), and `maui devflow -p maccatalyst ui status` (agent/app/device details + capabilities).

Ports: the **app itself** listens on TCP **9223**; the CLI's broker daemon listens on **19223**. Check: `lsof -iTCP -sTCP:LISTEN -P | grep 9223`.

**If `agent_count` is 0 but the app is running:** the DevFlow DLLs probably didn't make it into the bundle (incremental build again). Verify:
```bash
find "<path-to>/Stage Buddy Helper.app" -iname "*devflow*"
# expect under Contents/MonoBundle/: Microsoft.Maui.DevFlow.Agent.dll, .Agent.Core.dll, .Logging.dll
```
If missing: delete the project's `obj/` and `bin/`, rebuild (build-only, as above), relaunch. This fixed it here.

## Driving the UI (all verified)

Always pass `-p maccatalyst` for the Mac helper. Output is JSON.

```bash
# Full live visual tree (type, text, bounds, children, ...) — read on-screen state without a human
maui devflow -p maccatalyst ui tree

# Targeted + compact: just Labels with their text (much easier to parse than the full tree)
maui devflow -p maccatalyst ui query --type Label --format compact
# e.g. returns: "Waiting for Stage Buddy…", "No snippets received yet"

# Tap by visible text; --and-tree dumps the tree after the tap to confirm the effect in one round trip
maui devflow -p maccatalyst ui tap --text "Decline" --and-tree
# tap also accepts --automationId, --type, and --index (when multiple match)

# Agent connection / app / capabilities check
maui devflow -p maccatalyst ui status

# Screenshot to a file
maui devflow -p maccatalyst ui screenshot --output helper.png
```

Other `ui` subcommands available (from `maui devflow ui --help`, not yet exercised): `alert` (system dialogs), `assert`, `clear`, `element`, `fill`, `focus`, `hit-test`, `navigate` (Shell routes), `permission` (iOS simulator only), `property` / `set-property` (live editing), `resize`, `scroll`.

Other top-level `maui devflow` commands: `agent`, `batch`, `broker`, `commands`, `device`, `extensions`, `init`, `logs`, `mcp` (starts an MCP server — an alternative to shelling out to the CLI per call), `network`, `recording`, `skills`, `storage`, `theme`, `version`.

## Limitations and gotchas

- **No physical iOS devices.** The DevFlow Agent (tap/tree/screenshot) supports Mac Catalyst and iOS Simulator only — NOT real iPhones/iPads (per Microsoft's platform support table: https://learn.microsoft.com/en-us/dotnet/maui/developer-tools/devflow/?view=net-maui-10.0). It can automate the Mac side of a cross-device test; the real-iPad side still needs a human.
- **A running Mac helper can block the simulator app's agent.** Both agents want host TCP port 9223 (simulators share the Mac's network stack). Observed: with the helper running, `maui devflow diagnose` stayed stuck at `agent_count: 1` (MacCatalyst only) and the iOS app never registered. Quit the helper (`osascript -e 'quit app "Stage Buddy Helper"'`) before simulator work — the e2e fixture does this automatically.
- **Stray simulators pollute Multipeer/Bonjour discovery.** Simulators share the host Mac's network stack, so a booted simulator running an old app build appears as a real peer to the Mac helper (we saw a confusing "unknown peer" pairing dialog from a simulator left over from hours earlier). Before trusting any DevFlow-observed pairing/discovery state during physical-device testing:
  ```bash
  xcrun simctl list devices booted   # should list no devices
  ```
  A stray dialog can be dismissed without human help: `maui devflow -p maccatalyst ui tap --text "Decline" --and-tree`.
