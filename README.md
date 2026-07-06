# Stage Buddy

Your on-stage companion for live-coding talks. Stage Buddy runs on an iPad (or iPhone) beside you while you present: it shows where you are in the talk, whether you're on pace, and — the whole point — puts each demo snippet on your **Mac's pasteboard with one tap**, ready to ⌘V into a live editor. No cloud, no Wi-Fi required: the devices talk directly over [MultipeerConnectivity](https://developer.apple.com/documentation/multipeerconnectivity) (AWDL), encrypted, even with no network joined.

Live demos fail in predictable ways. Stage Buddy is built around that:

- **One tap → exact paste.** Multi-line, tabs, unicode — the snippet lands byte-for-byte on the Mac pasteboard (`.Reliable` delivery; verified down to the pasteboard change count).
- **Pacing at a glance.** Beats (talk sections and demos) carry planned durations; a pacing bar shows on-track / tolerance / behind against the timeline, and finishing a talk records actual-vs-planned per beat.
- **Placeholders for talk day.** Snippets can contain `{{placeholders}}`; a sheet asks for values before the first copy (`{{date}}` prefills automatically). No more editing demos in the speaker room.
- **Fallbacks when things break.** Steps can carry a fallback note for when the live path dies, and if the Mac link is down the snippet renders as a **QR code** you can scan instead.
- **Rehearsal mode.** Run the full talk with no Mac connected, clearly badged, with the same pacing and summary.
- **A helper that stays awake.** The Mac helper suppresses App Nap while connected and shows what was last received; pairing is confirm-on-first-contact, then silent reconnect via a stored token. Re-pair anytime from Settings.

## How it fits together

| Project | What it is |
|---|---|
| `src/StageBuddy.Core` | Platform-free logic: talk model, validation, import, live-run engine + pace math, placeholders, run log. Fully unit-tested. |
| `src/StageBuddy.Sync.Multipeer` | MultipeerConnectivity layer: pairing state machine, peer-ID persistence, reliable snippet send/receive. |
| `src/StageBuddy.App` | The iOS/iPadOS app (.NET MAUI + [MauiReactor](https://github.com/adospace/reactorui-maui) MVU): library, import, live view, summary, settings. |
| `src/StageBuddy.MacHelper` | Mac Catalyst helper: receives snippets, writes the pasteboard, shows connection state. |
| `tests/` | Core unit tests, plus an opt-in end-to-end suite that drives the real app on the iOS simulator. |

## Talks are just JSON

A talk is a timeline of beats; demo beats contain steps, and each step's snippet is what gets pasted. Import by pasting JSON into the app.

```json
{
  "session": { "title": "Building MCP Servers in .NET", "plannedDurationMinutes": 45 },
  "timeline": [
    { "id": "beat-intro", "type": "talk", "title": "Intro & motivation", "plannedDurationMinutes": 5 },
    {
      "id": "beat-first-server", "type": "demo", "title": "First MCP server", "plannedDurationMinutes": 8,
      "steps": [
        {
          "id": "step-add-package",
          "note": "Show the NuGet package, empty console app",
          "snippet": "dotnet add package ModelContextProtocol",
          "language": "bash",
          "fallbackNote": "If live setup breaks, show the pre-built project"
        }
      ]
    }
  ]
}
```

Rules the importer enforces: ids globally unique (beats and steps share one namespace), `talk` beats have no steps, `demo` beats have at least one, notes non-empty, and beat durations should sum to the session duration (the pacing bar uses the timeline sum). See `tests/DeviceTestData/m2-checklist.json` for a feature-complete example.

## Building

Requirements: macOS with Xcode, .NET 10 SDK with the MAUI workload.

```bash
dotnet build StageBuddy.slnx                                  # everything
dotnet test tests/StageBuddy.Core.Tests                       # core logic tests
dotnet build src/StageBuddy.App -f net10.0-ios                # iOS app
dotnet build src/StageBuddy.MacHelper -f net10.0-maccatalyst  # Mac helper
```

Run the helper by opening the built `.app` bundle; run the iOS app on a simulator or deploy to a device with `xcrun devicectl device install app` + `process launch` after a signed build (`-p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Development"`). MultipeerConnectivity needs **real hardware** on the iOS side — simulators have no AWDL.

The end-to-end suite drives the actual app on the iOS simulator (import flows, a full live run, the QR fallback path) via the MAUI DevFlow agent and is opt-in:

```bash
STAGEBUDDY_E2E=1 dotnet test tests/StageBuddy.App.EndToEndTests
```

## Provenance

This is a personal project, built almost entirely with [Claude Code](https://claude.com/claude-code) — including its own test tooling and the design pass. The development process is part of the repo: `stage-buddy-spec.md` (what to build), `plan.md` (how to build it), and `progress.md` (what actually happened, including the bugs the e2e suite caught) are the working artifacts, kept honest commit by commit.

Icons are from [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) (MIT).

## License

[LGPL-3.0](LICENSE): use it however you like, including inside closed-source software — but this code, and any modifications you make to it, stay open under the same license.
