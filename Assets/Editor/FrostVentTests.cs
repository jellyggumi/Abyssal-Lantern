using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the Frost eruption-vent variant (Frostbound Gorge / Stage3):
    /// stage-aware vent scheduling and the Frost gimmick's construction-time safety.
    /// </summary>
    public class FrostVentTests
    {
        // ---- Stage1: StyleForTurn(turn, stage) matches legacy two-way rotation ----

        [Test]
        public void StyleForTurn_Stage1_MatchesLegacyTwoWayRotation()
        {
            Assert.AreEqual(VentSchedule.StyleForTurn(2), VentSchedule.StyleForTurn(2, StageId.Stage1));
            Assert.AreEqual(VentSchedule.StyleForTurn(5), VentSchedule.StyleForTurn(5, StageId.Stage1));
            Assert.AreEqual(VentSchedule.StyleForTurn(8), VentSchedule.StyleForTurn(8, StageId.Stage1));
            Assert.AreEqual(VentSchedule.StyleForTurn(11), VentSchedule.StyleForTurn(11, StageId.Stage1));
        }

        // ---- Stage2: Magma only ----

        [Test]
        public void StyleForTurn_Stage2_IsAlwaysMagma()
        {
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(2, StageId.Stage2));
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(5, StageId.Stage2));
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(8, StageId.Stage2));
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(11, StageId.Stage2));
        }

        // ---- Stage3: Frost only ----

        [Test]
        public void StyleForTurn_Stage3_IsAlwaysFrost()
        {
            Assert.AreEqual(EruptionStyle.Frost, VentSchedule.StyleForTurn(2, StageId.Stage3));
            Assert.AreEqual(EruptionStyle.Frost, VentSchedule.StyleForTurn(5, StageId.Stage3));
            Assert.AreEqual(EruptionStyle.Frost, VentSchedule.StyleForTurn(8, StageId.Stage3));
            Assert.AreEqual(EruptionStyle.Frost, VentSchedule.StyleForTurn(11, StageId.Stage3));
        }

        // ---- Live-object construction: Frost style must not throw and must start Dormant ----

        [Test]
        public void EruptionVentGimmick_FrostStyle_ConstructsWithoutThrowing_AndStartsDormant()
        {
            var go = new GameObject("FrostVent");
            EruptionVentGimmick vent = null;

            Assert.DoesNotThrow(() => vent = go.AddComponent<EruptionVentGimmick>());
            vent.style = EruptionStyle.Frost;

            Assert.AreEqual(EruptionVentGimmick.Phase.Dormant, vent.CurrentPhase);

            Object.DestroyImmediate(go);
        }
    }
}
