# Narrative interludes — 인트로 · 스테이지간 컷씬 · 내레이션 씬

- run-id: 20260809-castle-war-stage1
- owner: design + engineering
- status: implemented + EditMode-pinned 2026-08-10

## 1. The defect

The campaign cut from a results screen **straight into the next battlefield**.
Three deliberately distinct stages — Siege Plains, Desolate Dunes, Volcanic
Abyss — therefore read as "the same fight again with different rocks". Nothing
told the player they had *travelled*, and nothing explained why the new board
plays differently.

Two related gaps:

- The 11-page webtoon prologue existed but was **unreachable as a cold open**.
  `GameManager.webtoonIntroShown` was assigned and never read — dead code. A
  first-time player arrived at a title card with no framing at all.
- Clearing the final stage dropped straight to a scoreboard. The campaign had
  no ending, only a last scoreboard.

## 2. Design

Three narration beats, one surface, one script model.

| Beat | When | Content |
|---|---|---|
| **Opening** | once per app session, before the title card | 3 lines: where we are, what the sling does, one line of character voice |
| **Stage entry** | only when the campaign *moves* to a different stage | names the place, states what this battlefield changes **in play terms**, closes on character voice |
| **Epilogue** | series won on the final stage, before the scoreboard | 3 lines closing the campaign |

The stage-entry scripts are deliberately **framing and tutorial at once**. Stage 2
says the range is shorter and walls are higher; Stage 3 says the gorge is wide and
the wind is worse. That is the same information the balance sheet encodes, said
in the fiction's voice at the moment it becomes relevant.

**The long form stays optional.** The 11-page webtoon remains behind the title's
프롤로그 button. Putting it in front of a new player was the original "long read
before the game" mistake; the cold open is the short form — three lines, skippable.

**A rematch never gets a cutscene.** `ShouldPlayOnEntry(entering, previous,
advancedFromClear)` requires both an actual stage change *and* a clear. Putting a
cutscene between a player and their retry is how a good scene becomes a hated one.

## 3. Split: script model vs surface

- `Assets/Scripts/StageInterlude.cs` — **pure**. Scripts, timeline arithmetic
  (`LineIndexAt`, `TimeInLine`, `TotalDurationSeconds`, `IsComplete`), and the
  `ShouldPlayOnEntry` gate. No engine state, so EditMode pins both the campaign's
  narrative coverage and its pacing.
- `Assets/Scripts/StageInterludeController.cs` — the runtime surface. Backdrop
  panel (reusing the generated `Resources/Webtoon/` art), heading, speaker line,
  and body text revealed through the existing `NarrativeTypewriter`.

Built at runtime, so `SampleScene` stays untouched — same precedent as the
webtoon prologue and the results screen.

**Runs on `Time.unscaledTime`.** Every caller freezes the board (`timeScale = 0`)
while a cutscene plays; a scaled clock would hang forever.

## 4. Pacing

```yaml
interlude:
  pre_roll_seconds: 0.7      # black hold, so the cut never lands as a jump
  line_hold_seconds: 1.9     # after a line finishes typing
  line_fade_seconds: 0.35
  type_characters_per_second: 30
  tail_seconds: 0.6          # fade to black before handing control back
```

A line costs `len/30 + 1.9 + 0.35` seconds, so longer prose genuinely holds
longer instead of every beat sharing one fixed duration.

## 5. Input

- **Click / Space / Enter** — first press completes the current line's reveal,
  second press advances. The standard VN contract: a fast reader is never held
  hostage by the typewriter.
- **Esc** — skips the whole scene.

## 6. Wiring

| Site | Behaviour |
|---|---|
| `GameManager.ShowIntro` | plays the cold open once per session (now the real use of `webtoonIntroShown`), then the title |
| `GameManager.ShowIntro` (skipIntro path) | plays the stage-entry beat with the board built and frozen behind it, then `StartGame` |
| `GameManager.RequestStage` | arms `pendingStageInterlude` via `ShouldPlayOnEntry`; static, so it survives the scene reload the advance triggers |
| `GameManager.RequestTitle` | clears the armed flag — abandoning the advance must not fire its cutscene on the title's boot |
| `GameManager.EndGame` | plays the epilogue before the scoreboard when the series is won **and** no stage remains |

The epilogue condition is deliberately not `nextStage == null`: a mid-campaign
defeat also has no next stage. It must be a series win with nothing after it.

## 7. A latent NRE this surfaced

`InterludeScript` is a `readonly struct`. A `default(InterludeScript)` **skips the
constructor**, so the constructor's `lines ?? Empty` never ran and the field was
null — `HasContent` would have thrown at `StageInterludeController.Play`'s very
first statement. The beats are now a private field read through a null-safe
`public InterludeLine[] Lines => lines ?? Empty`, and an EditMode test fails if
that accessor is ever reverted. Caught by the test lane during authoring, before
it could ship.

## 8. Verification [OBSERVED 2026-08-10]

- **316/316 EditMode tests pass** (`editmode-results.xml`), up from 300.
- 16 new pins in `Assets/Tests/EditMode/StageInterludeTests.cs`: every stage has a
  real script, the three scripts are genuinely distinct (heading *and* body),
  narration/speaker are inverse properties, the timeline is monotonic and visits
  **every** line (a line no elapsed value selects is a line the player never
  sees), duration accounting matches the summed lines rather than a literal,
  completion is a closed interval, the entry gate covers all four
  movement/clear combinations, a default script is throw-free, and the typewriter
  contract not already covered by `MobileNarrativeCommerceTests`.
