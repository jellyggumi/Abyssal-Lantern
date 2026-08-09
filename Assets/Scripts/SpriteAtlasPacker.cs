using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters
{
    public class SpriteAtlasPacker : MonoBehaviour
    {
        public static SpriteAtlasPacker Instance { get; private set; }

        [Header("Sprites to Pack")]
        public List<Sprite> spritesToPack = new List<Sprite>();

        [Header("Atlas Settings")]
        [Tooltip("Square atlas resolution. Source art here is 1254-1536px, so 1024 forces heavy downscale and visible breakup; 2048 keeps detail.")]
        [Range(512, 4096)] public int atlasSize = 2048;
        [Tooltip("Padding in pixels between packed sprites to avoid bleed at high light/zoom.")]
        [Range(0, 16)] public int atlasPadding = 4;

        private Texture2D packedTexture;
        // Primary lookup: keyed by the original Sprite object identity (see PackSprites
        // dedupe fix below) - this is what GetPackedSprite(Sprite) uses and is guaranteed
        // correct even when unit frame sets share bare filenames across folders.
        private readonly Dictionary<Sprite, Sprite> packedSpritesByRef = new Dictionary<Sprite, Sprite>();
        // Secondary, name-keyed lookup kept only for the string overload below (no current
        // caller); NOT reliable when multiple source sprites share a bare name/path suffix.
        private readonly Dictionary<string, Sprite> packedSpritesByName = new Dictionary<string, Sprite>();
        private bool isPacked;

        public bool IsPacked => isPacked;
        public int PackedSpriteCount => packedSpritesByRef.Count;
        public Texture2D PackedTexture => packedTexture;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                PackSprites();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PackSprites()
        {
            if (isPacked) return;
            if (spritesToPack == null || spritesToPack.Count == 0)
            {
                LoadDefaultSprites();
            }

            // BUGFIX (unit-spawn-visual bug): PerfectPixel exports reuse identical bare
            // filenames per state across unit folders (GeneratedUnitFrames/{Knight,Archer,
            // Bomber}/Idle/idle_000.png, walk_000.png, ...). Deduping/keying by sprite.name
            // collapsed every unit's frames onto whichever one packed first alphabetically
            // (Archer), so Knight/Bomber silently rendered Archer's atlas cells regardless of
            // which spawn button was pressed. Dedupe by Sprite object identity instead - each
            // unit's frame is a distinct Sprite asset (distinct GUID/instance) even when the
            // file basename collides.
            var uniqueSprites = new Dictionary<Sprite, Sprite>();
            foreach (var sprite in spritesToPack)
            {
                if (sprite != null && sprite.texture != null && !uniqueSprites.ContainsKey(sprite))
                {
                    uniqueSprites.Add(sprite, sprite);
                }
            }
            spritesToPack = new List<Sprite>(uniqueSprites.Values);


            List<Texture2D> textures = new List<Texture2D>();
            List<Sprite> validSprites = new List<Sprite>();

            foreach (var sprite in spritesToPack)
            {
                if (sprite != null && sprite.texture != null)
                {
                    Texture2D readableTex = MakeTextureReadable(sprite.texture);
                    textures.Add(readableTex);
                    validSprites.Add(sprite);
                }
            }

            if (textures.Count == 0) return;

            int size = Mathf.Clamp(Mathf.ClosestPowerOfTwo(atlasSize), 512, 4096);
            packedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            packedTexture.name = "RuntimePackedSpriteAtlas";
            packedTexture.filterMode = FilterMode.Point;
            packedTexture.wrapMode = TextureWrapMode.Clamp;

            Rect[] rects = packedTexture.PackTextures(textures.ToArray(), atlasPadding, size);
            isPacked = true;

            for (int i = 0; i < validSprites.Count; i++)
            {
                Sprite orig = validSprites[i];
                Rect rect = rects[i];

                Rect pixelRect = new Rect(
                    rect.x * packedTexture.width,
                    rect.y * packedTexture.height,
                    rect.width * packedTexture.width,
                    rect.height * packedTexture.height
                );

                Vector2 pivot = new Vector2(
                    orig.pivot.x / orig.rect.width,
                    orig.pivot.y / orig.rect.height
                );

                Sprite packedSprite = Sprite.Create(packedTexture, pixelRect, pivot, orig.pixelsPerUnit);
                packedSprite.name = orig.name;
                packedSpritesByRef[orig] = packedSprite;
                packedSpritesByName[orig.name] = packedSprite;

            }


        }

        // Gimmicks manage their own presentation-scale + BoxCollider2D sync from the RAW
        // resource sprite (ExplosiveGimmick, CastleCoreGimmick, EventGateGimmick,
        // BuffDebuffGimmick, MovingGimmick, ItemPickup). Sweeping their renderers into the
        // shared atlas and later silently remapping them (ApplyPackedSpritesInScene) shrinks
        // the visible sprite to the packed atlas's cell size while the collider stays sized
        // from the original sprite bounds — collider/gimmick size mismatch. Unlike
        // DestructibleBlock (which proactively calls GetPackedSprite before computing its
        // collider), these never resync after the swap, so they are excluded from packing
        // entirely. ItemPickup is spawned mid-match (after the one-shot ApplyRuntimeSpriteAtlas
        // pass in GameManager.Start already ran) so it isn't hit by this in practice today —
        // excluded anyway so it stays safe if that assumption ever changes.
        private static bool IsGimmickRenderer(SpriteRenderer renderer)
        {
            if (renderer == null) return false;
            var go = renderer.gameObject;
            return go.GetComponent<ExplosiveGimmick>() != null
                || go.GetComponent<CastleCoreGimmick>() != null
                || go.GetComponent<MovingGimmick>() != null
                || go.GetComponent<EventGateGimmick>() != null
                || go.GetComponent<BuffDebuffGimmick>() != null
                || go.GetComponent<EruptionVentGimmick>() != null
                || go.GetComponent<ItemPickup>() != null;
        }

        private void LoadDefaultSprites()
        {
            spritesToPack = new List<Sprite>();

            // 1. Find all sprites referenced by SpriteRenderers in the scene (gimmicks excluded)
            foreach (var renderer in FindObjectsOfType<SpriteRenderer>(true))
            {
                if (renderer != null && renderer.sprite != null && !IsGimmickRenderer(renderer))
                {
                    spritesToPack.Add(renderer.sprite);
                }
            }

            // 2. Load all generated unit frames from Resources (works in editor and standalone)
            Sprite[] resourceSprites = Resources.LoadAll<Sprite>("GeneratedUnitFrames");
            if (resourceSprites != null)
            {
                foreach (var sprite in resourceSprites)
                {
                    if (sprite != null) spritesToPack.Add(sprite);
                }
            }

            // 3. Editor-only fallback to load specific sprites if needed
#if UNITY_EDITOR
            string[] spriteNames =
            {
                "knight", "archer", "bomber",
                "block_normal", "block_cracked", "block_heavily_cracked", "StoneBlock",
                "arrow", "Cannonball", "explosion", "Background"
            };

            foreach (var name in spriteNames)
            {
                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
                if (sprite != null) spritesToPack.Add(sprite);
            }
#endif
        }

        private Texture2D MakeTextureReadable(Texture2D source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                // sRGB readback: source sprites are sRGB. Reading back through a Linear
                // RT double-applies gamma, which shows up as washed/inverted-looking
                // ("뒤집어진") texture breakup once scene lighting brightens the sprite.
                RenderTextureReadWrite.sRGB
            );

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return readableTex;
        }

        public Sprite GetPackedSprite(Sprite original)
        {
            if (original == null) return null;
            if (packedSpritesByRef.TryGetValue(original, out var packed))
            {
                return packed;
            }
            return original;
        }

        public Sprite GetPackedSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (packedSpritesByName.TryGetValue(name, out var packed))
            {
                return packed;
            }
            return null;
        }


        public int ApplyPackedSpritesInScene()
        {
            if (!isPacked) PackSprites();

            int remapped = 0;
            foreach (var renderer in FindObjectsOfType<SpriteRenderer>(true))
            {
                if (renderer == null || renderer.sprite == null || IsGimmickRenderer(renderer)) continue;

                var packed = GetPackedSprite(renderer.sprite);
                if (packed != null && packed != renderer.sprite)
                {
                    renderer.sprite = packed;
                    remapped++;
                }
            }

            return remapped;
        }
    }
}
