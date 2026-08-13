using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    public enum HeroItemType { Sword, Shield, Boots }

    /// <summary>
    /// Per-side hero growth: destroying gimmicks drops loot; any unit collecting it raises that
    /// side's stats for the current best-of-three series. A new runtime session clears the
    /// static state; scene reloads within the series deliberately do not.
    /// </summary>
    public static class HeroGrowth
    {
        public const float DamagePerSword = 0.15f;
        public const float HpPerShield = 0.20f;
        public const float SpeedPerBoots = 0.12f;
        public const int MaxStacksPerType = 5;

        // [0] = player side, [1] = enemy side.
        private static readonly int[] swords = new int[2];
        private static readonly int[] shields = new int[2];
        private static readonly int[] boots = new int[2];


        private static int Side(bool isPlayer) => isPlayer ? 0 : 1;

        public static void Reset()
        {
            for (int i = 0; i < 2; i++) { swords[i] = 0; shields[i] = 0; boots[i] = 0; }
        }

        /// <summary>Grants one stack; returns the new stack count (clamped at the cap).</summary>
        public static int Grant(bool isPlayer, HeroItemType type)
        {
            int s = Side(isPlayer);
            switch (type)
            {
                case HeroItemType.Sword: return swords[s] = Mathf.Min(MaxStacksPerType, swords[s] + 1);
                case HeroItemType.Shield: return shields[s] = Mathf.Min(MaxStacksPerType, shields[s] + 1);
                default: return boots[s] = Mathf.Min(MaxStacksPerType, boots[s] + 1);
            }
        }

        public static int Stacks(bool isPlayer, HeroItemType type)
        {
            int s = Side(isPlayer);
            return type == HeroItemType.Sword ? swords[s] : type == HeroItemType.Shield ? shields[s] : boots[s];
        }

        public static float DamageMult(bool isPlayer) => 1f + swords[Side(isPlayer)] * DamagePerSword;
        public static float HpMult(bool isPlayer) => 1f + shields[Side(isPlayer)] * HpPerShield;
        public static float SpeedMult(bool isPlayer) => 1f + boots[Side(isPlayer)] * SpeedPerBoots;

        public static string KoreanName(HeroItemType type)
        {
            return type == HeroItemType.Sword ? "공격의 검" : type == HeroItemType.Shield ? "수호의 방패" : "신속의 부츠";
        }
    }

    /// <summary>Pure drop policy: which destroyed gimmicks yield loot, and what kind.</summary>
    public static class ItemDropRules
    {
        public const float GimmickDropChance = 0.6f;
        public const float PickupLifetimeSeconds = 14f;

        public static bool ShouldDrop(float roll01) => roll01 < GimmickDropChance;

        public static HeroItemType TypeForRoll(float roll01)
        {
            float r = Mathf.Repeat(roll01, 1f);
            if (r < 1f / 3f) return HeroItemType.Sword;
            if (r < 2f / 3f) return HeroItemType.Shield;
            return HeroItemType.Boots;
        }
    }

    /// <summary>Spawns loot at destroyed-gimmick sites ("아이템을 얻기 위해 기믹을 부순다").</summary>
    public static class ItemDropper
    {
        /// <summary>Chance-gated drop (kegs, towers). Returns the pickup or null.</summary>
        public static GameObject TrySpawn(Vector3 position)
        {
            if (!Application.isPlaying) return null;
            if (!ItemDropRules.ShouldDrop(Random.value)) return null;
            return Spawn(position, ItemDropRules.TypeForRoll(Random.value));
        }

        /// <summary>Guaranteed drop (the war beast, boss-grade gimmicks).</summary>
        public static GameObject SpawnGuaranteed(Vector3 position)
        {
            if (!Application.isPlaying) return null;
            return Spawn(position, ItemDropRules.TypeForRoll(Random.value));
        }

        private static GameObject Spawn(Vector3 position, HeroItemType type)
        {
            var go = new GameObject($"ItemPickup_{type}");
            go.transform.position = new Vector3(position.x, Mathf.Max(position.y, 0.6f), 0f);
            var pickup = go.AddComponent<ItemPickup>();
            pickup.type = type;

            SiegeAlarmSystem.Post($"전리품 드랍! {HeroGrowth.KoreanName(type)}", new Color(1f, 0.9f, 0.4f, 1f));
            GameFeelVfx.SpawnShockwaveRing(go.transform.position, new Color(1f, 0.9f, 0.4f, 0.6f), 1.2f, 0.4f);
            return go;
        }
    }

    /// <summary>
    /// World loot: bobs in place, any living non-launched unit touching it grants its
    /// side one growth stack. Expires after PickupLifetimeSeconds so stale loot never
    /// clutters the lane.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        public HeroItemType type;

        private float bornAt;
        private Vector3 basePos;
        private SpriteRenderer sr;

        private static readonly Dictionary<HeroItemType, string> ArtKeys = new Dictionary<HeroItemType, string>
        {
            { HeroItemType.Sword, "item_sword" },
            { HeroItemType.Shield, "item_shield" },
            { HeroItemType.Boots, "item_boots" },
        };

        private void Start()
        {
            bornAt = Time.time;
            basePos = transform.position;

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            if (!GimmickSpriteLibrary.TryApply(sr, ArtKeys[type], Color.white))
            {
                sr.sprite = FallbackSprite();
                sr.color = type == HeroItemType.Sword ? new Color(1f, 0.55f, 0.25f, 1f)
                    : type == HeroItemType.Shield ? new Color(0.4f, 0.7f, 1f, 1f)
                    : new Color(0.5f, 1f, 0.55f, 1f);
            }
            if (sr.sprite != null)
            {
                float native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (native > 0.0001f)
                {
                    float s = 0.9f / native; // ~0.9u loot chunk
                    transform.localScale = new Vector3(s, s, 1f);
                }
            }

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = sr.sprite != null ? Mathf.Max(0.35f, sr.sprite.bounds.extents.magnitude) : 0.45f;
        }

        private static Sprite cachedFallback;

        private static Sprite FallbackSprite()
        {
            if (cachedFallback != null) return cachedFallback;
            const int size = 32;
            var tex = new Texture2D(size, size);
            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                    tex.SetPixel(x, y, d < size / 2f - 1 ? Color.white : clear);
                }
            tex.Apply();
            cachedFallback = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return cachedFallback;
        }

        private void Update()
        {
            // Bob + shimmer so the loot reads as interactive.
            transform.position = basePos + new Vector3(0f, Mathf.Sin(Time.time * 3.2f) * 0.18f, 0f);
            if (sr != null)
            {
                var c = sr.color;
                c.a = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
                sr.color = c;
            }

            if (Time.time - bornAt > ItemDropRules.PickupLifetimeSeconds)
            {
                GameFeelVfx.SpawnCollapseDust(transform.position, 0.2f);
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var unit = other.GetComponent<UnitController>();
            if (unit == null || unit.CurrentState == UnitState.Dead || unit.CurrentState == UnitState.Launched) return;

            int stacks = HeroGrowth.Grant(unit.isPlayerUnit, type);
            string side = unit.isPlayerUnit ? "아군" : "적군";
            SiegeAlarmSystem.Post($"{side} 영웅 성장: {HeroGrowth.KoreanName(type)} x{stacks}",
                unit.isPlayerUnit ? new Color(0.55f, 0.95f, 1f, 1f) : new Color(1f, 0.5f, 0.4f, 1f));
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.5f,
                $"{HeroGrowth.KoreanName(type)} +1", new Color(1f, 0.9f, 0.4f, 1f), 2.0f, 0.6f);
            GameFeelVfx.SpawnImpactBurst(transform.position, new Color(1f, 0.9f, 0.4f, 0.8f), 0.5f);
            Destroy(gameObject);
        }
    }
}
