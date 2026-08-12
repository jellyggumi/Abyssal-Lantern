using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

// Applied to the whole PlayMode assembly, so every test gets the reset without each fixture
// having to remember to ask for it. Remembering is exactly what failed here: the leaks were
// found one at a time, each after a test had already been chasing the wrong cause.
[assembly: CastleBusters.Tests.ResetSessionState]

namespace CastleBusters.Tests
{
    /// <summary>
    /// Clears the session state that survives a scene reload, before every PlayMode test.
    ///
    /// A siege is built to outlive scene loads — the best-of-three tally has to survive the
    /// reload between games, the cold open must not replay, hero loot carries across a rematch.
    /// All of that lives in statics, and statics outlive tests as readily as they outlive
    /// scenes. The result was a suite whose failures moved when the run composition changed and
    /// moved again when it did not: identical code and an identical filter produced 43/46 and
    /// then 44/46 (D-015). Every one of those investigations started by looking at the failing
    /// assertion, which was never where the problem was.
    ///
    /// The cold-open flag is set to "already shown" rather than cleared. Either value is
    /// deterministic; this one starts each test at the title instead of paying for an intro
    /// beat it is not testing, and the one test that does care about the cold open dismisses
    /// whichever one is playing itself.
    /// </summary>
    public sealed class ResetSessionStateAttribute : TestActionAttribute
    {
        public override ActionTargets Targets => ActionTargets.Test;

        public override void BeforeTest(ITest test)
        {
            HeroGrowth.Reset();                      // loot stacks carried into the next match
            GameplayUxDirector.ResetSessionStats();         // SessionMaxCombo, read back by combo banners
            SiegePrototypeEconomy.ResetDemo();       // marks and the one-time banner entitlement
            StageProgressStore.ResetSessionMirror();   // the session's unlock frontier
            // The first-play coach keys off the editor's real PlayerPrefs: on a fresh
            // profile it would appear in exactly one arbitrary test and hold that test's
            // turn clock. Suppression (not key deletion) keeps the developer's own pref.
            FirstPlayCoachController.SuppressForSession = true;

            InvokeStatic(typeof(GameManager), "ResetSeries");
            SetStaticField(typeof(GameManager), "webtoonIntroShown", true);
        }

        public override void AfterTest(ITest test) { }

        /// <summary>
        /// Reached by reflection on purpose. These are private because nothing in the game has
        /// any business resetting them mid-session — widening them to satisfy a test would
        /// weaken the runtime to fix the harness.
        /// </summary>
        static void InvokeStatic(System.Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, null);
        }

        static void SetStaticField(System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }
    }
}
