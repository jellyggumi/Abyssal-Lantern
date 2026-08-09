using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters
{
    public static class SetupSceneLayout
    {
        [MenuItem("CastleBusters/Setup Scene Layout")]
        public static void Setup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            // 1. Create/Load ExplosiveBarrel Prefab
            var barrelPrefab = CreateExplosiveBarrelPrefab();

            // 2. Assign to GameManager
            var gameManager = Object.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.explosiveBarrelPrefab = barrelPrefab;
                EditorUtility.SetDirty(gameManager);
                Debug.Log("Assigned ExplosiveBarrel prefab to GameManager.");
            }

            // 3. Load BlockData assets
            var woodData = AssetDatabase.LoadAssetAtPath<BlockData>("Assets/Settings/WoodBlockData.asset");
            var stoneData = AssetDatabase.LoadAssetAtPath<BlockData>("Assets/Settings/StoneBlockData.asset");
            var ironData = AssetDatabase.LoadAssetAtPath<BlockData>("Assets/Settings/IronBlockData.asset");

            if (woodData == null || stoneData == null || ironData == null)
            {
                Debug.LogError("Failed to load BlockData assets! Run CreateBlockDataAssets first.");
                return;
            }

            // 4. Clean up duplicate blocks and assign BlockData / Gimmicks
            var playerCastle = GameObject.Find("PlayerCastle");
            var enemyCastle = GameObject.Find("EnemyCastle");

            if (playerCastle != null)
            {
                ProcessCastle(playerCastle, woodData, stoneData, ironData, barrelPrefab);
            }
            if (enemyCastle != null)
            {
                ProcessCastle(enemyCastle, woodData, stoneData, ironData, barrelPrefab);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Scene layout setup completed successfully!");
        }

        private static GameObject CreateExplosiveBarrelPrefab()
        {
            string prefabPath = "Assets/Prefabs/ExplosiveBarrel.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            // Create temporary GameObject
            var go = new GameObject("ExplosiveBarrel");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            var normalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/block_normal.png");
            spriteRenderer.sprite = normalSprite;
            spriteRenderer.color = new Color(1.0f, 0.3f, 0.2f); // Red-orange color for TNT

            var col = go.AddComponent<BoxCollider2D>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.8f;

            var gimmick = go.AddComponent<ExplosiveGimmick>();
            gimmick.explosionRadius = 2.2f;
            gimmick.explosionDamage = 80f;
            gimmick.explosionEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosionEffect.prefab");

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("Created ExplosiveBarrel prefab.");
            return prefab;
        }

        private static void ProcessCastle(GameObject castle, BlockData wood, BlockData stone, BlockData iron, GameObject barrelPrefab)
        {
            var blocks = castle.GetComponentsInChildren<DestructibleBlock>(true);
            var uniqueBlocks = new Dictionary<Vector3, DestructibleBlock>();
            var toDestroy = new List<GameObject>();

            foreach (var block in blocks)
            {
                Vector3 pos = block.transform.position;
                // Round position to avoid floating point precision issues
                Vector3 roundedPos = new Vector3(Mathf.Round(pos.x * 100f) / 100f, Mathf.Round(pos.y * 100f) / 100f, Mathf.Round(pos.z * 100f) / 100f);

                if (uniqueBlocks.ContainsKey(roundedPos))
                {                    toDestroy.Add(block.gameObject);
                }
                else
                {                    uniqueBlocks.Add(roundedPos, block);
                }
            }

            // Destroy duplicates
            foreach (var go in toDestroy)
            {
                Object.DestroyImmediate(go);
            }

            // Process unique blocks
            foreach (var kvp in uniqueBlocks)
            {
                var block = kvp.Value;
                float y = kvp.Key.y;
                float x = kvp.Key.x;

                // Check if this is the center block (Y = 1.5, and X is middle of the castle)
                // Player Castle X: -8, -7, -6 -> center is -7
                // Enemy Castle X: 6, 7, 8 -> center is 7
                bool isCenter = Mathf.Abs(y - 1.5f) < 0.1f && (Mathf.Abs(x - (-7.0f)) < 0.1f || Mathf.Abs(x - 7.0f) < 0.1f);

                if (isCenter)
                {
                    // Replace with Explosive Barrel
                    Vector3 pos = block.transform.position;
                    Transform parent = block.transform.parent;
                    Object.DestroyImmediate(block.gameObject);

                    var barrel = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab);
                    barrel.name = "ExplosiveBarrel";
                    barrel.transform.position = pos;
                    barrel.transform.SetParent(parent);
                    Undo.RegisterCreatedObjectUndo(barrel, "Create Explosive Barrel");
                }
                else
                {
                    // Assign BlockData based on height
                    if (y < 1.0f)
                    {
                        block.blockData = iron;
                    }
                    else if (y < 2.0f)
                    {
                        block.blockData = stone;
                    }
                    else
                    {
                        block.blockData = wood;
                    }
                    EditorUtility.SetDirty(block);
                }
            }
        }
    }
}
