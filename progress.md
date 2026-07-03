# Progress — updated 2026-07-03

## Done
- M1.1 scaffold: git init (branch main), .gitignore, StageBuddy.slnx, Directory.Build.props, Directory.Packages.props (CPM), NuGet.Config, all 5 projects created + referenced correctly, full solution builds with 0 warnings/errors, iOS and MacCatalyst per-TFM builds verified.
- M1.2 Core models (SessionInfo, Beat, BeatType, DemoStep, TalkDocument) + TalkJsonContext (source-generated STJ context) + TalkSerializationTests (8 tests, all green): spec sample round-trip, camelCase property mapping, talk/demo enum mapping, optional-field-absent behavior.
- M1.3 TalkValidator (+ ValidationResult/ValidationError) + TalkImporter (+ ImportResult) + tests, 22 new tests (15 validator, 7 importer), all green.
- M1.4 FileTalkRepository + TalkSummary + ITalkRepository + tests, 7 new tests, all green. Done ahead of its listed order because TalkImporter (task 3) needs ITalkRepository to compile/persist — implemented Storage first, then Import, in that order; both are captured together in one commit.

## In progress
- (none)

## Next
- M1.5 LiveTalkEngine + PaceCalculator + tests

## Blockers / decisions needed
- (none)

## Deviations from plan.md
- Added a repo-root `NuGet.Config` pinning the package source to nuget.org only. The dev machine's global NuGet config has a second private feed ("Rey Technology") registered; with Central Package Management enabled, `dotnet restore` fails outright (NU1507, source mapping required as an error) unless sources are unambiguous. Repo-local config is the least invasive fix — doesn't touch global machine config, doesn't affect other projects.
- `global.json` was generated automatically by the `xunit3` template (sets `test.runner: Microsoft.Testing.Platform`). Kept it — it's required for `dotnet test` to use the new Microsoft.Testing.Platform runner that xunit.v3.mtp-v2 uses.
- Test project template defaults to net8.0 — retargeted to net10.0 per plan §2/§3.6.
- `dotnet test` on the still-empty test project exits with code 8 ("zero tests ran") — expected, not a failure; first real tests land in M1.2.
- App/MacHelper heads scaffolded via the `maui-reactor-startup` template (from an already-installed Reactor.Maui.TemplatePack), then trimmed: removed Android/Tizen/Windows/(cross) platform folders and TFMs, kept only net10.0-ios (App) / net10.0-maccatalyst (MacHelper), set bundle IDs `com.mallibone.stagebuddy` / `com.mallibone.stagebuddy.helper` per plan §2. Placeholder `HomePage.cs` ("Hello World" counter) left in place as the stub root component — AppShell/real pages land in M1.6-M1.8.
- `TalkJsonContext` currently only registers `TalkDocument` (plan §3.2 shows the eventual state including `SyncEnvelope`/`TalkRunLog`, which don't exist as types yet — M2/M3 will add their `[JsonSerializable]` entries when those records land).
- `JsonStringEnumConverter<BeatType>` alone serializes enum members using their C# name ("Talk"/"Demo"), not the source-gen `PropertyNamingPolicy` (that policy only governs property names, not enum converters). Added a tiny `BeatTypeJsonConverter : JsonStringEnumConverter<BeatType>(JsonNamingPolicy.CamelCase)` subclass instead, since a `Type[]` converter list can't pass constructor args inline.
- Added `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` to `TalkJsonContext` — without it, re-serializing an object with absent optional fields (e.g. a talk beat's null `Steps`) writes explicit `null` properties that don't exist in hand-authored input JSON, breaking structural round-trip equality. Not explicitly called out in plan.md but required for the round-trip test it prescribes.
- `TalkImporter.ImportFromJson` → implemented as `ImportFromJsonAsync(string json, CancellationToken ct = default)`. Plan's signature omits Async/CancellationToken, but the method awaits `ITalkRepository.SaveAsync`, so it must be a `Task`-returning method; named per C# async convention and threaded a `CancellationToken` through for consistency with the repository interface.
- `TalkId` derivation (slug + 8-char content hash) hashes the raw input JSON string (SHA-256, first 8 hex chars), not a canonicalized/re-serialized form — "same content" is interpreted literally (same pasted bytes re-imports to the same id); a reformatted-but-semantically-identical JSON paste would get a new id. Not spelled out in plan.md; flagging in case that's not the intended semantics.
- xUnit v3 analyzer (`xUnit1051`) requires every test that calls an async method with a `CancellationToken` parameter to pass `TestContext.Current.CancellationToken` (enforced as an error via `TreatWarningsAsErrors`). Adopted a `private CancellationToken Ct => TestContext.Current.CancellationToken;` convenience property in test classes that touch `ITalkRepository`/`TalkImporter` — keep using this pattern in later test classes that call cancellable Core APIs.
