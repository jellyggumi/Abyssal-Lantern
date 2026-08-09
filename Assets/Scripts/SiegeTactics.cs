using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Target selection policy: knights/archers/bombers hunt the opponent's INSTALLATIONS —
    /// gimmicks the enemy placed and gimmicks stationed in their camp — never the floor.
    /// Candidates are ranked by weighted distance (lower score wins), so a slightly farther
    /// gimmick outranks a nearby plain wall block, but units never march across the whole
    /// map past everything.
    /// </summary>
    public static class TargetingRules
    {
        /// <summary>Blocks whose center sits below this are terrain tiles, never targets.
        /// Ground rows start at y = -0.5; every legitimate structure sits at y >= 0.5.</summary>
        public const float GroundLineY = 0f;

        public const float GimmickWeight = 0.55f;   // primary: cores, kegs, enemy installations
        public const float UnitWeight = 0.85f;      // enemy bodies en route
        public const float StructureWeight = 1.0f;  // plain castle/wall blocks

        public static bool IsGroundTile(float blockCenterY) => blockCenterY < GroundLineY;

        public static float Score(float distance, float weight) => distance * weight;

        /// <summary>Field-half test for neutral installations (kegs, towers, chariot).</summary>
        public static bool OnEnemyHalf(float x, bool attackerIsPlayer)
        {
            return attackerIsPlayer ? x > 0.5f : x < -0.5f;
        }
    }

    /// <summary>
    /// Player brick placement (pre-designated builds): during the ENEMY turn the player
    /// marks brick sites; the bricks materialize when the player's own turn starts.
    /// Placement can never crowd the unit muzzles (launch rings) or leave the field band.
    /// </summary>
    public static class BrickPlacementRules
    {
        public const int MaxPendingBricks = 2;
        public const float MinY = 0f;
        public const float MaxY = 8f;
        public const float MaxAbsX = 10.5f; // inside the keeps' band, well clear of both rings

        public static bool CanPlace(Vector2 position)
        {
            if (LaunchRingRules.IsInsideRing(position)) return false;
            if (Mathf.Abs(position.x) > MaxAbsX) return false;
            if (position.y < MinY || position.y > MaxY) return false;

            // Check if position overlaps any enemy unit to avoid moving/pushing them
            var colliders = Physics2D.OverlapCircleAll(position, 0.45f);
            foreach (var col in colliders)
            {
                var unit = col.GetComponent<UnitController>();
                if (unit != null && !unit.isPlayerUnit && unit.CurrentState != UnitState.Dead)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Pure per-unit combo rules (AOS overhaul, docs/design/aos-overhaul.md §2).
    /// Attack ordinals are 1-based and cycle naturally via modulo, so the 6th/10th beats
    /// repeat every cycle. All functions are static and EditMode-pinned.
    /// </summary>
    public static class UnitCombos
    {
        /// <summary>Bomber landing fuse: armed on touchdown, detonates this many seconds later.</summary>
        public const float BomberFuseSeconds = 2f;

        /// <summary>Knight melee: every 6th swing lands 3 hits, every 3rd lands 2, else 1.</summary>
        public static int KnightHits(int attackOrdinal)
        {
            if (attackOrdinal <= 0) return 1;
            if (attackOrdinal % 6 == 0) return 3;
            if (attackOrdinal % 3 == 0) return 2;
            return 1;
        }

        public enum ArcherVolleyKind { Single, Double, FrontAndLob }

        /// <summary>
        /// Archer: every 10th shot fires a double follow-up — one straight, one high lob —
        /// every 5th fires a plain double, else single.
        /// </summary>
        public static ArcherVolleyKind ArcherVolley(int attackOrdinal)
        {
            if (attackOrdinal <= 0) return ArcherVolleyKind.Single;
            if (attackOrdinal % 10 == 0) return ArcherVolleyKind.FrontAndLob;
            if (attackOrdinal % 5 == 0) return ArcherVolleyKind.Double;
            return ArcherVolleyKind.Single;
        }

        /// <summary>Arrows fired for a volley kind (lob counts as the second arrow).</summary>
        public static int ArrowsFor(ArcherVolleyKind kind)
        {
            return kind == ArcherVolleyKind.Single ? 1 : 2;
        }

        /// <summary>
        /// Archer situational jump: hop when the target sits noticeably above the archer
        /// (elevation gap) — path blockage is handled by the shared walk raycast.
        /// </summary>
        public static bool ArcherShouldJump(float selfY, float targetY, float elevationThreshold = 1.2f)
        {
            return targetY - selfY >= elevationThreshold;
        }

        /// <summary>
        /// Knight advance push: shove an enemy body that stands between the knight and a
        /// farther target while the knight is walking (not when the enemy IS the target in range).
        /// </summary>
        public static bool KnightShouldPush(float distToBlocker, float distToTarget, float attackRange)
        {
            return distToBlocker < attackRange * 0.9f && distToTarget > attackRange * 1.1f;
        }
    }

    /// <summary>Launch multiplicity per own-side turn ordinal (1-based). §2 Bomber.</summary>
    public static class VolleyRules
    {
        public static int BomberVolleyCount(int ownTurnOrdinal)
        {
            if (ownTurnOrdinal >= 9) return 4;
            if (ownTurnOrdinal >= 3) return 2;
            return 1;
        }

        /// <summary>
        /// Own-side turn ordinal (1-based) from the global turn counter. Player turns are
        /// even global counts (0, 2, 4...), AI turns odd — both map to 1, 2, 3...
        /// </summary>
        public static int OwnTurnOrdinal(int globalTurnCount)
        {
            return Mathf.Max(0, globalTurnCount) / 2 + 1;
        }
    }

    /// <summary>Timed, temporary, randomly-placed eruption vents. §3.</summary>
    public static class VentSchedule
    {
        public const int LifetimeTurns = 3;
        public const float MinX = -7f;
        public const float MaxX = 7f;
        public const float GroundY = 0.15f;

        /// <summary>A vent materializes on every 3rd turn beat (turn % 3 == 2, turn ≥ 2).</summary>
        public static bool ShouldSpawnOnTurn(int turn)
        {
            return turn >= 2 && turn % 3 == 2;
        }

        /// <summary>Vents alternate styles so both hazards appear over a match.</summary>
        public static EruptionStyle StyleForTurn(int turn)
        {
            return (turn / 3) % 2 == 0 ? EruptionStyle.Magma : EruptionStyle.Petal;
        }

        /// <summary>
        /// Stage-aware vent styling: Stage2 has volcanic Magma only; Stage3 has frozen Frost only;
        /// Stage1 alternates Magma and Petal (unchanged baseline).
        /// </summary>
        public static EruptionStyle StyleForTurn(int turn, StageId stage)
        {
            if (stage == StageId.Stage2) return EruptionStyle.Magma;
            if (stage == StageId.Stage3) return EruptionStyle.Frost;
            return StyleForTurn(turn);
        }

        public static bool Expired(int bornTurn, int currentTurn)
        {
            return currentTurn - bornTurn >= LifetimeTurns;
        }
    }

    /// <summary>
    /// Launch-ring exclusion (§5): nothing solid may materialize inside either launch
    /// affordance circle — a wall in the muzzle blocks every volley of that side.
    /// </summary>
    public static class LaunchRingRules
    {
        public const float RingRadius = 3.5f;   // LaunchManager.launchActivationRadius
        // Mutable (not const): GameManager.ApplyStageLayout() overrides these to
        // ±StageLayout.launchApronAbsX at Start() for the active stage. Defaults mirror
        // Stage1 (GameManager.LaunchApronAbsX=14.5) so EditMode tests — which never call
        // GameManager.Start() — keep seeing the original Stage1 ring positions.
        public static float PlayerRingX = -14.5f;
        public static float EnemyRingX = 14.5f;
        public const float RingY = 0.5f;

        public static bool IsInsideRing(Vector2 position)
        {
            return Vector2.Distance(position, new Vector2(PlayerRingX, RingY)) <= RingRadius
                || Vector2.Distance(position, new Vector2(EnemyRingX, RingY)) <= RingRadius;
        }
    }

    /// <summary>
    /// Capture objective rules (§1): attackers alone in the zone fill the gauge; any
    /// defender freezes it (contested); an empty zone decays at half speed.
    /// </summary>
    public static class CaptureRules
    {
        public const float CaptureSeconds = 6f;
        public const float CaptureRadius = 2.6f;
        public const float DecayRate = 0.5f; // fraction of fill speed while abandoned

        public static float Tick(float progress, int attackers, int defenders, float dt)
        {
            if (attackers > 0 && defenders == 0)
            {
                progress += dt / CaptureSeconds;
            }
            else if (attackers == 0)
            {
                progress -= dt / CaptureSeconds * DecayRate;
            }
            // contested (attackers > 0 && defenders > 0): gauge holds.
            return Mathf.Clamp01(progress);
        }

        public static bool Captured(float progress) => progress >= 1f - 1e-5f;
    }

    /// <summary>
    /// Situational buff/nerf/gate events (§6): every 4th turn one balance event spawns,
    /// aimed at whichever side is behind on core health. Pure so tests pin the policy.
    /// </summary>
    public static class BalanceEventPlanner
    {
        public const int LifetimeTurns = 4;
        public const float NeutralBand = 0.15f;

        public enum EventKind { None, BuffRune, DebuffRune, PowerGate, ReduceGate, NeutralMultiplyGate }

        public struct BalanceEvent
        {
            public EventKind kind;
            /// <summary>True → placed on the player's approach lane, false → enemy's.</summary>
            public bool onPlayerSide;
        }

        public static bool ShouldFireOnTurn(int turn) => turn >= 1 && turn % 4 == 1;

        /// <summary>
        /// coreFrac: current/max core health per side. The trailing side receives help on
        /// its approach; the leading side gets a hindrance; near-parity yields a neutral
        /// center gate. Alternates help/hinder by beat so one side is never double-punished.
        /// </summary>
        public static BalanceEvent Plan(int turn, float playerCoreFrac, float enemyCoreFrac)
        {
            var evt = new BalanceEvent { kind = EventKind.None, onPlayerSide = true };
            if (!ShouldFireOnTurn(turn)) return evt;

            float gap = playerCoreFrac - enemyCoreFrac;
            bool helpBeat = (turn / 4) % 2 == 0;

            if (Mathf.Abs(gap) < NeutralBand)
            {
                evt.kind = EventKind.NeutralMultiplyGate;
                return evt;
            }

            bool playerTrailing = gap < 0f;
            if (helpBeat)
            {
                evt.kind = (turn / 4) % 4 < 2 ? EventKind.BuffRune : EventKind.PowerGate;
                evt.onPlayerSide = playerTrailing;
            }
            else
            {
                evt.kind = (turn / 4) % 4 < 2 ? EventKind.DebuffRune : EventKind.ReduceGate;
                evt.onPlayerSide = !playerTrailing;
            }
            return evt;
        }
    }

    /// <summary>
    /// Chariot phase policy (§4): the war machine escalates as it takes damage.
    /// </summary>
    public static class ChariotRules
    {
        public const float MaxHP = 150f;
        public const float RamDamage = 22f;
        public const float RamCooldownSeconds = 0.8f;
        public const float RespawnDelaySeconds = 5f;
        public const float KillPlaneY = -20f;
        public enum ChariotPhase { Patrol, Frenzy, Rampage }

        public static ChariotPhase PhaseForHealth(float currentHP, float maxHP)
        {
            if (maxHP <= 0f) return ChariotPhase.Patrol;
            float frac = currentHP / maxHP;
            if (frac > 2f / 3f) return ChariotPhase.Patrol;
            if (frac > 1f / 3f) return ChariotPhase.Frenzy;
            return ChariotPhase.Rampage;
        }

        /// <summary>Horizontal cruise speed per phase.</summary>
        public static float SpeedFor(ChariotPhase phase)
        {
            switch (phase)
            {
                case ChariotPhase.Frenzy: return 2.4f;
                case ChariotPhase.Rampage: return 3.6f;
                default: return 1.1f;
            }
        }

        /// <summary>Half-width of the patrol sweep per phase (world units around origin).</summary>
        public static float SweepFor(ChariotPhase phase)
        {
            switch (phase)
            {
                case ChariotPhase.Frenzy: return 5.5f;
                case ChariotPhase.Rampage: return 8f;
                default: return 3.2f;
            }
        }
    }

    /// <summary>
    /// Flight patterns for the airborne war beast (the lateral chariot was reworked into a
    /// flying gimmick — "횡 이동 기믹을 날아다니는 것으로"). Every phase moves in BOTH axes,
    /// never a fixed x-line: Patrol glides with a vertical bob, Frenzy carves a figure-8,
    /// Rampage swoops in low dive passes. Pure of engine state so tests pin the envelope.
    /// </summary>
    public static class FlightRules
    {
        public const float BaseAltitude = 4.2f;
        public const float SteerGain = 2.2f;   // homing pull toward the pattern point
        public const float DiveDepth = 3.0f;    // Rampage dips this far below BaseAltitude

        public static Vector2 FlightPoint(ChariotRules.ChariotPhase phase, float t, float originX)
        {
            switch (phase)
            {
                case ChariotRules.ChariotPhase.Frenzy:
                    // Figure-8 (1:2 Lissajous): fast, wide, hard to predict.
                    return new Vector2(
                        originX + Mathf.Sin(t * 0.9f) * ChariotRules.SweepFor(phase),
                        BaseAltitude + Mathf.Sin(t * 1.8f) * 1.8f);
                case ChariotRules.ChariotPhase.Rampage:
                    // Swoop passes: long strafes that dive to the deck and climb out.
                    return new Vector2(
                        originX + Mathf.Sin(t * 0.7f) * ChariotRules.SweepFor(phase),
                        BaseAltitude - Mathf.Abs(Mathf.Sin(t * 0.35f)) * DiveDepth);
                default:
                    // Patrol: lazy glide, gentle bob.
                    return new Vector2(
                        originX + Mathf.Sin(t * 0.5f) * ChariotRules.SweepFor(phase),
                        BaseAltitude + Mathf.Sin(t * 1.3f) * 0.9f);
            }
        }
    }
}
