using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class CastleCoreVolleyCapTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private Random.State previousRandomState;

        [SetUp]
        public void SetUp()
        {
            previousRandomState = Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            Random.state = previousRandomState;
        }

        [Test]
        public void CastleCoreGimmick_MultipleSameTurnHits_CapPristineCoreAtTenHealth()
        {
            GameManager gameManager = CreateGameManager(4);
            CastleCoreGimmick core = CreateCore();

            core.TakeDamage(100f);
            core.TakeDamage(100f);

            Assert.That(gameManager.TurnCount, Is.EqualTo(4));
            Assert.That(core.currentHP, Is.EqualTo(10f).Within(0.0001f),
                "Same-turn health damage to a pristine 150 HP core must stop at the 140-point volley cap after shield absorption.");
        }

        [Test]
        public void CastleCoreGimmick_NextTurnDamage_ConsumesShieldAndDefeatsCore()
        {
            GameManager gameManager = CreateGameManager(7);
            CastleCoreGimmick core = CreateCore();

            core.TakeDamage(100f);
            Assert.That(core.currentHP, Is.EqualTo(50f).Within(0.0001f),
                "The opening hit must cross the shield threshold with 50 core health remaining.");

            SetPrivateField(gameManager, "turnCount", 8);
            core.TakeDamage(99f);

            Assert.That(core.currentHP, Is.EqualTo(1f).Within(0.0001f),
                "On the next turn, the activated 50-point shield must absorb first and the remaining 49 damage must reach core health.");

            core.TakeDamage(1f);
            Assert.That(core == null, Is.True,
                "After the next-turn shield is consumed, the final point of public damage must defeat the core.");
        }

        [Test]
        public void CastleCoreGimmick_TurnStartingBelowFullHealth_DoesNotApplyPristineCap()
        {
            GameManager gameManager = CreateGameManager(11);
            CastleCoreGimmick core = CreateCore();

            core.TakeDamage(5f);
            Assert.That(core.currentHP, Is.EqualTo(145f).Within(0.0001f));

            SetPrivateField(gameManager, "turnCount", 12);
            core.TakeDamage(144f);

            Assert.That(core.currentHP, Is.EqualTo(1f).Within(0.0001f),
                "A turn that starts below full health must apply all incoming health damage instead of imposing the pristine-core cap.");

            core.TakeDamage(51f);
            Assert.That(core == null, Is.True,
                "A previously damaged core must remain defeatable after the activated 50-point shield is consumed.");
        }

        private GameManager CreateGameManager(int turnCount)
        {
            var gameManager = CreateObject("CastleCoreVolleyCap_GameManager").AddComponent<GameManager>();
            InvokeAwake(gameManager);
            SetPrivateField(gameManager, "turnCount", turnCount);
            return gameManager;
        }

        private CastleCoreGimmick CreateCore()
        {
            var core = CreateObject("CastleCoreVolleyCap_Core").AddComponent<CastleCoreGimmick>();
            InvokeAwake(core);
            return core;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeAwake(MonoBehaviour component)
        {
            MethodInfo awake = component.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null,
                $"The EditMode fixture must execute {component.GetType().Name}.Awake through the production lifecycle path.");
            awake.Invoke(component, null);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Expected private field {target.GetType().Name}.{fieldName}.");
            field.SetValue(target, value);
        }
    }
}
