using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// A UI text only draws if a Canvas is somewhere above it. Two of this game's HUD labels —
    /// wind and score — sat at the scene ROOT instead, so they were updated every turn and
    /// rendered never. Nothing caught it: the objects are active, the components exist, the
    /// values are computed, and every code-level test passed. The defect lived in the scene
    /// graph, which no test was reading.
    ///
    /// So this reads the scene file. It is deliberately a text-level assertion rather than a
    /// PlayMode check, because the failure mode is authoring — a label dragged out of the
    /// Canvas in the editor — and that must fail in the fast suite, not only when someone
    /// happens to look at a screenshot.
    /// </summary>
    public class SceneHudRenderabilityTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>Every HUD label that must be visible during a match, by GameObject name.</summary>
        static readonly string[] MustRender = { "WindText", "ScoreText", "TurnText", "TimerText" };

        static string SceneText()
        {
            Assert.IsTrue(File.Exists(ScenePath), $"scene missing at {ScenePath}");
            return File.ReadAllText(ScenePath);
        }

        /// <summary>fileID of the RectTransform belonging to the GameObject with this name.</summary>
        static string RectTransformIdOf(string scene, string goName)
        {
            var goMatch = Regex.Match(scene, @"--- !u!1 &(\d+)\r?\nGameObject:(?:.|\r|\n)*?m_Name: " + Regex.Escape(goName) + @"\r?\n");
            Assert.IsTrue(goMatch.Success, $"no GameObject named {goName} in the scene");
            string goId = goMatch.Groups[1].Value;

            foreach (Match rt in Regex.Matches(scene, @"--- !u!224 &(\d+)\r?\nRectTransform:((?:.|\r|\n)*?)(?=--- !u!|\z)"))
            {
                if (Regex.IsMatch(rt.Groups[2].Value, @"m_GameObject: \{fileID: " + goId + @"\}"))
                    return rt.Groups[1].Value;
            }

            Assert.Fail($"{goName} has no RectTransform");
            return null;
        }

        static string FatherOf(string scene, string rectTransformId)
        {
            var m = Regex.Match(scene,
                @"--- !u!224 &" + rectTransformId + @"\r?\nRectTransform:((?:.|\r|\n)*?)(?=--- !u!|\z)");
            Assert.IsTrue(m.Success, $"RectTransform {rectTransformId} not found");
            var f = Regex.Match(m.Groups[1].Value, @"m_Father: \{fileID: (\d+)\}");
            Assert.IsTrue(f.Success, $"RectTransform {rectTransformId} has no m_Father");
            return f.Groups[1].Value;
        }

        /// <summary>Walks up m_Father until it reaches a transform whose GameObject holds a Canvas.</summary>
        static bool HasCanvasAncestor(string scene, string rectTransformId)
        {
            var canvasOwners = new HashSet<string>();
            foreach (Match c in Regex.Matches(scene, @"--- !u!223 &\d+\r?\nCanvas:((?:.|\r|\n)*?)(?=--- !u!|\z)"))
            {
                var owner = Regex.Match(c.Groups[1].Value, @"m_GameObject: \{fileID: (\d+)\}");
                if (owner.Success) canvasOwners.Add(owner.Groups[1].Value);
            }
            Assert.IsNotEmpty(canvasOwners, "the scene must contain at least one Canvas");

            string current = rectTransformId;
            for (int hops = 0; hops < 32; hops++)
            {
                var block = Regex.Match(scene,
                    @"--- !u!224 &" + current + @"\r?\nRectTransform:((?:.|\r|\n)*?)(?=--- !u!|\z)");
                if (!block.Success) return false;

                var owner = Regex.Match(block.Groups[1].Value, @"m_GameObject: \{fileID: (\d+)\}");
                if (owner.Success && canvasOwners.Contains(owner.Groups[1].Value)) return true;

                string father = FatherOf(scene, current);
                if (father == "0") return false;
                current = father;
            }
            return false;
        }

        [Test]
        public void EveryMatchHudLabel_LivesUnderACanvas()
        {
            string scene = SceneText();
            var orphans = new List<string>();

            foreach (string name in MustRender)
            {
                string rt = RectTransformIdOf(scene, name);
                if (!HasCanvasAncestor(scene, rt)) orphans.Add($"{name} (RectTransform {rt})");
            }

            Assert.IsEmpty(orphans,
                "these HUD labels have no Canvas ancestor, so they are updated but never drawn: "
                + string.Join(", ", orphans));
        }

        /// <summary>
        /// The controls×scene cell of qa/coverage-cross-matrix.md was empty: aiming is asserted
        /// 18 times in pure rules and never once against the scene that wires it. A launch point
        /// dragged loose in the editor kills every shot in the game while leaving all 18 green —
        /// the same shape as the wind/score defect, one concern over.
        /// </summary>
        [Test]
        public void LaunchManager_HasItsLaunchPointWired()
        {
            string scene = SceneText();


            bool found = false;
            foreach (Match mb in Regex.Matches(scene, @"--- !u!114 &\d+\r?\nMonoBehaviour:((?:.|\r|\n)*?)(?=--- !u!|\z)"))
            {
                string body = mb.Groups[1].Value;
                if (!body.Contains("launchPoint:")) continue;
                found = true;

                var lp = Regex.Match(body, @"launchPoint: \{fileID: (-?\d+)\}");
                Assert.IsTrue(lp.Success, "launchPoint field is present but unreadable");
                Assert.AreNotEqual("0", lp.Groups[1].Value,
                    "a launcher in the scene has no launchPoint transform — every shot would "
                    + "spawn at the component's own origin instead of the muzzle");
            }

            Assert.IsTrue(found,
                "no scene component exposes launchPoint; if the launcher moved out of the scene "
                + "this test must be re-pointed rather than deleted");
        }

        [Test]
        public void WindAndScore_AreNotSceneRoots()
        {
            // The exact shape of the original defect, pinned on its own: a root-level UI label.
            // Wind is the largest aiming variable in the game and score is the only running
            // measure of how the match is going; both were invisible for the whole match.
            string scene = SceneText();

            foreach (string name in new[] { "WindText", "ScoreText" })
            {
                string rt = RectTransformIdOf(scene, name);
                Assert.AreNotEqual("0", FatherOf(scene, rt),
                    $"{name} is parented to the scene root — a UI text there renders nothing");
            }
        }
    }
}
