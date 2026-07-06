---
name: prep-talk
description: Turn a talk outline, abstract, or rough notes into a valid StageBuddy talk JSON (the demo script the iOS app imports). Use when the user is prepping a talk, wants "the json for the demo script", asks to create/convert/update a StageBuddy talk, or hands over an agenda/outline and wants it stage-ready. Also use to add demo steps or snippets to an existing talk file.
---

# Prep a StageBuddy talk

Produce a talk document the app's importer accepts on first paste. The schema and rules below mirror `src/StageBuddy.Core` (`Models/`, `Validation/TalkValidator.cs`, `Placeholders/PlaceholderEngine.cs`) — if this file and the code disagree, the code wins; check it and update this skill.

## Input

Whatever the user has: an abstract, a bullet outline, session title + length, an existing deck. If the total duration or the demo snippets are missing, ask for them — snippets are the payload (they land verbatim on the Mac pasteboard when the speaker taps Copy); never invent code the user didn't approve.

## Schema (all field names camelCase)

```json
{
  "session": {
    "title": "string, non-empty",
    "plannedDurationMinutes": 45
  },
  "timeline": [
    {
      "id": "beat-intro",
      "type": "talk",
      "title": "Intro & motivation",
      "plannedDurationMinutes": 5
    },
    {
      "id": "beat-first-demo",
      "type": "demo",
      "title": "First MCP server",
      "plannedDurationMinutes": 8,
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

- `type` is `"talk"` or `"demo"` (lowercase in JSON).
- Per step, only `id` and `note` are required; `snippet`, `language`, `fallbackNote` are optional. `language` is a free-form badge on the code card (`csharp`, `bash`, `json`, `text`, …).
- Omit fields rather than writing `null` (round-trip serialization ignores nulls).

## Validation rules (importer rejects on any error)

1. `session.title` non-empty; `session.plannedDurationMinutes` > 0.
2. `timeline` has at least one beat.
3. **Ids are globally unique across beats AND steps combined** (one shared namespace), and non-empty. Use readable slugs: `beat-intro`, `step-add-package`.
4. `talk` beats must NOT have a `steps` array with entries. `demo` beats MUST have at least one step.
5. Every step `note` is non-empty. A `snippet`, if present, must be non-empty — omit the key instead.
6. Beat durations must sum to `session.plannedDurationMinutes`. (Only a warning to the validator, but the pacing bar uses the timeline sum as its denominator — always make them match; adjust the session total rather than padding fake beats.)

## Placeholders in snippets

`{{name}}` — word characters only (`\w+`; no spaces or hyphens). Before the first copy of a run, the app shows a sheet asking for values. `{{date}}` is built in and prefills today as `yyyy-MM-dd`. Use placeholders for anything decided on talk day: URLs, API keys, room-specific values. Unknown placeholders are left intact in the pasted text, so don't use `{{...}}` decoratively.

## Authoring guidance

- Snippets paste exactly as written: real newlines (`\n`) and tabs (`\t`) are preserved end-to-end. Write them paste-ready — the speaker pastes into a live editor, mid-talk.
- Split demos into steps at each point the speaker would switch to the editor and paste. One paste = one step. `note` is the on-stage cue ("what am I doing now"), not documentation.
- Add a `fallbackNote` to any step that can fail live (network, provisioning, login) — it's shown when the Mac link is down.
- Order of magnitude: beats of 3–10 minutes each; a step count a human can tap through.

## Output & delivery

Write the JSON to a file named after the talk (kebab-case, e.g. `mcp-servers-dotnet.json`) wherever the user keeps talk material (ask if unclear). Then check it against every rule above — especially id uniqueness and the duration sum — before handing it over. Offer both delivery paths:

1. **Paste import (normal path):** open Stage Buddy on the device → Import → paste the JSON. The talk id is derived from a hash of the raw pasted string: identical bytes re-import as the same talk, any reformat creates a new one.
2. **Direct seed (dev iPad over USB):**
   ```bash
   xcrun devicectl device copy to --device <udid> \
     --domain-type appDataContainer --domain-identifier com.mallibone.stagebuddy \
     --source <talk>.json --destination Library/talks/<talk>.json
   ```
   The library reloads when the page appears; the talk id is the filename.

A worked example with every feature exercised: `tests/DeviceTestData/m2-checklist.json`.
