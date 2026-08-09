using UnityEngine;

namespace CastleBusters
{
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "CastleBusters/Unit DataMultiplier")]
    public class UnitData : ScriptableObject
    {
        public string unitName;
        public UnitType unitType;
        public float maxHP = 120f;
        public float moveSpeed = 2f;
        public float attackDamage = 25f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;

        [Header("Bomber Specific")]
        public float explosionRadius = 2.5f;
        public float explosionDamage = 80f;

        // Knight/Archer previously had no per-prefab tunables at all (only Bomber did),
        // even though UnitController.PerformMeleeCombo/TryMove already hardcode a knight
        // push-force multiplier, a combo-hit interval, an archer hop velocity, and a volley
        // follow-up delay. Exposing them here mirrors the Bomber explosionRadius/explosionDamage
        // precedent so a "Heavy Knight" or "Sniper Archer" variant prefab can be tuned without
        // touching code, instead of every unit of a type behaving identically.
        [Header("Knight Specific")]
        [Tooltip("Velocity multiplier applied to an enemy body shoved by the Knight advance-push (§2).")]
        public float knightPushForceMultiplier = 1.6f;
        [Tooltip("Seconds between hits within a multi-hit melee combo (3rd/6th swing).")]
        public float knightComboIntervalSeconds = 0.14f;

        [Header("Archer Specific")]
        [Tooltip("Upward velocity applied for the Archer's situational hop over elevation gaps.")]
        public float archerJumpVelocity = 6.5f;
        [Tooltip("Seconds before the follow-up shot in a Double/Sky-Volley combo (5th/10th shot).")]
        public float archerVolleyFollowupDelaySeconds = 0.18f;


        [Header("Visuals")]
        public Sprite uiIcon;
        public GameObject prefab;
    }
}
