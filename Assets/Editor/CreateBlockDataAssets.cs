using UnityEditor;
using UnityEngine;

namespace CastleBusters
{
    public static class CreateBlockDataAssets
    {
        [MenuItem("CastleBusters/Create Block Data Assets")]
        public static void CreateAssets()
        {
            // Load sprites
            var normalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/block_normal.png");
            var crackedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/block_cracked.png");
            var heavilyCrackedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/block_heavily_cracked.png");
            var explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosionEffect.prefab");

            // Create Wood Block Data
            var woodData = ScriptableObject.CreateInstance<BlockData>();
            woodData.blockName = "Wood";
            woodData.maxHP = 30f;
            woodData.mass = 0.5f;
            woodData.friction = 0.6f;
            woodData.bounciness = 0.1f;
            woodData.normalSprite = normalSprite;
            woodData.crackedSprite = crackedSprite;
            woodData.heavilyCrackedSprite = heavilyCrackedSprite;
            woodData.destructionEffectPrefab = explosionEffect;
            woodData.blockColor = new Color(0.8f, 0.5f, 0.2f);
            AssetDatabase.CreateAsset(woodData, "Assets/Settings/WoodBlockData.asset");

            // Create Stone Block Data
            var stoneData = ScriptableObject.CreateInstance<BlockData>();
            stoneData.blockName = "Stone";
            stoneData.maxHP = 70f;
            stoneData.mass = 1.5f;
            stoneData.friction = 0.8f;
            stoneData.bounciness = 0.02f;
            stoneData.normalSprite = normalSprite;
            stoneData.crackedSprite = crackedSprite;
            stoneData.heavilyCrackedSprite = heavilyCrackedSprite;
            stoneData.destructionEffectPrefab = explosionEffect;
            stoneData.blockColor = new Color(0.6f, 0.6f, 0.6f);
            AssetDatabase.CreateAsset(stoneData, "Assets/Settings/StoneBlockData.asset");

            // Create Iron Block Data
            var ironData = ScriptableObject.CreateInstance<BlockData>();
            ironData.blockName = "Iron";
            ironData.maxHP = 150f;
            ironData.mass = 3.0f;
            ironData.friction = 0.4f;
            ironData.bounciness = 0.01f;
            ironData.normalSprite = normalSprite;
            ironData.crackedSprite = crackedSprite;
            ironData.heavilyCrackedSprite = heavilyCrackedSprite;
            ironData.destructionEffectPrefab = explosionEffect;
            ironData.blockColor = new Color(0.3f, 0.4f, 0.5f);
            AssetDatabase.CreateAsset(ironData, "Assets/Settings/IronBlockData.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Block Data Assets created successfully!");
        }
    }
}
