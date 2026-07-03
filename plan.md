# Stage Buddy — Complete Implementation Plan

> **Audience:** a fresh Claude Sonnet session executing this plan with no memory of any prior conversation. Everything you need is in this file, `stage-buddy-spec.md` (the functional spec, source of truth for behavior), and `progress.md` (execution state — create it on first start, see §8). Read all three before writing any code.

---

## 1. Goal & Constraints

Build **Stage Buddy**: an iOS/iPadOS companion app for delivering live conference talks, plus a small Mac Catalyst helper app. The iOS app tracks talk pacing on a story-arc timeline and steps through demo notes/snippets; tapping **Copy** sends the snippet peer-to-peer (MultipeerConnectivity) to the Mac helper, which puts it on the Mac clipboard for instant pasting during live demos.

**Hard constraints (from spec — do not deviate without escalation, §7):**

- .NET 10, .NET MAUI, **MauiReactor** (MVU component framework — **no XAML anywhere**).
- Targets: `net10.0-ios` (main app), `net10.0-maccatalyst` (helper app). Same solution. No Android/Windows.
- Fully local data. No cloud, no backend, no runtime API dependencies.
- MultipeerConnectivity via the built-in `Microsoft.iOS`/`Microsoft.MacCatalyst` bindings (namespace `MultipeerConnectivity` is already exposed — **no custom binding project**).
- Talk content is authored externally (Markdown → JSON via AI) and imported by pasting JSON. **No in-app content editing.**
- Git from the start: sensible .NET .gitignore, frequent commits, professional commit messages (see §9).
- Build order: **M1 local core → M2 P2P sync → M3 resilience & polish** (spec §9). Do not start M2 before M1's definition of done is met.

**Verified environment (2026-07-02):** .NET SDK 10.0.301; `maui` + `macos` workloads installed; Xcode 26.5 (build 17F42). macOS host.

---

## 2. Solution Layout

Four source projects + one test project. Two separate app heads — do **not** multi-target one app for both roles: the iOS app and the Mac helper are different products (different bundle ID, Info.plist, entitlements, UI). Sharing happens through class libraries, not TFMs.

```
StageBuddy/
├── StageBuddy.slnx                    # .NET 10 XML solution format
├── Directory.Build.props              # net-wide: Nullable, ImplicitUsings, LangVersion latest
├── Directory.Packages.props           # Central Package Management (CPM)
├── .gitignore                         # from `dotnet new gitignore`
├── stage-buddy-spec.md                # (exists) functional spec
├── plan.md                            # (this file)
├── progress.md                        # execution state — maintain per §8
├── src/
│   ├── StageBuddy.Core/               # TFM net10.0 — ZERO MAUI/platform refs. All logic lives here.
│   ├── StageBuddy.Sync.Multipeer/     # TFMs net10.0-ios;net10.0-maccatalyst — MC implementation. No MAUI ref.
│   ├── StageBuddy.App/                # MAUI + MauiReactor head, net10.0-ios
│   └── StageBuddy.MacHelper/          # MAUI + MauiReactor head, net10.0-maccatalyst ONLY
└── tests/
    └── StageBuddy.Core.Tests/         # TFM net10.0, xUnit v3
```

**Dependency graph:** `App → Core + Sync.Multipeer` · `MacHelper → Core + Sync.Multipeer` · `Sync.Multipeer → Core` · `Tests → Core` (only Core — the test project must restore and run on any host with plain `dotnet test`, no Xcode, no simulators).

**Why a separate `StageBuddy.Sync.Multipeer` library instead of `#if` blocks inside each app:** the MultipeerConnectivity API surface is byte-for-byte identical in `Microsoft.iOS.dll` and `Microsoft.MacCatalyst.dll`. A library multi-targeting both TFMs compiles the *same source* against both bindings with near-zero `#if` directives, and both heads reference it. It needs no MAUI reference. `#if IOS` / `#if MACCATALYST` stays available inside it for the rare divergence (there should be almost none).

**Why the Mac helper is a MAUI/MauiReactor project (not bare UIKit):** the stack already carries MAUI + MauiReactor; reusing it for a one-screen helper (status dot + last received snippet) is cheaper than hand-written UIKit and keeps one UI idiom.

**Bundle IDs:** `com.mallibone.stagebuddy` (App), `com.mallibone.stagebuddy.helper` (MacHelper). The MC `serviceType` must be **identical** in both apps; the bundle IDs must be **different**. Mismatched service type / plist keys is the #1 "they never see each other" bug.

**NuGet packages (all via CPM in `Directory.Packages.props`):**

| Package | Version | Used by | Notes |
|---|---|---|---|
| `Reactor.Maui` | 4.0.x (≥ 4.0.18) | App, MacHelper | **Must be the 4.0.x line** — 3.x targets .NET 8/9 and will not restore against MAUI 10 |
| `QRCoder` | 1.8.x | App (M3 only) | Use `PngByteQRCode` (no System.Drawing dependency) |
| `xunit.v3` + runner | latest stable | Tests | See §6 rationale |
| `Microsoft.Extensions.TimeProvider.Testing` | 9.x | Tests | `FakeTimeProvider` for pacing/backoff tests |

**Storage decision: flat JSON files, NOT SQLite.** Talks are imported, read, and displayed as whole documents — never partially queried or joined; the library holds tens of items. SQLite would force JSON-in-a-column for the nested `Beat → Steps` structure and adds a native dependency for nothing. The import format *is* the storage format. Layout under the app-data root:

```
{root}/talks/{talkId}.json                      # imported TalkDocument, re-serialized
{root}/runs/{talkId}/{yyyyMMdd-HHmmss}.json     # one TalkRunLog per completed run (M3)
```

---

## 3. File-by-File Plan

Every file below states *why it exists*, *what pattern to follow*, and *what to avoid*. Signatures are normative unless a compile error forces a change (note deviations in progress.md).

### 3.1 Repo root

| File | Content & rationale |
|---|---|
| `Directory.Build.props` | `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. One place, all projects. |
| `Directory.Packages.props` | `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` + `<PackageVersion>` entries. Avoid: version attributes in csproj files. |
| `.gitignore` | Generate with `dotnet new gitignore`. |
| `StageBuddy.slnx` | Create via `dotnet new sln --format slnx` (or `dotnet sln add` produces it on .NET 10 when named .slnx). |

### 3.2 `src/StageBuddy.Core/` — all logic, zero platform dependencies

**Rule for this whole project: no reference to `Microsoft.Maui.*` or platform bindings, ever.** If a type here needs the clipboard, a file path root, or "now", it takes an abstraction (`ISnippetSender`, constructor `string rootPath`, `TimeProvider`). This is what makes ~80% of the app testable with plain `dotnet test`.

#### Models (`Models/`)

`TalkDocument.cs`, `SessionInfo.cs`, `Beat.cs`, `DemoStep.cs`, `BeatType.cs` — C# records mirroring the spec JSON schema exactly:

```csharp
public sealed record TalkDocument(SessionInfo Session, IReadOnlyList<Beat> Timeline);
public sealed record SessionInfo(string Title, int PlannedDurationMinutes);
public sealed record Beat(
    string Id,
    BeatType Type,                     // Talk | Demo
    string Title,
    int PlannedDurationMinutes,
    IReadOnlyList<DemoStep>? Steps);   // null for talk beats
public sealed record DemoStep(
    string Id, string Note, string? Snippet, string? Language, string? FallbackNote);
public enum BeatType { Talk, Demo }
```

Rationale: immutable records = safe to share across engine/UI; schema-shaped = import format doubles as storage format. Avoid: mutable POCOs, inheritance hierarchies for beat types (the enum + optional `Steps` matches the JSON reality and is simpler).

#### Serialization (`Serialization/TalkJsonContext.cs`)

System.Text.Json **source-generated** context — mandatory, not optional: reflection-based STJ produces trimmer/AOT warnings and runtime failures on iOS.

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(JsonStringEnumConverter<BeatType>)],
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(TalkDocument))]
[JsonSerializable(typeof(SyncEnvelope))]
[JsonSerializable(typeof(TalkRunLog))]
public sealed partial class TalkJsonContext : JsonSerializerContext;
```

Avoid: the non-generic `JsonStringEnumConverter` (not AOT/source-gen compatible — use `JsonStringEnumConverter<BeatType>`); `JsonSerializer.Serialize(obj)` overloads without the context anywhere in the codebase.

#### Validation (`Validation/TalkValidator.cs`, `ValidationResult.cs`, `ValidationError.cs`)

Hand-rolled semantic validator, **not** a JSON Schema library (no mature AOT-safe option; the rules are trivial). Deserialize first (wrap `JsonException` into a friendly single error), then validate:

- Session title non-empty; session `plannedDurationMinutes > 0`.
- Timeline non-empty; all beat ids and step ids non-empty and **globally unique**.
- `demo` beats: ≥ 1 step. `talk` beats: no steps (null/empty).
- Every step: non-empty `note`; if `snippet` present, non-empty.
- **Warning (non-fatal, import proceeds):** sum of beat durations ≠ session duration.

`ValidationResult { bool IsValid; IReadOnlyList<ValidationError> Errors; IReadOnlyList<string> Warnings }`; `ValidationError { string Path; string Message }` with human-readable paths like `timeline[2].steps[0].note` — these render directly on the import screen. Avoid: throwing on first error (collect all); validating inside the models.

#### Import (`Import/TalkImporter.cs`, `Models/TalkSummary.cs`)

`TalkImporter.ImportFromJson(string json)` orchestrates: deserialize → validate → derive stable `TalkId` (slugified title + 8-char content hash — re-import of same content overwrites rather than duplicates) → persist via `ITalkRepository`. Returns a result type carrying either the summary or the validation errors. Avoid: file I/O directly in the importer (goes through the repository).

`TalkSummary(string TalkId, string Title, int PlannedDurationMinutes, int BeatCount)` — lightweight projection for the library list.

#### Live engine (`Live/LiveTalkEngine.cs`, `PaceCalculator.cs`, `LiveTalkSnapshot.cs`, `PaceStatus.cs`, `BeatTiming.cs`)

**The heart of the app. Design rule: the engine is a pure, clock-injected state machine with NO internal timer.** It records event timestamps via `TimeProvider`; every derived value is computed on demand in `GetSnapshot()`. This makes it exhaustively testable with `FakeTimeProvider` and means the UI timer is purely cosmetic.

```csharp
public sealed class LiveTalkEngine(TalkDocument talk, TimeProvider clock)
{
    public void Start();          // begins the run; no-op if already started
    public void NextBeat();       // manual advance (talk beats' "Next" button)
    public void AdvanceStep();    // called AFTER a successful Copy; last step → next beat
    public void Back();           // step-wise within demo beats, beat-wise otherwise; no-op at position 0
    public LiveTalkSnapshot GetSnapshot();
}

public sealed record LiveTalkSnapshot(
    int BeatIndex, int? StepIndex, Beat CurrentBeat, DemoStep? CurrentStep,
    TimeSpan Elapsed, TimeSpan Remaining, double OverallProgress,   // 0..1
    PaceStatus Pace, bool IsFinished,
    IReadOnlyList<BeatTiming> CompletedBeatTimings);                 // feeds M3 post-talk log

public enum PaceStatus { OnPaceOrAhead, WithinTolerance, Behind }    // green / amber / red
```

`PaceCalculator.Compute(TimeSpan elapsed, TimeSpan cumulativePlannedAtPosition, TimeSpan tolerance)` — **static pure function** implementing spec §5: green when `elapsed ≤ planned`, amber when `elapsed ≤ planned + tolerance` (default 1 min), red beyond. "Cumulative planned at position" = sum of planned durations of completed beats + the current beat's planned duration prorated is **not** required — use sum of fully planned durations of all beats *before* the current one plus elapsed-within-beat comparison kept simple: compare total elapsed against cumulative planned of completed beats + current beat's plan. If ambiguity bites during test-writing, escalate per §7 rather than inventing semantics.

`BeatTiming(string BeatId, TimeSpan Planned, TimeSpan Actual)` recorded at each beat transition.

Avoid: `DateTime.UtcNow` anywhere (always the injected `TimeProvider`); a `System.Timers.Timer` inside the engine; pace/color logic in the UI layer.

#### Storage (`Storage/ITalkRepository.cs`, `FileTalkRepository.cs`, `ITalkRunLogRepository.cs`, `FileTalkRunLogRepository.cs`, `TalkRunLog.cs`)

```csharp
public interface ITalkRepository
{
    Task SaveAsync(string talkId, TalkDocument talk, CancellationToken ct = default);
    Task<TalkDocument?> LoadAsync(string talkId, CancellationToken ct = default);
    Task<IReadOnlyList<TalkSummary>> ListAsync(CancellationToken ct = default);
    Task DeleteAsync(string talkId, CancellationToken ct = default);
}
```

`FileTalkRepository(string rootPath)` — takes a **plain string root path** (the MAUI head passes `FileSystem.AppDataDirectory`; tests pass a temp dir). Creates directories lazily; serializes via `TalkJsonContext`. `TalkRunLog` (M3): talk id, started-at, list of `BeatTiming`, total planned/actual. Avoid: referencing `Microsoft.Maui.Storage.FileSystem` here; caching layers (files this small don't need them).

#### Sync abstractions (`Sync/`)

The seam the whole milestone plan pivots on. **M1 ships against `LocalClipboardSnippetSender`; M2 swaps in Multipeer via DI; rehearsal mode (M3) swaps back. `LiveTalkPage` never knows which one it has.**

```csharp
public sealed record Snippet(string Text, string? Language, string TalkTitle, string StepId);
public enum ConnectionState { Disconnected, Discovering, Connecting, Connected }
public enum SendResult { Sent, NotConnected, Failed }

public interface ISnippetSender
{
    ConnectionState State { get; }
    event EventHandler<ConnectionState>? StateChanged;   // ALWAYS raised on the UI thread
    void StartPairing();                                 // idempotent
    void StopPairing();
    Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default);
}

public interface ISnippetReceiver                        // Mac side
{
    ConnectionState State { get; }
    event EventHandler<ConnectionState>? StateChanged;
    event EventHandler<Snippet>? SnippetReceived;        // ALWAYS raised on the UI thread
    void StartAdvertising();
    void StopAdvertising();
}
```

`SyncEnvelope.cs` — versioned wire format (M2, but define + test in M1/M2 boundary):

```csharp
public sealed record SyncEnvelope(int V, string Type, SnippetPayload? Payload);
public sealed record SnippetPayload(string Text, string? Language, string TalkTitle, string StepId);
// V = 1; Type: "snippet" now, "ping"/"ack" reserved.
```

Rationale for an envelope over a raw string: a version field gives forward compatibility between an updated phone app and a stale Mac helper, and a `type` field allows ack/ping later at zero cost. Avoid: raw-string framing; platform types (`NSData` etc.) in any signature here.

`PairingStateMachine.cs` — platform-free connection lifecycle so it's unit-testable. Consumes synthetic events (`PeerFound`, `PeerLost`, `SessionConnected`, `SessionDisconnected`, `PermissionDenied`, `Stopped`), produces state transitions and "retry after X" outputs:

```
Disconnected ─Start→ Discovering ─PeerFound→ Connecting ─SessionConnected→ Connected
     ▲                   ▲                        │ SessionDisconnected            │
     │                   └── retry w/ backoff ────┘                                │
     └────────── Stop / PermissionDenied ◄──────── peer lost / disconnected ───────┘
```

Backoff: 1 s, 2 s, 4 s, capped at 5 s, **retry forever until explicit Stop** (the speaker is on stage — never give up silently). Uses injected `TimeProvider` for backoff scheduling. Avoid: putting this logic inside the Multipeer delegate classes where it can't be tested.

#### Placeholders (`Placeholders/PlaceholderEngine.cs`) — M3

- `DetectPlaceholders(string snippet)` → distinct ordered names matching `{{name}}` (regex `\{\{(\w+)\}\}`).
- `Substitute(string snippet, IReadOnlyDictionary<string,string> values)` → replaces all occurrences; **unknown placeholders are left intact** (visible on the Mac = better than silent corruption).
- Built-in `{{date}}` resolves via injected `TimeProvider` (format `yyyy-MM-dd`) unless overridden by the user.
- Malformed input (`{{` without `}}`) is left as-is, never throws.

### 3.3 `src/StageBuddy.Sync.Multipeer/` — MultipeerConnectivity implementation (M2)

Multi-targets `net10.0-ios;net10.0-maccatalyst`. References Core only. **This is the most pitfall-dense code in the project — read §7 escalation criteria before starting it.** All classes from namespace `MultipeerConnectivity` in the platform bindings.

| File | Content & rationale |
|---|---|
| `MultipeerConstants.cs` | `public const string ServiceType = "stagebuddy-sync";` — 15 chars, exactly at the limit; lowercase letters/digits/hyphen only. Must match `NSBonjourServices` entries in BOTH apps' Info.plists. Define once, referenced everywhere. |
| `MultipeerCommon.cs` | Peer-ID persistence + session factory. **`MCPeerID` must be archived and reused across launches** — `new MCPeerID(name)` every launch creates a *different* underlying identity → ghost peers and flaky reconnects. Archive via `NSKeyedArchiver.GetArchivedData(peerId, true, out _)` into `NSUserDefaults`, restore via `NSKeyedUnarchiver.GetUnarchivedObject(new Class(typeof(MCPeerID)), data, out _)`. Session factory: `new MCSession(peerId, null, MCEncryptionPreference.Required)` — **`.Required` explicitly on both sides**; the default optional encryption historically causes cross-OS-version negotiation failures. |
| `Delegates/SessionDelegate.cs` | Subclasses `MCSessionDelegate`. Overrides `DidChangeState` and `DidReceiveData`; the three stream/resource members get empty bodies (protocol requires them). Forwards to an owner object. **All callbacks arrive on background GCD threads.** |
| `Delegates/BrowserDelegate.cs` | Subclasses `MCNearbyServiceBrowserDelegate`: `FoundPeer` → invite; `LostPeer`; `DidNotStartBrowsingForPeers` → surface error (typically local-network permission denied — the UI should hint at Settings → Privacy → Local Network). |
| `Delegates/AdvertiserDelegate.cs` | Subclasses `MCNearbyServiceAdvertiserDelegate`. `DidReceiveInvitationFromPeer` receives an `invitationHandler` that **must be invoked exactly once on every code path** (a leaked handler leaves the phone hanging in Connecting until timeout). Known pairing token → `invitationHandler(true, session)` immediately; unknown → main-thread confirm dialog, then invoke with the user's answer. |
| `MultipeerSnippetSender.cs` | iOS side, implements `ISnippetSender`. Owns `MCNearbyServiceBrowser` + `MCSession` + `PairingStateMachine`. `FoundPeer` → `browser.InvitePeer(peerID, session, tokenData, 15)` where `tokenData` is a GUID pairing token (generated on first pairing, stored in preferences via a small injected key-value abstraction). On `Connected` → stop browsing (radio/battery). On disconnect → **recreate the `MCSession`** (a disconnected session is spent — never reuse) and restart browsing per the state machine's backoff. `SendAsync`: serialize `SyncEnvelope` with `TalkJsonContext` → `NSData.FromArray(...)` → `session.SendData(data, session.ConnectedPeers, MCSessionSendDataMode.Reliable, out NSError e)`. Guard payloads > 1 MB (snippets are KBs; `SendResource` territory starts at hundreds of KB). |
| `MultipeerSnippetReceiver.cs` | Mac side, implements `ISnippetReceiver`. Owns `MCNearbyServiceAdvertiser` + `MCSession`. Advertises from app launch, never stops while running; after a disconnect just recreates the session and keeps advertising. Persists the accepted pairing token; "Re-pair" clears it. |

**Two .NET-specific wiring rules that apply to every file above:**

1. **Root every delegate object in a field** on the long-lived sender/receiver class (`_sessionDelegate`, `_browserDelegate`, …). The native `.Delegate` property is a *weak* reference — if only Objective-C references the managed subclass, the GC collects it and callbacks **silently stop**. This is the #1 mysterious-failure mode of MC in .NET.
2. **Marshal outcomes to the main thread** with `MainThread.BeginInvokeOnMainThread(...)` (or an injected dispatcher action, since this project has no MAUI ref — inject `Action<Action> dispatchToUi` via constructor) **before** raising `StateChanged`/`SnippetReceived`. Never let a MauiReactor component receive an event on a GCD thread.

Avoid in this project: `MCAdvertiserAssistant`/`MCBrowserViewController` (stock pairing UI — wrong for a podium app; the programmatic pair gives auto-accept + auto-reconnect); business logic in delegates (delegate → owner → state machine).

### 3.4 `src/StageBuddy.App/` — iOS app head (MAUI + MauiReactor)

MauiReactor pattern for every page: a class deriving `Component` (stateless) or `Component<TState>`; `Render()` returns the visual tree via fluent C#; **no XAML files at all**. State mutations only via `SetState`. Services resolved via constructor-injected props or `Services.GetRequiredService<T>()` per MauiReactor conventions.

| File | Content & rationale |
|---|---|
| `MauiProgram.cs` | `builder.UseMauiReactorApp<AppShell>()`. DI: `ITalkRepository → new FileTalkRepository(FileSystem.AppDataDirectory)`, `ISnippetSender → LocalClipboardSnippetSender` (M1; becomes a factory choosing Multipeer vs local in M2/M3), `TimeProvider.System`. |
| `Pages/AppShell.cs` | Root component: navigation host (Shell or NavigationPage — pick whichever MauiReactor 4.x documents as primary; don't fight the framework). Routes: library (home), import, live, settings, post-talk summary (M3). |
| `Pages/TalkLibraryPage.cs` | Lists `TalkSummary` items from `ITalkRepository.ListAsync()`. Tap → LiveTalkPage. Swipe/context actions: Delete, Rehearse (M3). Toolbar: Import, Settings. Empty state points at Import. |
| `Pages/ImportPage.cs` | Big `Editor` as paste target + "Paste from clipboard" button (`Clipboard.Default.GetTextAsync()`) + Import button → `TalkImporter.ImportFromJson`. Failure: render each `ValidationError` (path + message) in a list. Success: navigate back, library refreshed. Warnings shown but non-blocking. |
| `Pages/LiveTalkPage.cs` | The core screen. State holds the `LiveTalkEngine` instance + latest `LiveTalkSnapshot` (MauiReactor state persists across renders). `OnMounted`: create engine, `DeviceDisplay.Current.KeepScreenOn = true`, start an `IDispatcherTimer` at 1 s ticking `SetState(s => s.Snapshot = s.Engine.GetSnapshot())`. `OnWillUnmount`: stop timer, `KeepScreenOn = false`. Explicit **Start** button gates `engine.Start()` — timer display shows 00:00 until tapped (spec §5). Renders `PacingBar` + (`TalkBeatView` \| `DemoStepView`) by `CurrentBeat.Type`. Copy flow: `await sender.SendAsync(snippet)` → `Sent` → `engine.AdvanceStep()`; otherwise show error affordance and **do not advance**. Back button always visible. `IsFinished` → M1: simple "done" state; M3: navigate to PostTalkSummaryPage. Avoid: any pace computation here — the page only projects the snapshot. |
| `Pages/SettingsPage.cs` | M1: static "Local clipboard mode" note. M2: pairing status bound to `ISnippetSender.State`/`StateChanged`, Re-pair button. M3: rehearsal-mode toggle. |
| `Pages/PostTalkSummaryPage.cs` (M3) | Table of actual-vs-planned per beat from `CompletedBeatTimings`, total delta; persists `TalkRunLog` via run-log repository on first render. |
| `Components/PacingBar.cs` | **Stateless** component: takes the snapshot as a prop; renders progress fill (`OverallProgress`), color by `Pace` (green `OnPaceOrAhead`, amber `WithinTolerance`, red `Behind`), elapsed + remaining labels always visible. |
| `Components/TalkBeatView.cs` | Title, planned duration, large **Next beat** button. |
| `Components/DemoStepView.cs` | Step note, snippet in monospace on dark background, large **Copy** button (the primary touch target — size for podium use), fallback-note disclosure if present (M3). |
| `Components/PlaceholderSheet.cs` (M3) | Bottom sheet before first copy of a run when the talk contains placeholders: one entry per detected name, `{{date}}` pre-filled from TimeProvider. Values cached per run. |
| `Components/QrFallbackView.cs` (M3) | QRCoder `PngByteQRCode` → `ImageSource.FromStream`, rendered when `sender.State != Connected` or manually invoked. QR is a **UI affordance, not an `ISnippetSender` implementation** — don't model it as a sender. |
| `Services/LocalClipboardSnippetSender.cs` | `Clipboard.Default.SetTextAsync(snippet.Text)`; `State` always `Connected`; `StartPairing`/`StopPairing` no-ops. This is what makes M1 shippable and M3 rehearsal mode free. |
| `Platforms/iOS/Info.plist` | Standard MAUI template plus (M2): `NSLocalNetworkUsageDescription` ("Stage Buddy connects to your Mac over the local network to send code snippets while you present.") and `NSBonjourServices` array with `_stagebuddy-sync._tcp` **and** `_stagebuddy-sync._udp` — both entries required or discovery silently finds nothing (iOS 14+ local-network privacy). No Bluetooth usage key needed: MC has used AWDL/peer-to-peer Wi-Fi since iOS 11 (spec's "Bluetooth discovery" line is outdated; practical consequence: Wi-Fi must be ON on both devices, joined network not required). |

### 3.5 `src/StageBuddy.MacHelper/` — Mac Catalyst helper head (M2)

| File | Content & rationale |
|---|---|
| `MauiProgram.cs` | MauiReactor bootstrap; DI: `ISnippetReceiver → MultipeerSnippetReceiver`, dispatcher action = `MainThread.BeginInvokeOnMainThread`. |
| `Pages/HelperHomePage.cs` | Single small window: connection status (green dot "Connected to iPhone" / "Waiting for Stage Buddy…"), last-received snippet preview + timestamp. **Keep this window open** — closing the last Catalyst window can terminate the app. Subscribes `SnippetReceived` → `PasteboardWriter.Write`. On `Connected`: take an App Nap suppression token — `NSProcessInfo.ProcessInfo.BeginActivity(NSActivityOptions.UserInitiated \| NSActivityOptions.IdleSystemSleepDisabled, "StageBuddy live session")`; `EndActivity` on disconnect. Without this, a helper hidden behind the IDE for 10+ minutes gets App-Napped and stops receiving. |
| `Services/PasteboardWriter.cs` | `Clipboard.Default.SetTextAsync(text)` on the main thread. **Critical spec correction: `NSPasteboard` (AppKit) is NOT bound in Mac Catalyst** — the spec's `NSPasteboard.general` wording is satisfied via UIKit's `UIPasteboard.General`, which macOS bridges to the system pasteboard; MAUI's `Clipboard` wraps exactly that. Setting it replaces old contents atomically — no `ClearContents`/`DeclareTypes` dance exists or is needed. The helper does **not** need to be frontmost: a background Catalyst app keeps its MC socket and may write the pasteboard. |
| `Platforms/MacCatalyst/Info.plist` | Same `NSLocalNetworkUsageDescription` + `NSBonjourServices` entries as iOS — **macOS 15+ enforces local-network privacy on the Mac too.** |
| `Platforms/MacCatalyst/Entitlements.plist` | `com.apple.security.app-sandbox` true, `com.apple.security.network.client` true, `com.apple.security.network.server` true (advertiser accepts inbound). Wire via `<CodesignEntitlements>` in the csproj for the Catalyst TFM. **Set these from day one** — Debug builds often run unsandboxed, so missing entitlements "work in Debug" and break when signed properly. Pasteboard *writing* needs no entitlement (reads are what trigger macOS pasteboard-privacy alerts; we only write). |

### 3.6 `tests/StageBuddy.Core.Tests/`

xUnit v3, references Core only. One test class per Core area. Required coverage:

| Test class | Must cover |
|---|---|
| `TalkSerializationTests` | Round-trip of the **spec §3 sample JSON verbatim** (embed as a test resource); camelCase mapping; `"talk"`/`"demo"` enum mapping; optional fields absent. |
| `TalkValidatorTests` | Every rule in §3.2-Validation: each failure produces the right path+message; multiple errors collected; duration-mismatch is warning-not-error. |
| `TalkImporterTests` | Happy path persists and returns summary; invalid JSON returns errors without persisting; re-import same content = same id (overwrite, no duplicate). |
| `FileTalkRepositoryTests` | Temp-dir save/load/list/delete; list on empty root; corrupted file doesn't crash listing (skip + continue). |
| `LiveTalkEngineTests` | With `FakeTimeProvider`: not-started state; Start; talk-beat NextBeat; demo AdvanceStep auto-rolls into next beat after last step; Back within steps, Back across a beat boundary, Back at position 0 (no-op); finish detection; `CompletedBeatTimings` actuals match the fake clock's advances. |
| `PaceCalculatorTests` | Green at exactly on-plan; amber at plan+1ms through plan+tolerance; red past tolerance; boundary at exactly ±tolerance; ahead of schedule. |
| `PairingStateMachineTests` (M2) | Full transition table; backoff sequence 1/2/4/5/5…; retry-forever until Stop; PermissionDenied terminal until restarted. |
| `SyncEnvelopeTests` (M2) | Round-trip; unknown `type` tolerated; `v` mismatch surfaces detectably. |
| `PlaceholderEngineTests` (M3) | Detection order/distinctness; multi-occurrence substitution; unknown left intact; `{{date}}` via FakeTimeProvider; malformed braces never throw; special chars in values. |

Framework rationale: xUnit v3 has first-class .NET 10/Microsoft.Testing.Platform support and is the ecosystem default; TUnit is younger (novelty risk), MSTest adds nothing here.

---

## 4. Task Order & Dependencies

Execute strictly in order within a milestone; tasks marked ∥ may run in parallel. **Commit after every green task** (§9).

### M1 — Local core (simulator only)

| # | Task | Depends on |
|---|---|---|
| 1 | Scaffold: `git init`, `dotnet new gitignore`, slnx, Build/Packages props, all 5 projects + references, empty-but-compiling. Verify `dotnet build` + `dotnet test` pass. | — |
| 2 | Core models + `TalkJsonContext` + `TalkSerializationTests` | 1 |
| 3 | `TalkValidator` + `TalkImporter` + tests | 2 |
| 4 ∥ | `FileTalkRepository` + `TalkSummary` + tests | 2 |
| 5 ∥ | `LiveTalkEngine` + `PaceCalculator` + tests (the largest test surface — see §3.6) | 2 |
| 6 | App head bootstrap: MauiProgram, AppShell, DI; builds & launches on iOS simulator | 1 |
| 7 | `TalkLibraryPage` + `ImportPage` (+ `LocalClipboardSnippetSender`) | 3, 4, 6 |
| 8 | `LiveTalkPage` + `PacingBar` + `TalkBeatView` + `DemoStepView` | 5, 7 |
| 9 | M1 review & verification (§5), fix findings, commit | 8 |

### M2 — MultipeerConnectivity (physical iPhone + Mac required for verification)

| # | Task | Depends on |
|---|---|---|
| 1 | `SyncEnvelope` + `PairingStateMachine` in Core + tests | M1 |
| 2 | `MultipeerCommon` (peer-ID archiving, session factory) | 1 |
| 3 ∥ | `MultipeerSnippetReceiver` + advertiser/session delegates | 2 |
| 4 ∥ | `MultipeerSnippetSender` + browser/session delegates + reconnect loop | 2 |
| 5 | MacHelper head: HelperHomePage, PasteboardWriter, App Nap token, Info.plist, Entitlements.plist | 3 |
| 6 | iOS app wiring: DI swap (sender factory), Settings pairing UI, iOS Info.plist keys | 4 |
| 7 | Compile-only gate: both heads build for their TFMs; then **STOP — device verification is a user checkpoint** (§5 M2). Record status in progress.md. | 5, 6 |

### M3 — Resilience & polish

| # | Task | Depends on |
|---|---|---|
| 1 ∥ | `PlaceholderEngine` + tests | M1 |
| 2 | `PlaceholderSheet` + Copy-flow integration (resolve before send, cache per run) | 1 |
| 3 ∥ | Fallback-note disclosure in `DemoStepView` | M1 |
| 4 ∥ | Rehearsal mode: sender factory honors toggle → local sender + on-screen badge | M2 task 6 |
| 5 ∥ | `QrFallbackView` shown when `State != Connected` + manual trigger | M2 |
| 6 | `TalkRunLog` + `FileTalkRunLogRepository` + tests + `PostTalkSummaryPage` + last-run deltas in library | M1 |
| 7 | Final review & verification, commit | all |

---

## 5. Definition of Done

**Every task:** solution builds with zero warnings (warnings are errors); `dotnet test tests/StageBuddy.Core.Tests` fully green; new logic in Core has tests per §3.6; committed.

**M1 done when:** on the iOS simulator you can — import the spec §3 sample JSON via paste (and see validation errors when you mangle it), see the talk in the library, start it with the Start button, watch elapsed/remaining tick and the pace bar change color as the fake-real clock runs, advance talk beats with Next, step through demo steps with Copy (lands on simulator clipboard, auto-advances), go Back across a beat boundary, and reach the finished state. All engine/validator/repository behavior covered by green tests.

**M2 done when:** both heads compile for their TFMs, all Core tests green, plists/entitlements in place per §3.4/§3.5, and the **manual two-device checklist** passes (this requires the user — physical iPhone + Mac; MC does not work on simulators, no AWDL):

1. First launch both apps → local-network permission prompts appear with our usage strings (iOS and macOS 15+); denying on iOS surfaces a Settings hint; re-allowing recovers without reinstall.
2. First pairing: Mac shows confirm dialog with the phone's name; Accept → phone chip goes Discovering → Connecting → Connected in < ~5 s; Decline → returns to Discovering, no crash.
3. Known-peer reconnect: kill + relaunch phone app → auto-reconnects with **no** Mac dialog in < ~5 s.
4. Copy path: Copy on phone → ⌘V in a real Mac editor pastes the exact snippet (multi-line, tabs, non-ASCII).
5. Rapid fire: 5 fast consecutive Copies → pasteboard holds the last one, none dropped (`.Reliable`).
6. Helper hidden behind a full-screen IDE 10+ min → Copy still pastes (App Nap token works).
7. Phone backgrounded 30 s → returns → auto-reconnect; KeepScreenOn prevents lock during Live View.
8. Mac sleep/wake → reconnects without restarting either app.
9. Both devices Wi-Fi ON but joined to no network → still works (AWDL). Phone Wi-Fi OFF → drops, UI shows disconnected state.
10. Re-pair in Settings → next connect shows the Mac confirm dialog again.

**M3 done when:** placeholders resolve via the sheet before first copy (with `{{date}}` prefilled, unknowns left intact); fallback notes visible on steps that have them; rehearsal mode runs a full talk with no Mac and a visible badge; QR fallback renders when disconnected and scans correctly; finishing a talk shows actual-vs-planned per beat and persists a run log; library shows last-run deltas. All M3 Core logic tested.

**No UI automation for v1** (deliberate): the risky code is either pure (unit-tested) or radio/OS-bound (unreachable by UI automation); MauiReactor views are thin projections of tested state. Do not add Appium/XCUITest.

---

## 6. Verification Commands

```bash
dotnet build StageBuddy.slnx                                  # gate for every task
dotnet test tests/StageBuddy.Core.Tests                       # gate for every task (host-side, no Xcode)
dotnet build src/StageBuddy.App -f net10.0-ios                # compile gate for platform code
dotnet build src/StageBuddy.MacHelper -f net10.0-maccatalyst  # compile gate for platform code
# Run on iOS simulator (M1 verification):
dotnet build src/StageBuddy.App -f net10.0-ios -t:Run
# Run Mac helper locally (M2):
dotnet build src/StageBuddy.MacHelper -f net10.0-maccatalyst -t:Run
```

Device deploy (M2, user-assisted): iPhone needs Developer Mode on; easiest path is letting Xcode create an automatic dev provisioning profile for `com.mallibone.stagebuddy` once, then `dotnet build -t:Run -f net10.0-ios -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Development"`. MC needs no special App ID capability.

Optional dev-loop improvement: `dotnet tool install -g Reactor.Maui.HotReload`, then `dotnet-maui-reactor -f net10.0-ios` alongside the app for MauiReactor hot reload.

---

## 7. Escalation Criteria — when to invoke a Fable subagent

You (Sonnet) execute directly by default. Spawn a **Fable** subagent (Agent tool, `model: "fable"`) only when you hit one of these; give it **only the narrow problem statement + the specific files involved + the relevant section of this plan** — never the whole codebase:

1. **Ambiguous architectural tradeoff** — the plan under-specifies something and two reasonable designs diverge in ways that ripple (e.g. the pace-calculation semantics ambiguity flagged in §3.2 actually bites; how navigation state should survive a process restart mid-talk).
2. **Cross-cutting refactor spanning > 5 files** — a needed change touches interfaces in Core plus both heads plus the sync library at once.
3. **MultipeerConnectivity misbehavior after the checklist is followed** — discovery/connection fails even with delegates rooted in fields, peer ID archived, encryption `.Required`, plist keys and entitlements verified. These bugs are subtle (GC, threading, session lifecycle); don't burn hours probing — hand Fable the sender/receiver/delegate files + observed symptoms.
4. **Trimmer/AOT failures on device builds** that aren't fixed by adding a type to `TalkJsonContext`.
5. **Security-adjacent decisions** beyond the plan: changing the pairing trust model, loosening `MCEncryptionPreference`, adding any network surface beyond MC.
6. **MauiReactor 4.x fighting back** — an API assumed here doesn't exist and the workaround would restructure a page's state model rather than rename a call.

Do **not** escalate for: compile errors, package-version bumps, test failures with clear causes, renames, plist typos, or anything where this plan already states the decision.

## 8. Handoff Protocol — progress.md

Maintain `progress.md` at the repo root. **Update it (a) after every completed task, (b) before starting any known connectivity gap (M2 device checkpoint, end of a work session), and (c) proactively when your context is filling up** — a restart must never require rebuilding context from scratch. Keep it short and current; overwrite, don't append a journal. Format:

```markdown
# Progress — updated <date>
## Done
- M1.1 scaffold (commit abc1234)
- M1.2 Core models + serialization (commit def5678)
## In progress
- M1.5 LiveTalkEngine — engine done, PaceCalculator boundary tests failing (amber edge), see tests/.../PaceCalculatorTests.cs
## Next
- M1.6 App head bootstrap
## Blockers / decisions needed
- (e.g.) M2.7 device checklist needs the user's physical iPhone
## Deviations from plan.md
- (anything you changed vs. the plan, and why)
```

## 9. Git Conventions

- Repo not yet initialized — M1 task 1 does `git init` on branch `main`, first commit = scaffold + spec + plan.md.
- Conventional Commits, imperative mood, scoped where useful: `feat(core): add LiveTalkEngine with clock-injected pacing`, `test(core): cover pace tolerance boundaries`, `chore: scaffold solution with CPM`, `fix(sync): root browser delegate to prevent GC collection`.
- Commit after **every** green task, not per milestone. Never commit red tests or build warnings.
- End every commit message with: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
