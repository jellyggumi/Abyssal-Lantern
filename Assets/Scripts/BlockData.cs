using System;
using UnityEngine;


namespace CastleBusters
{
    [CreateAssetMenu(fileName = "NewBlockData", menuName = "CastleBusters/Block Data")]
    public class BlockData : ScriptableObject
    {
        public string blockName;
        public float maxHP = 100f;
        public float mass = 1.0f;
        public float friction = 0.5f;
        public float bounciness = 0.05f;

        [Header("Visuals")]
        public Sprite normalSprite;
        public Sprite crackedSprite;
        public Sprite heavilyCrackedSprite;
        public GameObject destructionEffectPrefab;
        public Color blockColor = Color.white;

        // Every block instantiated from this data used to allocate its own PhysicsMaterial2D (one on
        // spawn, another every time it started falling). With dozens of tiles sharing the same
        // friction/bounciness that was pure allocation churn; cache one instance per BlockData asset
        // and hand out the same reference to every block that uses it.
        [NonSerialized] private PhysicsMaterial2D sharedPhysicsMaterial;

        public PhysicsMaterial2D GetSharedPhysicsMaterial()
        {
            if (sharedPhysicsMaterial == null)
            {
                sharedPhysicsMaterial = new PhysicsMaterial2D(string.IsNullOrEmpty(blockName) ? name : blockName)
                {
                    friction = friction,
                    bounciness = bounciness
                };
            }
            return sharedPhysicsMaterial;
        }
    }
}
