# Progress — updated 2026-07-03

## Done
- M1.1 scaffold: git init (branch main), .gitignore, StageBuddy.slnx, Directory.Build.props, Directory.Packages.props (CPM), NuGet.Config, all 5 projects created + referenced correctly, full solution builds with 0 warnings/errors, iOS and MacCatalyst per-TFM builds verified.

## In progress
- (none)

## Next
- M1.2 Core models + TalkJsonContext + TalkSerializationTests

## Blockers / decisions needed
- (none)

## Deviations from plan.md
- Added a repo-root `NuGet.Config` pinning the package source to nuget.org only. The dev machine's global NuGet config has a second private feed ("Rey Technology") registered; with Central Package Management enabled, `dotnet restore` fails outright (NU1507, source mapping required as an error) unless sources are unambiguous. Repo-local config is the least invasive fix — doesn't touch global machine config, doesn't affect other projects.
- `global.json` was generated automatically by the `xunit3` template (sets `test.runner: Microsoft.Testing.Platform`). Kept it — it's required for `dotnet test` to use the new Microsoft.Testing.Platform runner that xunit.v3.mtp-v2 uses.
- Test project template defaults to net8.0 — retargeted to net10.0 per plan §2/§3.6.
- `dotnet test` on the still-empty test project exits with code 8 ("zero tests ran") — expected, not a failure; first real tests land in M1.2.
- App/MacHelper heads scaffolded via the `maui-reactor-startup` template (from an already-installed Reactor.Maui.TemplatePack), then trimmed: removed Android/Tizen/Windows/(cross) platform folders and TFMs, kept only net10.0-ios (App) / net10.0-maccatalyst (MacHelper), set bundle IDs `com.mallibone.stagebuddy` / `com.mallibone.stagebuddy.helper` per plan §2. Placeholder `HomePage.cs` ("Hello World" counter) left in place as the stub root component — AppShell/real pages land in M1.6-M1.8.
