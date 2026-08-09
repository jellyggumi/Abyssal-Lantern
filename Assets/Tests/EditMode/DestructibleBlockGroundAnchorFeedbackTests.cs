using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class DestructibleBlockGroundAnchorFeedbackTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameplayUxDirector.ResetSessionStats();
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
            GameplayUxDirector.ResetSessionStats();
        }

        [Test]
        public void DestructibleBlock_DestroyGroundAnchor_DoesNotIncrementSessionMaxCombo()
        {
            EnsureLiveGameplayUxDirector();

            GameObject blockObject = CreateObject("GroundAnchorFeedbackRegressionBlock");
            var block = blockObject.AddComponent<DestructibleBlock>();
            block.isGroundAnchor = true;

            InvokeDestroyBlock(block);

            Assert.That(block == null, Is.True,
                "DestroyBlock must still complete destruction for a ground anchor while suppressing premium feedback.");
            Assert.That(GameplayUxDirector.SessionMaxCombo, Is.Zero,
                "Destroying a ground anchor must not register the structure-break combo owned by premium destruction feedback.");
        }

        private void EnsureLiveGameplayUxDirector()
        {
            if (GameplayUxDirector.Instance == null)
            {
                var director = CreateObject("GroundAnchorFeedbackRegressionUxDirector")
                    .AddComponent<GameplayUxDirector>();
                InvokeNonPublic(director, "Awake");
            }

            Assert.That(GameplayUxDirector.Instance, Is.Not.Null,
                "The regression fixture requires the live NotifyBreak recipient that previously inflated session combo accounting.");
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeDestroyBlock(DestructibleBlock block)
        {
            MethodInfo destroyBlock = typeof(DestructibleBlock).GetMethod(
                "DestroyBlock",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(destroyBlock, Is.Not.Null,
                "The fixture must invoke DestructibleBlock's production destruction path.");
            destroyBlock.Invoke(block, new object[] { null });
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                $"Expected lifecycle method {target.GetType().Name}.{methodName}.");
            method.Invoke(target, null);
        }
    }
}
