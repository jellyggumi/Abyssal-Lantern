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
        public void PristineTurn_StopsAtTheVolleyCap()
        {
            GameManager gameManager = CreateGameManager(4);
            CastleCoreGimmick core = CreateCore();

            // Written against the constants, not against arithmetic from a 150 HP core: the
            // rule is "a pristine turn spends a fixed budget", and it should keep holding when
            // the core's health is retuned.
            core.TakeDamage(CastleCoreGimmick.FullHealthVolleyDamageCap);
            core.TakeDamage(CastleCoreGimmick.FullHealthVolleyDamageCap);

            Assert.That(gameManager.TurnCount, Is.EqualTo(4));
            Assert.That(core.currentHP,
                Is.EqualTo(CastleCoreGimmick.CoreMaxHP - CastleCoreGimmick.FullHealthVolleyDamageCap).Within(0.0001f),
                "same-turn damage to a pristine core must stop at the volley cap, however many hits arrive");
        }

        [Test]
        public void Shield_AbsorbsBeforeHealth()
        {
            GameManager gameManager = CreateGameManager(7);
            CastleCoreGimmick core = CreateCore();

            // Walk the core down to just under its half-health shield threshold, one turn at a
            // time and never past the per-turn cap. A loop rather than a fixed pair of hits
            // because whether that threshold is even reachable in one turn depends on how the
            // cap and the core's health compare — a relationship that has already moved once.
            float justUnderThreshold = CastleCoreGimmick.CoreMaxHP * 0.5f - 1f;
            int turn = 7;
            while (core != null && core.currentHP > CastleCoreGimmick.CoreMaxHP * 0.5f && turn < 20)
            {
                SetPrivateField(gameManager, "turnCount", turn++);
                float remaining = core.currentHP - justUnderThreshold;
                core.TakeDamage(Mathf.Min(remaining, CastleCoreGimmick.FullHealthVolleyDamageCap));
            }

            Assert.That(core != null && core.currentHP > 0f, Is.True,
                "the fixture must leave a live core under the shield threshold");

            SetPrivateField(gameManager, "turnCount", turn);
            float healthBeforeShieldedHit = core.currentHP;
            core.TakeDamage(CastleCoreGimmick.ShieldMaxHP);

            Assert.That(core.currentHP, Is.EqualTo(healthBeforeShieldedHit).Within(0.0001f),
                "a raised shield must absorb its full value before any damage reaches core health");
        }

        [Test]
        public void TurnStartingBelowFullHealth_DoesNotApplyPristineCap()
        {
            GameManager gameManager = CreateGameManager(11);
            CastleCoreGimmick core = CreateCore();

            core.TakeDamage(5f);
            Assert.That(core.currentHP, Is.EqualTo(CastleCoreGimmick.CoreMaxHP - 5f).Within(0.0001f));

            SetPrivateField(gameManager, "turnCount", 12);
            float before = core.currentHP;
            float overCap = CastleCoreGimmick.FullHealthVolleyDamageCap + 20f;
            core.TakeDamage(overCap);

            Assert.That(before - core.currentHP, Is.EqualTo(overCap).Within(0.0001f),
                "a turn that starts below full health must apply all incoming damage, cap included");
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
