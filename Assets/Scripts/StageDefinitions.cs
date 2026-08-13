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

    /// <summary>Wall material tier for one keep course. Maps 1:1 onto the BlockData
    /// resources (Wood 30 / Stone 85 / Iron 150 HP) so a stage's silhouette states its
    /// durability without text: timber splinters, stone holds, iron gleams.</summary>
    public enum KeepTier { Wood, Stone, Iron }

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
        // Wall material per keep course, aligned index-for-index with GameManager.KeepProfile
        // (outpost, outer, middle, inner). Level design lives here: the same four-course
        // profile reads differently per stage because the materials differ, not the shape.
        public readonly KeepTier[] keepCourseMaterials;

        public StageLayout(StageId id, string displayName, float launchApronAbsX, float groundHalfWidth,
            float groundAnchorAbsX, float gateAbsX, float windCapEnd,
            float cameraDesiredWorldWidth, float cameraMaxHalfHeight,
            Vector3[] barrelPositions, int wallHeightBlocks, int maxFieldObstacles, int mutateEveryNTurns,
            Color backgroundTint, FieldObstacleKind[] allowedGimmicks, KeepTier[] keepCourseMaterials,
            bool locked = false)
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
            this.keepCourseMaterials = keepCourseMaterials;
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
            launchApronAbsX: 17.0f,
            groundHalfWidth: 23f,
            groundAnchorAbsX: 11.5f,
            gateAbsX: 17.5f,
            windCapEnd: 7.0f,
            cameraDesiredWorldWidth: 45f,
            cameraMaxHalfHeight: 11.2f,
            barrelPositions: GameManager.InitialBarrelPositions,
            wallHeightBlocks: 3,
            maxFieldObstacles: 6,
            mutateEveryNTurns: 3,
            backgroundTint: Color.white,
            allowedGimmicks: new[] { FieldObstacleKind.Barrel, FieldObstacleKind.MiniTower, FieldObstacleKind.Rune, FieldObstacleKind.Patrol },
            // 목책 전초 → 석재 성벽 → 철재 내성: the approach is soft and teaches the breach,
            // the wall line holds, the course shielding the core gleams. Wall HP total
            // 3·30 + 3·85 + 4·85 + 5·150 = 1435, which puts the MatchLengthModel estimate
            // at ~321s — inside the five-minute band, closer than all-stone ever was.
            keepCourseMaterials: new[] { KeepTier.Wood, KeepTier.Stone, KeepTier.Stone, KeepTier.Iron });

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
            launchApronAbsX: 15.8f,
            groundHalfWidth: 21.8f,
            groundAnchorAbsX: 10.9f,
            gateAbsX: 16.3f,
            windCapEnd: 6.7f,
            cameraDesiredWorldWidth: 41.9f,
            cameraMaxHalfHeight: 10.4f,
            barrelPositions: System.Array.Empty<Vector3>(),
            wallHeightBlocks: 4,
            maxFieldObstacles: 4,
            mutateEveryNTurns: 2,
            backgroundTint: new Color(1.0f, 0.9f, 0.7f, 1f),
            allowedGimmicks: new[] { FieldObstacleKind.Rune, FieldObstacleKind.SpikeTrap, FieldObstacleKind.Patrol },
            // A bastion is strongest at its wall line: the iron bulwark sits at the MIDDLE
            // course. The approaches are rammed earth and timber, because a taller keep
            // built of the same stone as Stage1 would simply take LONGER to knock down —
            // 18 blocks of stone models at 373s against a 300s target. Wall HP total
            // 3·30 + 4·30 + 5·150 + 6·85 = 1470, which lands at 328s: a bigger fortress
            // that is not a longer grind, with the escalation carried by its faster field
            // mutation and leaner obstacle cap instead.
            keepCourseMaterials: new[] { KeepTier.Wood, KeepTier.Wood, KeepTier.Iron, KeepTier.Stone });

        // Stage3 "Volcanic Abyss": a vast long-range gorge, not just a wider Stage1.
        // Launch apron pushed from 14.5 to 18.5 (player-to-player distance 29 -> 37,
        // +27.6%) with ground/camera/gate/wind widened to match and the wind cap raised
        // 6.5 -> 7.2 to keep late-match aiming meaningful over the longer throw. Kegs sit
        // in the wings at ±11.5/±14.0 — the outer pair was ±15.0 until the muzzle-footprint
        // defect fix (2026-08-12): 3.5u of muzzle clearance is INSIDE the launched Knight's
        // spawn footprint (2.64u body half-width + 0.76u keg half-width + drift), so the
        // first knight volley detonated it at the muzzle, exactly like Stage1's removed ±11
        // pair. 14.0 keeps the wing identity with real clearance (4.5u > the 4.2u footprint
        // band; KegPlacementSafetyTests derives the band from the actual prefabs). The wide
        // midfield between ±11.5 stays open, which is the whole point of a "gorge". Field
        // composition rotates a 5th obstacle kind (SpikeTrap) and mutates every 4th turn
        // (vs Stage1's 3rd) at a higher cap (7 vs 6) — the wide board reads as open, not
        // cluttered, and the pacing is deliberately slower/tenser than Stage1 or Stage2.
        // Fiery reddish background tint reinforces the volcanic mood.
        public static readonly StageLayout Stage3 = new StageLayout(
            StageId.Stage3, "VOLCANIC ABYSS / 화산 심연",
            launchApronAbsX: 21.5f,
            groundHalfWidth: 27f,
            groundAnchorAbsX: 13.5f,
            gateAbsX: 22f,
            windCapEnd: 7.7f,
            cameraDesiredWorldWidth: 54f,
            cameraMaxHalfHeight: 13.5f,
            // Wing kegs only, both outside the keep entirely. The inner pair was ±11.5 —
            // 2.5u from a core with a 2.2u blast, the same too-thin margin that let Stage1's
            // kegs splash their own core once bodies shoved them (2026-08-13). ±12.5 clears
            // the core by 3.5u and the muzzle by 9.0u; ±16.5 clears the muzzle by 5.0u.
            // Neither sits in a wall column (|x| ∈ [3.5, 7.5]), so nothing ejects them.
            barrelPositions: new[]
            {
                new Vector3(-16.5f, 0.5f, 0f),
                new Vector3(-12.5f, 0.5f, 0f),
                new Vector3(12.5f, 0.5f, 0f),
                new Vector3(16.5f, 0.5f, 0f),
            },
            // Campaign redistribution: fortress height now rises 2 -> 3 -> 4 across the
            // sequential unlock order. Stage3 previously kept Stage1's 2 — the only stage
            // value carried over without a stated reason — which made the last stage the
            // softest fortress in the game. Wind stays distance-derived (Stage2 lower,
            // Stage3 higher) because that is physics, not progression, and pacing
            // (obstacle cap and mutation cadence) stays each stage's identity.
            wallHeightBlocks: 5,
            maxFieldObstacles: 7,
            mutateEveryNTurns: 4,
            backgroundTint: new Color(1.0f, 0.75f, 0.7f, 1f),
            allowedGimmicks: new[] { FieldObstacleKind.Barrel, FieldObstacleKind.MiniTower, FieldObstacleKind.SpikeTrap },
            // The final citadel: a vast charred palisade — three courses of timber the ash
            // has dried to tinder — around a single iron heart guarding the core. It is the
            // widest, tallest keep in the game at 21 blocks, and all-stone it would model at
            // 414s: a seven-minute grind, not a five-minute siege. Timber keeps the
            // silhouette huge while the match stays the intended length. Wall HP total
            // 3·30 + 5·30 + 6·30 + 7·150 = 1470 → 328s.
            keepCourseMaterials: new[] { KeepTier.Wood, KeepTier.Wood, KeepTier.Wood, KeepTier.Iron });

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
