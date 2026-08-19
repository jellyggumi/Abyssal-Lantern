using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters
{
    public class DebrisFragment : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 velocity;
        private float gravity = -9.81f;
        private float torque;
        private float lifetime;
        private float elapsed;
        private Vector3 originalScale;
        private Color baseColor;

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 35;
        }

        public void Initialize(Sprite sprite, Vector3 position, Vector3 startVelocity, float startTorque, float startLifetime, Color color, Vector3 scale)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            baseColor = color;
            transform.position = position;
            velocity = startVelocity;
            torque = startTorque;
            lifetime = startLifetime;
            elapsed = 0f;
            originalScale = scale;
            transform.localScale = scale;
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            gameObject.SetActive(true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                gameObject.SetActive(false);
                if (DebrisPool.Instance != null)
                {
                    DebrisPool.Instance.ReturnDebris(this);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;

            }

            float t = elapsed / lifetime;
            velocity.y += gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, torque * Time.deltaTime);

            // Playtest note: a plain Lerp(1,0,t) alpha fade made every fragment look
            // "washed out" from the very first frame, since it starts losing opacity
            // immediately. Hold full opacity through the first ~55% of life (debris reads
            // as solid, physical chunks while airborne) then fade quickly over the tail,
            // which reads as "settling into dust" instead of a uniform fog. Scale shrink
            // uses an ease-in curve (t^2) so it stays chunky in flight and only collapses
            // fast right at the very end, matching the snappier feel of a real fragment
            // burning up / crumbling rather than smoothly deflating the whole time.
            float fadeStart = 0.55f;
            float alpha = t <= fadeStart ? 1f : Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            float shrinkT = t * t;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, shrinkT);

        }
    }

    public class DebrisPool : MonoBehaviour
    {
        public static DebrisPool Instance { get; private set; }

        private List<Sprite> fragmentSprites = new List<Sprite>();
        private Queue<DebrisFragment> pool = new Queue<DebrisFragment>();
        private GameObject poolContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GenerateFragmentSprites();
            PrewarmPool(40);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void GenerateFragmentSprites()
        {
            // Four authored chunks with lit and shadowed faces, falling back to the procedural
            // shards below. The procedural path was never circles — it builds a random 4-6 vertex
            // convex polygon with an anti-aliased edge — but every pixel inside it is pure white,
            // so a chunk reads as a flat silhouette that the tint colours uniformly. The authored
            // art carries its own internal shading, which is what makes a fragment read as a piece
            // of masonry with a near and far face instead of a paper cut-out.
            //
            // Kept at 8 sprites: the pool indexes into this list and 8 shared textures was already
            // the measured memory decision. Four authored chunks fill it twice.
            var authored = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                var chunk = Resources.Load<Sprite>($"Effects/debris_chunk_{i:00}");
                if (chunk != null) authored.Add(chunk);
            }

            if (authored.Count > 0)
            {
                for (int i = 0; i < 8; i++) fragmentSprites.Add(authored[i % authored.Count]);
                return;
            }

            // Anti-aliased debris shards spawned on every hit/collapse/break - i.e. the actual visible
            // "breaking apart" effect during an explosion. Bumped from 128px (no mipmaps) to 192px with
            // mipmaps + trilinear filtering: fragments shrink to ~15-35% scale and fade out over their
            // lifetime, and without mip data that minification was the main source of shimmering/aliasing
            // during a breakup. Only 8 shared textures total, so the memory cost is negligible.
            const int fragmentRes = 192;
            for (int i = 0; i < 8; i++)
            {
                Texture2D tex = GenerateFragmentTexture(fragmentRes, fragmentRes);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, fragmentRes, fragmentRes), new Vector2(0.5f, 0.5f), fragmentRes);
                sprite.name = $"AntiAliasedFragment_{i}";
                fragmentSprites.Add(sprite);
            }
        }


        private Texture2D GenerateFragmentTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.filterMode = FilterMode.Trilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 4;


            // Generate a random convex polygon
            int numVertices = Random.Range(4, 7);
            Vector2 centroid = new Vector2(width / 2f, height / 2f);
            List<float> angles = new List<float>();
            for (int i = 0; i < numVertices; i++)
            {
                angles.Add(Random.Range(0f, Mathf.PI * 2f));
            }
            angles.Sort();

            Vector2[] vertices = new Vector2[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                float r = Random.Range(width * 0.15f, width * 0.4f);
                vertices[i] = centroid + new Vector2(Mathf.Cos(angles[i]) * r, Mathf.Sin(angles[i]) * r);
            }

            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = GetSignedDistanceToPolygon(p, vertices);
                    if (d > 0f)
                    {
                        // Anti-aliasing: smooth the edge over 1.5 pixels
                        float alpha = Mathf.Clamp01(d / 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply(true, true);

            return tex;
        }

        private float GetSignedDistanceToPolygon(Vector2 p, Vector2[] vertices)
        {
            int n = vertices.Length;
            float minDistSq = float.MaxValue;
            bool inside = true;
            float firstSign = 0f;

            for (int i = 0; i < n; i++)
            {
                Vector2 a = vertices[i];
                Vector2 b = vertices[(i + 1) % n];
                Vector2 v = b - a;
                Vector2 w = p - a;
                float t = Mathf.Clamp01(Vector2.Dot(w, v) / Vector2.Dot(v, v));
                Vector2 c = a + t * v;
                float distSq = (p - c).sqrMagnitude;
                if (distSq < minDistSq) minDistSq = distSq;

                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (i == 0)
                {
                    firstSign = Mathf.Sign(cross);
                }
                else if (Mathf.Sign(cross) != firstSign && cross != 0f)
                {
                    inside = false;
                }
            }

            float dist = Mathf.Sqrt(minDistSq);
            return inside ? dist : -dist;
        }

        private void PrewarmPool(int count)
        {
            poolContainer = new GameObject("DebrisPoolContainer");
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("DebrisFragment");
                go.transform.SetParent(poolContainer.transform);
                var fragment = go.AddComponent<DebrisFragment>();
                go.SetActive(false);
                pool.Enqueue(fragment);
            }
        }

        public DebrisFragment GetDebris()
        {
            if (pool.Count > 0)
            {
                var fragment = pool.Dequeue();
                return fragment;
            }
            else
            {
                var go = new GameObject("DebrisFragment");
                if (poolContainer != null) go.transform.SetParent(poolContainer.transform);
                return go.AddComponent<DebrisFragment>();
            }
        }

        public void ReturnDebris(DebrisFragment fragment)
        {
            if (poolContainer != null) fragment.transform.SetParent(poolContainer.transform);
            pool.Enqueue(fragment);
        }

        public void SpawnDebrisBurst(Vector3 position, Color color, int count = 8)
        {
            if (fragmentSprites.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var fragment = GetDebris();
                Sprite sprite = fragmentSprites[Random.Range(0, fragmentSprites.Count)];
                Vector3 startVelocity = new Vector3(Random.Range(-4f, 4f), Random.Range(2f, 8f), 0f);
                float startTorque = Random.Range(-180f, 180f);
                float startLifetime = Random.Range(0.6f, 1.2f);
                float scaleFactor = Random.Range(0.15f, 0.35f);
                Vector3 scale = new Vector3(scaleFactor, scaleFactor, 1f);

                fragment.Initialize(sprite, position, startVelocity, startTorque, startLifetime, color, scale);
            }
        }
    }
}
