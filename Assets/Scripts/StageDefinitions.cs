using UnityEngine;

namespace CastleBusters
{
    /// <summary>Selectable siege layouts — three genuinely distinct concepts, not palette
    /// swaps of the same board. Stage1 ("Siege Plains") is the original frozen AC1-11
    /// contract and the fixed baseline every other stage is judged against: moderate
    /// distance, moderate obstacle density, an open dueling range. Stage2 ("Ashen Bastion")
    /// compresses the arena into a close-quarters fortress duel: shorter launch-to-launch
    /// distance, taller walls, zero starting kegs (hazards are earned mid-match, not handed
    /// out at the gate), and a relentless every-2nd-turn field mutation with no rest beats.
    /// Stage3 ("Frostbound Gorge") stretches the arena into a vast long-range gorge: the
    /// widest launch distance, kegs pushed out to the wings instead of hugging the bridge,
    /// a 5th field-obstacle kind (SpikeTrap), a 3-way vent rotation (adds Frost), and the
    /// slowest mutation cadence (every 4th turn) so the wide board reads as open, not
    /// cluttered.</summary>
    public enum StageId { Stage1, Stage2, Stage3 }

    /// <summary>
    /// Pure per-stage layout + composition numbers. GameManager copies these into its
    /// mutable static fields (CoreAbsX stays shared/unchanged; LaunchApronAbsX + ring
    /// positions + camera framing + wind cap are stage-dependent) once at StartGame(), never
    /// during Awake(), so EditMode tests (which only ever call Awake()/CreateGround() via
    /// reflection, never Start()) keep seeing the Stage1 defaults regardless of what a prior
    /// PlayMode session selected. All fields are plain data so tests can assert on them
    /// directly.
    /// </summary>
    public readonly struct StageLayout
    {
        public readonly StageId id;
        public readonly string displayName;
        public readonly float launchApronAbsX;   // player/enemy launch point |x|
        public readonly float groundHalfWidth;   // ground tile grid half-extent
        public readonly float groundAnchorAbsX;  // |x| beyond which ground tiles are anchors
        public readonly float gateAbsX;          // deep-wing aerial gate |x| (0 = center gate keeps its own x)
        public readonly float windCapEnd;        // GameManager.windCapEnd override
        public readonly float cameraDesiredWorldWidth;
        public readonly float cameraMaxHalfHeight;
        public readonly FieldObstacleKind[] allowedGimmicks;
        // True for a stage that exists in the table but isn't finished/offered yet — the
        // intro screen's stage picker renders it dimmed and non-interactive, and
        // GameManager.RequestStage() refuses to select it. Locked stages otherwise carry
        // Stage1's numbers (never garbage/zeroed data) so a future bug can't accidentally
        // route live gameplay through an unconfigured layout.
        public readonly bool locked;

        // ---- Composition (concept-level differentiation, not just spacing) ----
        // Match-start powder kegs. Stage2 deliberately ships empty (a fortress isn't handed
        // free explosives at the gate); Stage3's spread wide into the wings instead of
        // hugging the bridge the way Stage1's do.
        public readonly Vector3[] barrelPositions;
        // 2 = Stage1/3's original stone column; Stage2 goes to 3 for a heavier, more
        // fortified silhouette matching its "bastion" concept.
        public readonly int wallHeightBlocks;
        // GimmickFieldDirector.maxFieldObstacles for the active stage — Stage2 stays lean
        // (dense but few pieces cycling fast), Stage3 can carry more across its wider board.
        public readonly int maxFieldObstacles;
        // Field composition mutates every Nth turn (GimmickFieldDirector.PlanForTurn) —
        // Stage1=3 (baseline), Stage2=2 (relentless, no rest beat), Stage3=4 (sedate, the
        // wide board reads as open rather than cluttered).
        public readonly int mutateEveryNTurns;
        // Multiplies the background sprite's tint (CreateBackground) for a per-stage mood
        // without needing dedicated art: white = unchanged, Stage2 = ashen grey.
        public readonly Color backgroundTint;

        public StageLayout(StageId id, string displayName, float launchApronAbsX, float groundHalfWidth,
            float groundAnchorAbsX, float gateAbsX, float windCapEnd,
            float cameraDesiredWorldWidth, float cameraMaxHalfHeight,
            Vector3[] barrelPositions, int wallHeightBlocks, int maxFieldObstacles, int mutateEveryNTurns,
            Color backgroundTint, FieldObstacleKind[] allowedGimmicks, bool locked = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.launchApronAbsX = launchApronAbsX;
            this.groundHalfWidth = groundHalfWidth;
            this.groundAnchorAbsX = groundAnchorAbsX;
            this.gateAbsX = gateAbsX;
            this.windCapEnd = windCapEnd;
            this.cameraDesiredWorldWidth = cameraDesiredWorldWidth;
            this.cameraMaxHalfHeight = cameraMaxHalfHeight;
            this.barrelPositions = barrelPositions;
            this.wallHeightBlocks = wallHeightBlocks;
            this.maxFieldObstacles = maxFieldObstacles;
            this.mutateEveryNTurns = mutateEveryNTurns;
            this.backgroundTint = backgroundTint;
            this.allowedGimmicks = allowedGimmicks;
            this.locked = locked;
        }
    }

    /// <summary>Pure stage table + lookup. EditMode-pinned (StageDefinitionsTests).</summary>
    public static class StageDefinitions
    {
        // Stage1 numbers mirror the frozen constants exactly (GameManager.LaunchApronAbsX=14.5,
        // groundHalfWidth=20, anchor band |x|>=10, gates at +-15, windCapEnd=6.5,
        // desiredWorldWidth=39, maxHalfHeight=11.2, 4 kegs at the original bridge-hugging
        // spots, 2-high walls, 6-cap/3-turn-mutate field, white tint) — selecting Stage1
        // must be a total no-op. This is the fixed baseline Stage2/Stage3 are judged against.
        public static readonly StageLayout Stage1 = new StageLayout(
            StageId.Stage1, "SIEGE PLAINS / 공성 평원",
            launchApronAbsX: 14.5f,
            groundHalfWidth: 20f,
            groundAnchorAbsX: 10f,
            gateAbsX: 15f,
            windCapEnd: 6.5f,
            cameraDesiredWorldWidth: 39f,
            cameraMaxHalfHeight: 11.2f,
            barrelPositions: GameManager.InitialBarrelPositions,
            wallHeightBlocks: 2,
            maxFieldObstacles: 6,
            mutateEveryNTurns: 3,
            backgroundTint: Color.white,
            allowedGimmicks: new[] { FieldObstacleKind.Barrel, FieldObstacleKind.MiniTower, FieldObstacleKind.Rune, FieldObstacleKind.Patrol });

        // Stage2 "Desolate Dunes": a close-quarters fortress duel, not a smaller Stage1.
        // Launch apron pulled in from 14.5 to 13.5 (player-to-player distance 29 -> 27,
        // -6.9%) — a real but bounded compression, ground/camera/gate/wind scaled to match
        // using the same margin ratios Stage1/3 already establish (ground = apron + 5.5,
        // anchor = half of ground-half-width, gate = apron + 0.5, camera width ~= apron *
        // 2.69, camera max-height ~= apron * 0.2872). 13.5 (not a tighter number) is
        // deliberate: it keeps a real 1.0u buffer between the enemy launch ring's inner
        // edge and the shared core's own collider edge (balance-review finding — a tighter
        // apron left zero free strip there, tangent to the core). Zero starting kegs (a
        // fortress isn't handed free explosives at its own gate — hazards are earned
        // mid-match via the field director instead) and 3-high walls (vs Stage1's 2) give
        // it a visibly heavier, more fortified silhouette. Field composition mutates every
        // 2nd turn (vs Stage1's 3rd) with a lower obstacle cap (4 vs 6) — dense,
        // fast-cycling, never a rest beat. Wind cap scales proportionately to the distance
        // compression (-6.9% distance -> ~-7% wind range, matching Stage3's own
        // sub-proportional-widening precedent instead of an arbitrary steeper cut) —
        // shorter range still rewards precision over wind compensation without stacking an
        // outsized easing on top of the already-leaner obstacle/keg composition. Warm sandy
        // background tint reinforces the desert mood.
        public static readonly StageLayout Stage2 = new StageLayout(
            StageId.Stage2, "DESOLATE DUNES / 황량한 모래언덕",
            launchApronAbsX: 13.5f,
            groundHalfWidth: 19f,
            groundAnchorAbsX: 9.5f,
            gateAbsX: 14f,
            windCapEnd: 6.2f,
            cameraDesiredWorldWidth: 36.3f,
            cameraMaxHalfHeight: 10.4f,
            barrelPositions: System.Array.Empty<Vector3>(),
            wallHeightBlocks: 3,
            maxFieldObstacles: 4,
            mutateEveryNTurns: 2,
            backgroundTint: new Color(1.0f, 0.9f, 0.7f, 1f),
            allowedGimmicks: new[] { FieldObstacleKind.Rune, FieldObstacleKind.SpikeTrap, FieldObstacleKind.Patrol });

        // Stage3 "Volcanic Abyss": a vast long-range gorge, not just a wider Stage1.
        // Launch apron pushed from 14.5 to 18.5 (player-to-player distance 29 -> 37,
        // +27.6%) with ground/camera/gate/wind widened to match and the wind cap raised
        // 6.5 -> 7.2 to keep late-match aiming meaningful over the longer throw. Kegs move
        // from Stage1's bridge-hugging ±6.5/±11 out to the wings at ±11.5/±15.0, mirroring
        // Stage1's exact margin ratios scaled to the wider apron (outer keg clears the
        // muzzle by 3.5u, same as Stage1's ±11; inner keg clears the core by 2.5u, same as
        // Stage1's ±6.5) — strictly inside every pinned safety threshold (core clearance
        // >1.0u, muzzle clearance >3.0u — both with real margin, not boundary-exact) instead
        // of the wider board's raw midpoint. The wide midfield between ±11.5 stays open,
        // which is the whole point of a "gorge". Field composition rotates a 5th obstacle
        // kind (SpikeTrap) and mutates every 4th turn (vs Stage1's 3rd) at a higher cap (7
        // vs 6) — the wide board reads as open, not cluttered, and the pacing is
        // deliberately slower/tenser than Stage1 or Stage2. Fiery reddish background tint
        // reinforces the volcanic mood.
        public static readonly StageLayout Stage3 = new StageLayout(
            StageId.Stage3, "VOLCANIC ABYSS / 화산 심연",
            launchApronAbsX: 18.5f,
            groundHalfWidth: 24f,
            groundAnchorAbsX: 12f,
            gateAbsX: 19f,
            windCapEnd: 7.2f,
            cameraDesiredWorldWidth: 47f,
            cameraMaxHalfHeight: 13.5f,
            barrelPositions: new[]
            {
                new Vector3(-15.0f, 0.5f, 0f),
                new Vector3(-11.5f, 0.5f, 0f),
                new Vector3(11.5f, 0.5f, 0f),
                new Vector3(15.0f, 0.5f, 0f),
            },
            wallHeightBlocks: 2,
            maxFieldObstacles: 7,
            mutateEveryNTurns: 4,
            backgroundTint: new Color(1.0f, 0.75f, 0.7f, 1f),
            allowedGimmicks: new[] { FieldObstacleKind.Barrel, FieldObstacleKind.MiniTower, FieldObstacleKind.SpikeTrap });

        public static StageLayout For(StageId id)
        {
            switch (id)
            {
                case StageId.Stage2: return Stage2;
                case StageId.Stage3: return Stage3;
                default: return Stage1;
            }
        }
    }
}
