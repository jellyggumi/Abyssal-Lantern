# castle-war — repository operating rules

Repository-scoped instruction set for agent sessions in this project. Apply it
together with the code, the tests, and the live run artifacts under
`_workspace/current/`. `AGENTS.md` points here; there is exactly one contract.

> **Identity**: This repository is **castle-war** (formerly `Abyssal-Lantern`,
> product previously `unknown-castle` / "Castle Busters"). It is a Unity
> 6000.5.6f1 2D physics siege game being developed into a **faction-vs-faction
> castle war** (reference genre: *Archery Bastions: Castle War*). The remote
> rename `Abyssal-Lantern → castle-war` requires the repo owner (`jellyggumi`)
> — until it lands, `origin` keeps the old URL; do not re-point it silently.

---

## 1. Workspace: one live folder, one archive

**`_workspace/current/` is the only folder any session writes run artifacts
to.** There are no dated run folders. A new production cycle does not create a
sibling directory — it updates `current/` in place and records the transition
in `current/production/task-manifest.md`.

```
_workspace/
  current/                 <- the single up-to-date working folder
    intake/ design/ engineering/ pm/ qa/ ops/ ui/
    production/task-manifest.md
    messages/ retrospectives/
  archive/<run-id>/        <- frozen prior cycles, READ-ONLY
```

Rules:

- Write only under `_workspace/current/`. Treat `_workspace/archive/**` as
  immutable history: read it for evidence, never edit or delete it.
- Archiving is the only way material leaves `current/`. At cycle close, move
  superseded lane material to `_workspace/archive/<run-id>/` with `git mv` and
  write `current/retrospectives/cycle-<n>-retrospective.md`. Never delete a
  `_workspace/` artifact to make a gate or a summary look cleaner — the
  workspace is the studio's memory across sessions; a deleted artifact is a
  measurement the next cycle cannot cite.
- Mark statements `[OBSERVED]`, `[INFERENCE]`, or `[TARGET]` whenever the
  status could be confused. Never present a target or an inherited baseline as
  a new measurement.
- Cite exact repository-relative paths as evidence. A claim is not established
  by a file existing; cite the measurement, command, or test result behind it.

## 2. Engine perspective: Unity 6000.5.6f1 (2D URP, C#)

This is a **Unity 2D physics siege game** (entry: `Assets/Scenes/SampleScene`,
code under `Assets/Scripts/`, tests under `Assets/Tests/`). Editor version is
pinned by `ProjectSettings/ProjectVersion.txt` — always launch with
`/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity`.

- **Never apply the archived web-game guidance here.** The git history (and
  `_workspace/archive/`) contains the predecessor *Abyssal* HTML/Three.js web
  game; its files (`app.js`, `battle-*.js`, `defense-*.js`, …) are deleted from
  the working tree. Resurrecting them or following their patterns is wrong-engine
  work, not a translation exercise.
- Unity control paths, in order of preference:
  1. **Unity MCP** (`com.ivanmurzak.unity.mcp` 0.87.0 + animation/cinemachine/
     inputsystem/particlesystem/… extensions, already in `Packages/manifest.json`)
     for scene/asset/prefab manipulation while the editor is open.
  2. **Unity CLI batch mode** for tests and builds (see §5, §6).
- Lifecycle order: prototype → systems → content → assets → feel → perf → QA →
  release. Never build VFX/audio polish before the system it communicates is
  defined. Never ship before test-playable proof exists.
- Presentation code (camera, VFX, audio) may read simulation state but must
  never write back into it — deterministic simulation keeps the balance sims
  and the 41-test EditMode suite meaningful.
- UX first: every interaction must be readable without text — launch affordance,
  turn state, core HP, and win condition stay visible on screen at all times.
  A feature that needs a tooltip to be understood re-enters design.

## 3. Asset generation: fixed tool per asset class

Do not improvise a generator. Each class has exactly one owner:

| Asset class | Tool | Invocation |
|---|---|---|
| 2D sprites / key art / UI art | **Codex CLI** | `codex exec "<prompt>"` (or the `codex:codex-rescue` agent) — output lands in `_workspace/current/design/concept/` first |
| BGM | **Gemini via playwriter** | `playwriter` skill driving the user's open Chrome at `https://gemini.google.com/app`; result files go to `Assets/Resources/Audio/` after audit |
| SFX | rfxgen / game-sounds skill | procedural, checked into `Assets/Resources/Audio/SFX/` |

- Every generated asset gets an adjacent `.provenance.json` recording prompt,
  tool, model, and checksum. Generated output starts in the concept lane
  (`design/concept/`) and is promoted into `Assets/` only after an explicit
  audit — un-audited AI output in a shipped build is a licensing and quality
  liability the audit exists to catch.
- Prove style with a single asset before generating a full set.

## 4. Documentation: llm-wiki, organized per project

This repo doubles as an llm-wiki vault (`index.md`, `log.md`, `wiki/`, `raw/`).

- **Project-unit organization**: durable findings about this game file under
  `wiki/projects/castle-war/` (design decisions, balance reports, release
  notes). Legacy `wiki/reports/castle-busters-*.md` stay where they are as
  history; new pages use the castle-war project folder.
- `raw/` is immutable source capture; `wiki/` pages are LLM-written synthesis;
  `index.md` is the entry point and must link every new project page.
- Update the wiki at every cycle close (same moment as the retrospective), not
  ad hoc — a wiki updated mid-measurement records guesses as facts.
- One-off session scratch (probe scripts, logs) is not wiki material and is
  gitignored at the repo root; durable tooling lives in `scripts/` or `tools/`.

## 5. Verification and reporting

- Full regression uses this command exactly:
  ```
  "/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity" \
    -batchmode -projectPath . -runTests -testPlatform EditMode \
    -testResults ./editmode-results.xml -logFile ./editmode-test.log
  ```
  (PlayMode: same with `-testPlatform PlayMode`. The editor must be closed
  first — batch mode and an open editor fight over the project lock.)
- Report what was actually checked: the exact command or artifact path and the
  observed result. Distinguish carried evidence, new evidence, unresolved
  blockers, and human-only judgments.
- Numbers gate everything (G1–G8, `skill://game-studio-harness`). No adjective
  passes a gate.
- **Read every file the triage names before starting.** A survey lane computed a
  win-rate cliff from a simulator assumption and recommended intervention, while
  the measured value sat in a file the triage had named by path
  (`qa/b1-measurement-findings.md`). The assumption was off by 8–18×, and correcting
  it reversed the recommendation. The lane self-reported the miss; the rule exists so
  the next one does not repeat it.
- **Verify an id before quoting what it returned.** A lane pulled Steam reviews for
  eight app ids and found three misattributed — 219150 is Hotline Miami, not Worms
  Clan Wars. Caught before writing, so the corpus is clean. Unverified, Hotline
  Miami reviews would have been quoted as artillery players' voices.
- **A floor-type requirement is only valid where its target does not compete for
  size with something else.** A minimum-pixel rule works for HUD canvas text: the
  HUD does not contend with world geometry. It breaks for a world-space label
  attached to an object — raising a label on a 1.15u body to 12px at worst zoom
  makes the label 0.87–1.15× the body, so the annotation becomes the subject
  (measured 1.73× contradiction, cycle 2: floor needed fontSize 9.96, the
  subordinate-annotation ceiling allowed 5.75). When a floor cannot be met, the
  answer is neither "waive it" nor "reclassify as not applicable" — the first
  hides a measured defect, the second erases a user-reported one. Narrow the
  floor to the case where that channel alone carries the information, and move
  the rest to a channel that is not size-bound. Check the competition condition
  before writing any new floor.
- **Cite an accessibility rate with its unit.** A flash rate measured as an
  instantaneous derivative and one measured as WCAG's worst one-second window
  are different numbers for the same curve (11.50/s vs 9.00/s, cycle 2). Quoting
  one without the unit propagated a wrong figure into shipped code comments.
  Report both, and judge on the window; at a clamp or `min()` kink use one-sided
  limits, never a central difference, and take the worse side — smoothing a kink
  biases structurally toward the safe-looking answer.
- **PlayMode probes: `-nographics`, and run them TWICE.** The domain-reload hang is
  the MCP plugin's `OnBeforeAssemblyReload` → `DisposeLogCollector` →
  `BufferedFileLogStorage.cs:52`. It fires when the run has to reload assemblies, so
  the reliable pattern is: run once (it may hang), change no scripts, run again — the
  second pass has nothing to recompile and completes. Cycle 2 lost five consecutive
  attempts to this before the pattern was found; the sixth finished in 128s.
  Symptom of the bad case: the log stops with no scene markers, which means the run
  never reached the scene and any "zero errors" reading from it proves nothing.
  `-nographics` also avoids it in most cases, but `cam.Render()` segfaults without a
  graphics device, so a probe that writes PNGs must run WITH graphics and take the
  hang risk.

### Invariants earned the hard way (violating these has already cost us)

- **A test must measure the quantity the code produces.** A blink clamp was
  applied to a phase *rate* while the code produced the derivative of
  `t × rate`; the test asserted the rate, passed, and certified a WCAG violation
  as safe. A test that certifies safety it never measured is worse than no test.
  When a value is derived, assert the derived thing — count the real output.
- **A derived figure inherits every defect of the run it came from.** B1 measured
  Stage3's damage per turn at 5.31 and concluded d "cannot be one number, it spans
  24x". The stage had no walls and no core — a ground-atlas exception was aborting
  its boot — so the figure described an empty board, and re-measuring gave 128.00
  against a true spread of 1.33x. Before a measurement becomes a conclusion, check
  that the thing being measured was actually there.
- **Never rank levers the arithmetic cannot rank.** A test asserted that fixing
  accuracy beats fixing damage; it failed at 110.7 versus 111.1, because both enter
  the product multiplicatively so equal relative gains are equal. Wanting one lever
  is a design position — state it as one, and let the test assert only what the
  numbers force.
- **A test can pass ONLY in a bug's presence.** A ceiling test asserted that the
  widest handicap reaches its cap. It did reach it — and its reaching it WAS the
  defect, because two grades clamped to the same value and a four-grade scale
  behaved as three. Eleven tests were green while shipping that. "The ceiling
  works" and "the scale is a scale" are different contracts, and only the first
  existed. When a clamp, cap, or floor is asserted, also assert that the values it
  bounds stay distinguishable.
- **A simulator constant that no gameplay code reads is a measurement parameter,
  not a knob.** `beginnerAimError` was recommended as a one-constant fix on the
  belief that "the knob already exists". It is read only by the two sims, the
  editor, and tests; the player's launch path contains zero random draws (the AI's
  contains four), so that constant models a HUMAN's hand. Raising it moves what the
  gate reports and nothing a player experiences. Before writing "the knob already
  exists", grep for reads from gameplay code.
- **Fixing something invalidates decisions that cited it — go back and check.** The
  opening-volley damping landed at 22:51 and erased a 38%p first-turn gap. The
  arbitration that blocked all seven gimmicks on that gap was last touched at 13:11,
  nine hours EARLIER, so nobody cited a stale number: the fix simply arrived
  afterwards and no one revisited what it invalidated. Three documents kept citing
  87% for over a day. A fix is not finished until the decisions that depended on the
  old state have been re-read.
- **When a device changes damage or accuracy, say so on screen.** Every shipped
  comparable announces such rules — Hedgewars gives Karma / Vampirism / Extra
  Damage their own permanent HUD icons, Worms marks a handicapped roster with `+`/`-`,
  ShellShock added a wind icon to the server LIST. This project had two invisible
  ones. A player who is not told why a shot hurt less reads it as a miss.
- **The presentation/simulation boundary (§2) is load-bearing, and it has now paid
  twice.** Sim-side balance figures stayed measurable across a large presentation
  change because not one simulation symbol moved — so a baseline nobody thought to
  capture was still recoverable at HEAD. And a buff that wrote `sr.color` directly
  rendered for *zero frames*, because the animator owns that channel and reasserts
  it every frame. Those are the same rule seen from both sides: the boundary is why
  the measurement survived, and the boundary is why the shortcut failed. Treat a
  presentation change that needs to write simulation state as a design error, not a
  plumbing problem.
- **Presentation that reports a count must be bound to the event, not to the
  state.** A 0.625s attack clip looped for a 1.5s cooldown showed 2.40 swings per
  one damage event. Players count swings; a looping clip lies about the number.
- **One owner per render channel.** `sr.color` was written by buffs, by expiry
  blinks, and by the animator's per-frame team tint — so buff colour rendered for
  zero frames. Anything wanting to tint a unit goes through the animator, which
  composes flash over status over team.
- **Greyscale art times a neutral tint is invisible on bright ground.** Art here
  is authored greyscale and code multiplies colour in, so `Color.white` leaves the
  result colourless — measured as the "white square" report. Tints on effect art
  need saturation (pinned at ≥0.35).
- **Ask the scene, not the source, when pixels are the question.** Two wrong
  diagnoses in cycle 2 came from reading code about a frame nobody had captured.
  Enumerate the renderers at the position, then switch the suspect off and measure
  the difference.

## 6. Web build & deployment

- Target: **WebGL build** published so the game runs from the menu page of
  `https://jellyggumi.github.io/` (repo `jellyggumi/jellyggumi.github.io`,
  push access confirmed).
- Build via CLI: `-batchmode -buildTarget WebGL -executeMethod` a build script
  under `Assets/Editor/`; output to `Builds/WebGL/` (gitignored here), then
  copy into the pages repo (e.g. `games/castle-war/`) and add/keep the menu
  link. Compression must be gzip-with-decompression-fallback or disabled —
  GitHub Pages serves no `Content-Encoding: br` headers, and a silent Brotli
  build 404s on load.

## 7. Concurrent-session Git safety

- Assume other sessions (and the collaborator `jellyggumi`) are editing this
  repository. Run `git status --short` before edits and again immediately
  before committing; treat unexpected changes as another session's work.
- Stage with explicit pathspecs only. Never `git add -A`, `git add .`, or a
  cleanup/reset that absorbs unrelated work — the root collects gitignored
  probe scripts and Unity projection files, and broad staging is how they leak.
- Never restore, discard, or force-overwrite another session's changes. On
  collision: stop, document, resolve explicitly. Never force-push.
- Before a destructive asset operation, tag the pre-state
  (`git tag -f pre-<operation>-<date>`) so the deletion is recoverable.

## 8. Production cycle

Game production runs through `skill://game-studio-harness` (3-stage cycle,
G1–G8 numeric gates). Resume by reading
`_workspace/current/production/task-manifest.md` and the latest retrospective
— never restart Stage 1 when a manifest already records a later stage.
