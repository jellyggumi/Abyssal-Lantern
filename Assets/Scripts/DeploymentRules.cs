using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Roster cards that can be created during battle. Mirrors the four selection
    /// buttons; <see cref="DeployCard.Cannon"/> is deploy-only (never launched).
    /// </summary>
    public enum DeployCard { Knight, Archer, Cannon, Barrel }

    /// <summary>
    /// Field-population cap groups. Knight and Archer deliberately SHARE one group
    /// so the deploy verb thickens a line instead of flooding the map with bodies.
    /// </summary>
    public enum DeployCapGroup { Body, Battery, Hazard }

    /// <summary>
    /// Why a deploy was refused. Ordered most-permanent-first so the HUD can name the
    /// blocker the player must actually solve rather than the first one tripped.
    /// </summary>
    public enum DeployBlockReason { None, Locked, FieldCap, Cooldown, Supply, Zone }

    /// <summary>
    /// Supply (보급): the shared, real-time resource that gates mid-battle creation.
    /// Accrues on BOTH turns for BOTH sides — see design/deployment-economy.md §3.
    /// Pure of engine state so EditMode pins the curve.
    /// </summary>
    public static class SupplyRules
    {
        public const float MaxSupply = 24f;
        public const float StartSupply = 8f;
        public const float RegenPerSecond = 0.7f;
        /// <summary>Credited to the killer's side when one of its units kills an enemy unit.</summary>
        public const float KillBonus = 2f;
        /// <summary>Credited when a side destroys an opposing block.</summary>
        public const float BlockBonus = 0.5f;

        public static float Clamp(float supply) => Mathf.Clamp(supply, 0f, MaxSupply);

        /// <summary>Regen tick. Never exceeds <see cref="MaxSupply"/>, never goes negative.</summary>
        public static float Regen(float supply, float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return Clamp(supply);
            return Clamp(supply + RegenPerSecond * deltaSeconds);
        }

        /// <summary>Event credit (kill/block). Same clamp as regen — bonuses cannot overfill.</summary>
        public static float Credit(float supply, float amount) => Clamp(supply + amount);

        /// <summary>Spend. Returns false and leaves supply untouched when unaffordable.</summary>
        public static bool TrySpend(float supply, float cost, out float remaining)
        {
            if (cost < 0f || supply + 1e-4f < cost)
            {
                remaining = Clamp(supply);
                return false;
            }
            remaining = Clamp(supply - cost);
            return true;
        }
    }

    /// <summary>
    /// Per-card creation conditions (생성조건) and the deploy-zone test.
    /// Every number here is the authority for design/deployment-economy.md §4–§5;
    /// the runtime reads these constants instead of re-declaring them.
    /// </summary>
    public static class DeploymentRules
    {
        // ---- Deploy zone (§5) ----
        /// <summary>Never on the center line — a deploy always belongs to one side's half.</summary>
        public const float MinAbsX = 0.5f;
        /// <summary>Outer bound, inside the keeps' band (mirrors BrickPlacementRules.MaxAbsX intent).</summary>
        public const float MaxAbsX = 12.5f;
        public const float MinY = 0f;
        public const float MaxY = 8f;
        /// <summary>Radius used to reject a deploy landing on top of a live enemy body.</summary>
        public const float EnemyOverlapRadius = 0.45f;

        // ---- Cap-group ceilings, per side (§4) ----
        public const int BodyCap = 6;
        public const int BatteryCap = 2;
        public const int HazardCap = 3;

        public static DeployCapGroup GroupOf(DeployCard card)
        {
            switch (card)
            {
                case DeployCard.Cannon: return DeployCapGroup.Battery;
                case DeployCard.Barrel: return DeployCapGroup.Hazard;
                default: return DeployCapGroup.Body; // Knight + Archer share one cap
            }
        }

        public static int CapFor(DeployCapGroup group)
        {
            switch (group)
            {
                case DeployCapGroup.Battery: return BatteryCap;
                case DeployCapGroup.Hazard: return HazardCap;
                default: return BodyCap;
            }
        }

        public static int CapFor(DeployCard card) => CapFor(GroupOf(card));

        /// <summary>Supply price of one deploy.</summary>
        public static float CostOf(DeployCard card)
        {
            switch (card)
            {
                case DeployCard.Knight: return 5f;
                case DeployCard.Archer: return 6f;
                case DeployCard.Cannon: return 12f;
                case DeployCard.Barrel: return 4f;
                default: return 5f;
            }
        }

        /// <summary>Per-card, per-side reuse delay. Spending one card never locks another.</summary>
        public static float CooldownOf(DeployCard card)
        {
            switch (card)
            {
                case DeployCard.Knight: return 2.5f;
                case DeployCard.Archer: return 3.5f;
                case DeployCard.Cannon: return 12f;
                case DeployCard.Barrel: return 5f;
                default: return 2.5f;
            }
        }

        /// <summary>
        /// Turn the card becomes legal. Staggered so the roster teaches itself in order:
        /// melee → range → hazard → structure.
        /// </summary>
        public static int UnlockTurn(DeployCard card)
        {
            switch (card)
            {
                case DeployCard.Knight: return 0;
                case DeployCard.Archer: return 1;
                case DeployCard.Barrel: return 2;
                case DeployCard.Cannon: return 3;
                default: return 0;
            }
        }

        public static bool IsUnlocked(DeployCard card, int turnCount) => turnCount >= UnlockTurn(card);

        /// <summary>
        /// Enemy wall blocks that must be brought down before the battery can be sited.
        ///
        /// The turn gate alone meant artillery arrived for simply waiting. Requiring a
        /// breach makes the siege earn it: open a hole with the volley, then move the guns
        /// up. It only became a meaningful condition once a keep was more than one block
        /// deep — before the keep enlargement a single hit satisfied any such rule.
        /// </summary>
        public const int CannonBreachRequirement = 2;

        /// <summary>Cards whose unlock depends on having breached the enemy keep.</summary>
        public static bool NeedsBreach(DeployCard card) => card == DeployCard.Cannon;

        /// <summary>
        /// Whether the breach precondition is met. <paramref name="enemyWallsBreached"/>
        /// counts wall blocks lost by the *opposing* keep, so tearing down your own wall
        /// can never unlock your own battery.
        /// </summary>
        public static bool BreachSatisfied(DeployCard card, int enemyWallsBreached) =>
            !NeedsBreach(card) || enemyWallsBreached >= CannonBreachRequirement;

        /// <summary>Player-facing text for an unmet breach requirement. A lock that does not
        /// say what to do reads as a bug, not a rule.</summary>
        public static string BreachReasonText(int enemyWallsBreached) =>
            $"적 성벽 {CannonBreachRequirement}개를 부숴야 해금 (현재 {Mathf.Max(0, enemyWallsBreached)}개)";

        /// <summary>
        /// Geometric legality only (no scene queries), so EditMode pins the band exactly:
        /// inside the owner's half, inside the field band, and clear of both launch rings.
        /// </summary>
        public static bool InDeployZone(Vector2 position, bool deployerIsPlayer)
        {
            float absX = Mathf.Abs(position.x);
            if (absX < MinAbsX || absX > MaxAbsX) return false;
            if (position.y < MinY || position.y > MaxY) return false;
            // Own half only: the player reinforces the left field, the AI the right.
            if (deployerIsPlayer ? position.x > -MinAbsX : position.x < MinAbsX) return false;
            // A body inside the muzzle blocks every volley of that side (same exclusion
            // BrickPlacementRules applies to bricks).
            if (LaunchRingRules.IsInsideRing(position)) return false;
            return true;
        }

        /// <summary>
        /// Full 생성조건 gate. Conditions are tested most-permanent-first so the returned
        /// reason names the blocker the player has to solve, not merely the first failure:
        /// Locked (wait turns) → FieldCap (lose a unit) → Cooldown (wait seconds) →
        /// Supply (wait less) → Zone (just click elsewhere).
        /// </summary>
        public static DeployBlockReason Evaluate(
            DeployCard card,
            int turnCount,
            int aliveInGroup,
            float cooldownRemaining,
            float supply,
            Vector2 position,
            bool deployerIsPlayer)
        {
            if (!IsUnlocked(card, turnCount)) return DeployBlockReason.Locked;
            if (aliveInGroup >= CapFor(card)) return DeployBlockReason.FieldCap;
            if (cooldownRemaining > 0f) return DeployBlockReason.Cooldown;
            if (supply + 1e-4f < CostOf(card)) return DeployBlockReason.Supply;
            if (!InDeployZone(position, deployerIsPlayer)) return DeployBlockReason.Zone;
            return DeployBlockReason.None;
        }

        /// <summary>Player-facing (Korean) reason text — the HUD never shows a silent no-op.</summary>
        public static string ReasonText(DeployBlockReason reason, DeployCard card, int turnCount)
        {
            switch (reason)
            {
                case DeployBlockReason.Locked:
                    return $"{UnlockTurn(card)}턴부터 해금 (현재 {turnCount}턴)";
                case DeployBlockReason.FieldCap:
                    return $"배치 한도 초과 (최대 {CapFor(card)})";
                case DeployBlockReason.Cooldown:
                    return "재사용 대기 중";
                case DeployBlockReason.Supply:
                    return $"보급 부족 (필요 {CostOf(card):0})";
                case DeployBlockReason.Zone:
                    return "여기엔 배치할 수 없음";
                default:
                    return string.Empty;
            }
        }

        public static string DisplayName(DeployCard card)
        {
            switch (card)
            {
                case DeployCard.Knight: return "기사";
                case DeployCard.Archer: return "궁수";
                case DeployCard.Cannon: return "대포";
                case DeployCard.Barrel: return "화약통";
                default: return card.ToString();
            }
        }

        /// <summary>
        /// The same roster vocabulary, keyed by the runtime unit rather than the card.
        ///
        /// A projectile in flight knows its <see cref="UnitType"/> but not the card that paid
        /// for it, and the readback line has to name it in the words the HUD already uses.
        /// Routed through the card overload so there is exactly one place these nouns live —
        /// the alternative leaked raw asset names to the player once already ("EXPLOSIVEBARREL
        /// 준비", task #48).
        /// </summary>
        public static string DisplayName(UnitType type)
        {
            switch (type)
            {
                case UnitType.Knight: return DisplayName(DeployCard.Knight);
                case UnitType.Archer: return DisplayName(DeployCard.Archer);
                case UnitType.Cannon: return DisplayName(DeployCard.Cannon);
                case UnitType.Barrel: return DisplayName(DeployCard.Barrel);
                default: return "부대";
            }
        }

        /// <summary>The Cannon is an installation: it is placed, never launched (§2).</summary>
        public static bool IsDeployOnly(DeployCard card) => card == DeployCard.Cannon;

        /// <summary>
        /// Order the AI walks when picking what to deploy: the most board-changing legal
        /// card first, cheap bodies last. Deterministic (no RNG) so the enemy economy is
        /// symmetric with the player's and EditMode can pin the preference.
        /// </summary>
        public static readonly DeployCard[] AiPreferenceOrder =
        {
            DeployCard.Cannon,
            DeployCard.Archer,
            DeployCard.Knight,
            DeployCard.Barrel
        };

        public static DeployCard FromIndex(int index)
        {
            switch (index)
            {
                case 1: return DeployCard.Archer;
                case 2: return DeployCard.Cannon;
                case 3: return DeployCard.Barrel;
                default: return DeployCard.Knight;
            }
        }
    }

    /// <summary>
    /// Cannon (대포) installation contract — design/deployment-economy.md §6.
    /// Stationary, auto-firing, destructible. Pure so the reload/damage curve is pinned.
    /// </summary>
    public static class CannonRules
    {
        public const float MaxHP = 140f;
        public const float Range = 13f;
        public const float ReloadSeconds = 3.2f;
        public const float ShellDamage = 42f;
        // 1.5 -> 2.4: the battery is the roster's only area weapon since the bomber was
        // removed, and at 1.5 a shell that visibly landed in a cluster still only took the
        // one block it touched. 2.4 reliably catches a block and its neighbours, which is
        // what "heavy artillery" has to mean for the 12-supply price to read as worth it.
        public const float ShellSplashRadius = 2.4f;
        public const float MuzzleHeight = 0.55f;
        /// <summary>Extra apex the ballistic solve adds so the shell clears the caster's own wall.</summary>
        public const float ArcApexBonus = 2.5f;

        public static float SustainedDps => ShellDamage / ReloadSeconds;

        public static bool InRange(float distance) => distance >= 0f && distance <= Range;

        /// <summary>
        /// Ballistic launch velocity that reaches <paramref name="target"/> from
        /// <paramref name="muzzle"/>, apexing <see cref="ArcApexBonus"/> above the higher of
        /// the two endpoints so the shell arcs over the battery's own wall instead of
        /// drilling into it. Falls back to a flat lob when gravity is disabled.
        /// </summary>
        public static Vector2 SolveShellVelocity(Vector2 muzzle, Vector2 target, float gravity)
        {
            float g = Mathf.Abs(gravity);
            Vector2 delta = target - muzzle;
            if (g < 1e-4f) return delta.normalized * Range;

            float apex = Mathf.Max(muzzle.y, target.y) + ArcApexBonus - muzzle.y;
            apex = Mathf.Max(apex, 0.5f);

            float vy = Mathf.Sqrt(2f * g * apex);
            // Time up to apex, then down to the target's height.
            float tUp = vy / g;
            float dropHeight = Mathf.Max(0.05f, apex - delta.y);
            float tDown = Mathf.Sqrt(2f * dropHeight / g);
            float flight = Mathf.Max(0.05f, tUp + tDown);

            return new Vector2(delta.x / flight, vy);
        }
    }
}
