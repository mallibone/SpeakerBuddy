# Stage Buddy — Technical Specification

## 1. Overview

Stage Buddy is an iOS/iPadOS companion app for delivering live conference talks. It tracks pacing through a talk via a story-arc timeline and steps through demo notes/snippets, with instant peer-to-peer copy-to-Mac for pasting code during live demos.

**Primary user**: a single speaker (developer/tech lead) presenting on a Mac while holding an iPhone or iPad mini as a companion device on the podium.

## 2. Platform & Stack

- **.NET 10**
- **.NET MAUI**
- **MauiReactor** (functional/component-based MVU UI framework over MAUI — no XAML)
- **Targets**:
  - iOS / iPadOS — the Stage Buddy app (used live, on stage)
  - Mac Catalyst — a small Mac helper app (receives pasted snippets)
  - Same solution, shared project structure
- **Data**: fully local — no cloud sync, no backend service, no external API dependency at runtime
- **MultipeerConnectivity**: not a MAUI Essentials API; accessed directly via the existing `Microsoft.iOS` / `Microsoft.MacCatalyst` bindings (Apple's MultipeerConnectivity namespace is already exposed — no custom native binding project required). Implement via platform-conditional C# (`#if IOS || MACCATALYST` or per-platform partial classes), since the API surface used is identical on both targets.
- Git: Use Git and a sensible .gitignore for .net projects, commit often and make commit messages like a seasoned github developer would do

## 3. Core Data Model

Talks are authored externally (Markdown → JSON via AI), then imported into the app. JSON is transferred once via standard clipboard copy/paste (Mac → iOS), not via the live P2P sync channel.

```json
{
  "session": {
    "title": "Building MCP Servers in .NET",
    "plannedDurationMinutes": 45
  },
  "timeline": [
    {
      "id": "beat-1",
      "type": "talk",
      "title": "Intro & motivation",
      "plannedDurationMinutes": 5
    },
    {
      "id": "beat-2",
      "type": "demo",
      "title": "First MCP server",
      "plannedDurationMinutes": 8,
      "steps": [
        {
          "id": "step-1",
          "note": "Show the ModelContextProtocol NuGet package, empty console app",
          "snippet": "dotnet add package ModelContextProtocol",
          "language": "bash",
          "fallbackNote": "If live setup breaks, show the pre-built project instead"
        },
        {
          "id": "step-2",
          "note": "Register the server + stdio transport",
          "snippet": "builder.Services.AddMcpServer()\n    .WithStdioServerTransport();",
          "language": "csharp"
        }
      ]
    }
  ]
}
```

**Notes:**
- `timeline` is a single ordered array mixing `type: "talk"` and `type: "demo"` beats — this drives one unified progress bar across the whole talk, not separate demo/talk tracking.
- `talk` beats have no `steps`; advanced manually with a "Next" tap.
- `demo` beats step through their `steps` array; each Copy action auto-advances.
- `fallbackNote` (optional, per step): what to say/show if the live demo breaks.
- Snippets support placeholder syntax (`{{env}}`, `{{date}}`, etc.), resolved via a quick-edit sheet before copying — allows one JSON file to be reused across multiple events without editing on stage.

## 4. Import Flow

1. Speaker authors talk content in Markdown.
2. Converts Markdown → JSON matching the schema above (via AI, external to the app).
3. Copies JSON to clipboard on Mac.
4. Pastes into Stage Buddy's import screen on iOS (standard Universal Clipboard — acceptable here since it's a one-time transfer, not time-critical).
5. App validates JSON against schema and saves it to the local talk library (flat file or SQLite via MAUI local storage).

## 5. Live View (Core Screen)

- **Start**: manual button tap starts the timer. Timer does not run before this.
- **Progress bar**: single bar across the whole talk, colored by pace:
  - Green: on pace or ahead (cumulative actual ≤ cumulative planned)
  - Amber: within tolerance (e.g. ±1 minute)
  - Red: behind pace
- **Talk beats**: show title + planned duration; advance via manual "Next beat" tap.
- **Demo beats**: show current step's note + code snippet + large **Copy** button.
  - Copy → sends snippet via MultipeerConnectivity to the paired Mac → auto-advances to the next step.
  - **Back** is always available, in case of misclick or need to re-show a prior snippet.
- Elapsed time / remaining time always visible alongside the progress bar.

## 6. Mac ↔ iOS Sync (Live Copy-to-Paste)

**Primary mechanism: MultipeerConnectivity**
- Ad-hoc peer-to-peer link: Bluetooth for discovery, peer-to-peer Wi-Fi for actual data transfer.
- Fully independent of venue Wi-Fi/network infrastructure — same underlying tech as AirDrop.
- **Mac helper app** (Mac Catalyst): `MCNearbyServiceAdvertiser` advertises a custom service type; on receiving data, writes directly to `NSPasteboard.general`.
- **iOS app**: `MCNearbyServiceBrowser` discovers the Mac, connects via `MCSession`; Copy tap → `session.send(data, toPeers:, with: .reliable)`.
- Pairing should be near-instant on app launch (proximity-based, no network handshake).

**Fallback: QR code**
- If Bluetooth/MultipeerConnectivity is unavailable, display a QR code of the snippet.
- Speaker scans with phone camera → routes to Mac clipboard via Shortcuts.
- This is the only fallback needed; Bonjour/HTTP-over-LAN and personal-hotspot approaches were considered and explicitly dropped in favor of MultipeerConnectivity's zero-infrastructure model.

## 7. Additional Features

- **Talk library**: list of imported talks; select and run any of them. Supports **rehearsal mode** — runs the timer/progress standalone without requiring a paired Mac, for practicing pacing beforehand.
- **Post-talk log**: on finishing a talk, log actual vs. planned duration per beat to local storage, to calibrate future time estimates.
- **Settings screen**: Mac helper pairing status, re-pair action.

## 8. Explicitly Out of Scope (v1)

- Cloud sync or multi-device state beyond a single Mac + single iOS/iPadOS device pairing
- Android and Windows targets
- In-app editing of talk content (content is authored externally via Markdown → JSON, then imported)

## 9. Suggested Build Order

1. **M1 — Local core**: talk library, JSON import/validation, live view with timeline/timer/progress bar (no Mac sync yet; Copy just copies to local iOS clipboard)
2. **M2 — MultipeerConnectivity sync**: Mac Catalyst helper app, pairing, live P2P snippet transfer
3. **M3 — Resilience & polish**: QR fallback, placeholder substitution, fallback notes, rehearsal mode, post-talk log
