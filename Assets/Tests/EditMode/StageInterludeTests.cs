using System;
using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the narrative cutscene layer the campaign now cuts through: the per-stage entry
    /// beats, the epilogue, the playback timeline they are read back on, and the typewriter
    /// that reveals them. Every failure here is something a player sees — a stage that hard-
    /// cuts onto the battlefield over black, two acts narrating the same words, a beat no
    /// elapsed value ever selects, a reveal that never finishes, or a cutscene that never
    /// reports itself done and so never hands control back.
    ///
    /// Scope: the pure static <see cref="StageInterlude"/> and the plain-C#
    /// <see cref="NarrativeTypewriter"/>. StageInterludeController is deliberately never
    /// constructed — it builds a Canvas and samples Time.unscaledTime — and neither is
    /// GameManager, which reaches into scene singletons. Everything below is arithmetic over
    /// data, which is exactly why it belongs in EditMode.
    ///
    /// The typewriter's Unicode text-element contract (emoji and combining marks are revealed
    /// whole, never split) is already pinned by
    /// MobileNarrativeCommerceTests.NarrativeTypewriter_RevealsWholeEmojiAndCombiningTextElementsBeforeCompleting
    /// and is not repeated here. This fixture covers the reveal properties that test does not:
    /// the empty start, the prefix invariant across the whole walk, the exact landing on
    /// FullText, the ignored non-advancing frame, the skip, and the misconfigured speed.
    /// </summary>
    [TestFixture]
    public sealed class StageInterludeTests
    {
        /// <summary>Slack for timeline arithmetic. Script runtimes sit around ten seconds, where
        /// a float's own resolution is ~1e-6s, so a millisecond is loose enough to absorb
        /// accumulation order and tight enough that a real boundary bug still shows.</summary>
        private const float TimeTolerance = 1e-3f;

        /// <summary>Slack for summed durations, which are accumulated in the same order the
        /// implementation accumulates them.</summary>
        private const float DurationTolerance = 1e-4f;

        /// <summary>How far before a script's runtime "not finished yet" is sampled. Four orders
        /// of magnitude above float resolution at these magnitudes, so it is a genuine
        /// before-the-end instant rather than a rounding artefact.</summary>
        private const float NearlyDoneMargin = 0.01f;

        /// <summary>Oversampling factor for timeline sweeps: the step is the SHORTEST line's
        /// duration divided by this, so every line — including the briefest — is guaranteed at
        /// least this many samples and a sweep can never step over one. Nothing here hard-codes
        /// a step in seconds; add a terser line to a script and the sweep tightens with it.</summary>
        private const int SweepSamplesPerShortestLine = 8;

        /// <summary>Separator used only to flatten a script's beats into one comparable string.
        /// A control character, so it cannot occur inside prose and make two genuinely different
        /// line splits compare equal.</summary>
        private const string LineJoin = "\u0001";

        // ---- helpers -------------------------------------------------------------------

        private static StageId[] AllStages()
        {
            return (StageId[])Enum.GetValues(typeof(StageId));
        }

        /// <summary>Every script the campaign can actually play: one entry beat per stage plus
        /// the closer. Enumerated rather than listed, so a stage added to the table is covered
        /// by these rules the moment it exists instead of the day someone remembers.</summary>
        private static InterludeScript[] AllScripts()
        {
            var stages = AllStages();
            var scripts = new InterludeScript[stages.Length + 1];
            for (int i = 0; i < stages.Length; i++) scripts[i] = StageInterlude.ForStageEntry(stages[i]);
            scripts[stages.Length] = StageInterlude.Epilogue();
            return scripts;
        }

        private static string Label(InterludeScript script)
        {
            return $"the {script.kind} cutscene \"{script.heading}\"";
        }

        /// <summary>Elapsed seconds at which line <paramref name="index"/> takes the screen.
        /// Accumulated in the same order StageInterlude walks its own cursor, so the value
        /// returned is the exact float the implementation compares against rather than a
        /// separately-rounded approximation of it. Passing Lines.Length yields the instant the
        /// last line hands over to the tail fade.</summary>
        private static float LineStartSeconds(InterludeScript script, int index)
        {
            float cursor = StageInterlude.PreRollSeconds;
            for (int i = 0; i < index; i++) cursor += StageInterlude.LineDurationSeconds(script.Lines[i].text);
            return cursor;
        }

        private static float LinesEndSeconds(InterludeScript script)
        {
            return LineStartSeconds(script, script.Lines.Length);
        }

        private static float ShortestLineDurationSeconds(InterludeScript script)
        {
            float shortest = float.MaxValue;
            for (int i = 0; i < script.Lines.Length; i++)
                shortest = Mathf.Min(shortest, StageInterlude.LineDurationSeconds(script.Lines[i].text));
            return shortest;
        }

        private static float SweepStepSeconds(InterludeScript script)
        {
            return ShortestLineDurationSeconds(script) / SweepSamplesPerShortestLine;
        }

        private static string ScriptText(InterludeScript script)
        {
            var texts = new string[script.Lines.Length];
            for (int i = 0; i < texts.Length; i++) texts[i] = script.Lines[i].text;
            return string.Join(LineJoin, texts);
        }

        /// <summary>A real beat out of the shipped opening-stage script, so the reveal is
        /// exercised on prose a player actually reads rather than on a fixture string. Hangul
        /// syllables and ASCII punctuation are one UTF-16 unit each, with no surrogate pairs or
        /// combining marks, so <c>text.Length</c> is this line's text-element count — the split
        /// text-element cases live in MobileNarrativeCommerceTests.</summary>
        private static string ShippedNarrationLine()
        {
            return StageInterlude.ForStageEntry(StageId.Stage1).Lines[0].text;
        }

        // 1 -------------------------------------------------------------------------------

        [Test]
        public void EveryStage_EntersOnACutsceneWithRealLinesInsteadOfABlankCut()
        {
            foreach (var stage in AllStages())
            {
                var script = StageInterlude.ForStageEntry(stage);

                Assert.IsTrue(script.HasContent,
                    $"{stage} has no entry cutscene at all, so the campaign hard-cuts from the results screen straight onto the battlefield over a black screen");
                Assert.IsFalse(string.IsNullOrWhiteSpace(script.heading),
                    $"{stage}'s cutscene opens with no title card, leaving the player reading unattributed prose with no statement of where the campaign now is");
                Assert.GreaterOrEqual(script.Lines.Length, 2,
                    $"{stage}'s cutscene is a single beat, which flashes past as a caption instead of selling the scene change it exists to sell");

                for (int i = 0; i < script.Lines.Length; i++)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(script.Lines[i].text),
                        $"beat {i} of {stage}'s cutscene is blank, so the surface holds an empty text box for that beat's full runtime before moving on");
                }
            }
        }

        // 2 -------------------------------------------------------------------------------

        [Test]
        public void TheStageEntryCutscenes_AreDistinctBeatsRatherThanOneRepeatedOne()
        {
            var stages = AllStages();

            for (int a = 0; a < stages.Length; a++)
            {
                for (int b = a + 1; b < stages.Length; b++)
                {
                    var first = StageInterlude.ForStageEntry(stages[a]);
                    var second = StageInterlude.ForStageEntry(stages[b]);

                    Assert.AreNotEqual(first.heading, second.heading,
                        $"{stages[a]} and {stages[b]} announce themselves with the same title card, so the player is told twice that they have arrived in the same place");
                    Assert.AreNotEqual(ScriptText(first), ScriptText(second),
                        $"{stages[a]} and {stages[b]} narrate word for word the same beat, which is precisely the \"same fight again, different rocks\" reading these interludes exist to break");
                }
            }
        }

        // 3 -------------------------------------------------------------------------------

        [Test]
        public void TheCampaignScripts_MixImpersonalNarrationWithAttributedCharacterVoice()
        {
            bool sawNarration = false;
            bool sawSpeaker = false;

            foreach (var script in AllScripts())
            {
                foreach (var line in script.Lines)
                {
                    sawNarration |= line.IsNarration;
                    sawSpeaker |= line.speaker != null;

                    Assert.AreEqual(line.speaker == null, line.IsNarration,
                        $"a beat in {Label(script)} disagrees with itself about whether it is narration; the surface picks between the plain prose layout and the speaker name plate off IsNarration alone, so a mismatch prints attributed dialogue with nobody's name on it or narration under a blank plate");
                }
            }

            Assert.IsTrue(sawNarration,
                "not one beat in the whole campaign is impersonal narration, so every cutscene line is somebody talking and the scene never gets to describe where the player has arrived");
            Assert.IsTrue(sawSpeaker,
                "not one beat in the whole campaign is attributed to a character, so the cutscenes are wall-to-wall narration and the cast never speaks");
        }

        // 4 -------------------------------------------------------------------------------

        [Test]
        public void TheTimeline_WalksForwardThroughEveryBeatWithoutSkippingOrRewinding()
        {
            foreach (var script in AllScripts())
            {
                string label = Label(script);

                Assert.AreEqual(-1, StageInterlude.LineIndexAt(script, 0f),
                    $"{label} puts its first line up on the very frame of the cut; the black pre-roll is what stops the scene change from landing as a glitch");
                Assert.AreEqual(-1, StageInterlude.LineIndexAt(script, StageInterlude.PreRollSeconds - TimeTolerance),
                    $"{label} starts its first line while the screen is still inside the opening hold, so the line fades up against a backdrop that has not arrived yet");
                Assert.AreEqual(0, StageInterlude.LineIndexAt(script, StageInterlude.PreRollSeconds),
                    $"{label} is still showing nothing at the instant the opening hold ends, so its first beat loses screen time it was written to have");

                float total = StageInterlude.TotalDurationSeconds(script);
                float step = SweepStepSeconds(script);
                int sampleCount = Mathf.CeilToInt(total / step);

                var seen = new HashSet<int>();
                int previousIndex = int.MinValue;

                for (int s = 0; s <= sampleCount; s++)
                {
                    // Multiplied, never accumulated, so the sweep itself cannot drift off the
                    // boundaries it is probing.
                    float elapsed = s * step;
                    int index = StageInterlude.LineIndexAt(script, elapsed);

                    Assert.GreaterOrEqual(index, previousIndex,
                        $"at {elapsed:F3}s {label} drops back to beat {index} after already showing beat {previousIndex}, so the player watches a line they have finished reading replay mid-scene");

                    previousIndex = index;
                    if (index >= 0) seen.Add(index);
                }

                for (int i = 0; i < script.Lines.Length; i++)
                {
                    Assert.IsTrue(seen.Contains(i),
                        $"no instant on {label}'s timeline ever selects beat {i}, so that line is written, paid for in the cutscene's runtime, and never once shown to the player");
                }
            }
        }

        // 5 -------------------------------------------------------------------------------

        [Test]
        public void TimeInLine_RestartsOnEveryBeatAndStaysInsideTheBeatItBelongsTo()
        {
            foreach (var script in AllScripts())
            {
                string label = Label(script);

                for (int i = 0; i < script.Lines.Length; i++)
                {
                    float lineStart = LineStartSeconds(script, i);
                    float lineDuration = StageInterlude.LineDurationSeconds(script.Lines[i].text);

                    Assert.AreEqual(i, StageInterlude.LineIndexAt(script, lineStart),
                        $"guard: {label} is not on beat {i} at {lineStart:F3}s, so the rest of this case would be measuring the wrong line");
                    Assert.AreEqual(0f, StageInterlude.TimeInLine(script, lineStart), TimeTolerance,
                        $"beat {i} of {label} arrives with time already on its clock, so the typewriter opens that line part-way through and the player never sees its first characters typed");

                    float midway = lineStart + lineDuration * 0.5f;
                    Assert.Greater(StageInterlude.TimeInLine(script, midway), StageInterlude.TimeInLine(script, lineStart),
                        $"the clock on beat {i} of {label} does not move while the line is on screen, so the typewriter it drives freezes on the first character for the whole beat");
                }

                // Sweep only the stretch the lines themselves own. The tail deliberately holds
                // the final frame past its line's duration while the scene fades out, so that
                // overhang is the design, not a leak.
                float linesEnd = LinesEndSeconds(script);
                float step = SweepStepSeconds(script);
                int sampleCount = Mathf.CeilToInt((linesEnd - StageInterlude.PreRollSeconds) / step);

                for (int s = 0; s <= sampleCount; s++)
                {
                    float elapsed = StageInterlude.PreRollSeconds + s * step;
                    if (elapsed >= linesEnd) break;

                    int index = StageInterlude.LineIndexAt(script, elapsed);
                    float timeInLine = StageInterlude.TimeInLine(script, elapsed);
                    float lineDuration = StageInterlude.LineDurationSeconds(script.Lines[index].text);

                    Assert.GreaterOrEqual(timeInLine, 0f,
                        $"at {elapsed:F3}s {label} reports negative time on beat {index}, which winds the typewriter backwards and blanks a line that is already on screen");
                    Assert.Less(timeInLine, lineDuration + TimeTolerance,
                        $"at {elapsed:F3}s {label} reports {timeInLine:F3}s spent on beat {index}, past that beat's own {lineDuration:F3}s — the clock has leaked in from the previous line, so the typewriter runs off the end of the text it is revealing");
                }
            }
        }

        // 6 -------------------------------------------------------------------------------

        [Test]
        public void ACutscenesRuntime_IsTheOpeningHoldTheTailAndTheBeatsItActuallyPlays()
        {
            foreach (var script in AllScripts())
            {
                float summedBeats = 0f;
                for (int i = 0; i < script.Lines.Length; i++)
                    summedBeats += StageInterlude.LineDurationSeconds(script.Lines[i].text);

                float expected = StageInterlude.PreRollSeconds + StageInterlude.TailSeconds + summedBeats;

                Assert.AreEqual(expected, StageInterlude.TotalDurationSeconds(script), DurationTolerance,
                    $"{Label(script)} budgets a runtime that does not match the beats it plays — short, and the last line is cut off mid-reveal; long, and the player sits on black waiting for a scene that already finished");
            }
        }

        [Test]
        public void ALongerBeat_BuysStrictlyMoreScreenTimeThanAShorterOne()
        {
            string shortest = null;
            string longest = null;

            foreach (var script in AllScripts())
            {
                foreach (var line in script.Lines)
                {
                    if (shortest == null || line.text.Length < shortest.Length) shortest = line.text;
                    if (longest == null || line.text.Length > longest.Length) longest = line.text;
                }
            }

            Assert.Greater(longest.Length, shortest.Length,
                "guard: every shipped beat is the same length, so this case cannot tell whether more prose earns more time");
            Assert.Greater(StageInterlude.LineDurationSeconds(longest), StageInterlude.LineDurationSeconds(shortest),
                "the campaign's longest beat is given no more time than its shortest, so the player is handed more words inside the same window and the line cuts away before it can be read");
        }

        // 7 -------------------------------------------------------------------------------

        [Test]
        public void ACutscene_FinishesAtItsOwnRuntimeAndStaysFinished()
        {
            foreach (var script in AllScripts())
            {
                string label = Label(script);
                float total = StageInterlude.TotalDurationSeconds(script);

                Assert.IsFalse(StageInterlude.IsComplete(script, total - NearlyDoneMargin),
                    $"{label} reports itself finished while the tail fade is still running, so control snaps back to the battlefield over a cutscene that is still on screen");
                Assert.IsTrue(StageInterlude.IsComplete(script, total),
                    $"{label} never reports itself finished, so the controller never hands control back and the game hangs on the cutscene with no way out for the player");
                Assert.IsTrue(StageInterlude.IsComplete(script, total + 60f),
                    $"{label} un-finishes itself once elapsed runs well past its runtime, so a dropped frame or a backgrounded app resumes into a cutscene the player already sat through");
            }
        }

        // 8 -------------------------------------------------------------------------------

        [Test]
        public void TheEntryCutscene_PlaysOnlyWhenTheCampaignActuallyMovedForwardOnAClear()
        {
            Assert.IsTrue(StageInterlude.ShouldPlayOnEntry(StageId.Stage2, StageId.Stage1, advancedFromClear: true),
                "clearing a stage and moving to the next one plays no interlude, so the campaign goes back to cutting from the results screen straight into the next battlefield");
            Assert.IsTrue(StageInterlude.ShouldPlayOnEntry(StageId.Stage3, StageId.Stage2, advancedFromClear: true),
                "the last leg of the campaign advances with no interlude, so the run into the final stage lands with no more weight than a rematch");

            Assert.IsFalse(StageInterlude.ShouldPlayOnEntry(StageId.Stage1, StageId.Stage1, advancedFromClear: true),
                "a rematch of the stage just cleared plays the interlude again, dropping a cutscene between the player and the retry they asked for");
            Assert.IsFalse(StageInterlude.ShouldPlayOnEntry(StageId.Stage2, StageId.Stage1, advancedFromClear: false),
                "picking a stage from the menu plays the between-stages interlude, so a player who chose a stage directly is told a story beat about arriving somewhere they did not travel from");
            Assert.IsFalse(StageInterlude.ShouldPlayOnEntry(StageId.Stage1, StageId.Stage1, advancedFromClear: false),
                "restarting the stage already loaded plays an interlude, putting a cutscene in front of a plain retry");
        }

        // 9 -------------------------------------------------------------------------------

        [Test]
        public void AScriptThatWasNeverBuilt_PlaysAsAnEmptyBeatInsteadOfThrowing()
        {
            // default(InterludeScript) skips the constructor entirely, so the beat array behind
            // it really is null. The controller calls HasContent on whatever it is handed, so
            // reading the raw field instead of the null-safe view is a NullReferenceException on
            // the exact frame a cutscene is supposed to begin.
            var script = default(InterludeScript);

            Assert.IsNotNull(script.Lines,
                "a script that was never built hands back a null beat list, so the first thing the cutscene controller touches on the frame it starts play is a null reference and the scene change dies there");
            Assert.AreEqual(0, script.Lines.Length,
                "a script that was never built claims to carry beats, so the surface tries to render lines that do not exist");

            bool hasContent = true;
            int indexAtZero = 0;
            int indexMidway = 0;
            int indexFarPast = 0;
            float timeInLine = -1f;
            float total = -1f;
            bool completeAtStart = true;
            bool completeAtRuntime = false;

            Assert.DoesNotThrow(() =>
            {
                hasContent = script.HasContent;
                indexAtZero = StageInterlude.LineIndexAt(script, 0f);
                indexMidway = StageInterlude.LineIndexAt(script, StageInterlude.PreRollSeconds + 5f);
                indexFarPast = StageInterlude.LineIndexAt(script, 1000000f);
                timeInLine = StageInterlude.TimeInLine(script, StageInterlude.PreRollSeconds + 5f);
                total = StageInterlude.TotalDurationSeconds(script);
                completeAtStart = StageInterlude.IsComplete(script, 0f);
                completeAtRuntime = StageInterlude.IsComplete(script, StageInterlude.PreRollSeconds + StageInterlude.TailSeconds);
            }, "every call the cutscene controller makes while a scene is on screen must survive a script that was never built; a throw here is the crash the player gets instead of the stage they asked for");

            Assert.IsFalse(hasContent,
                "an empty script claims to have something to show, so the controller opens a cutscene that will never display a line and never move on by itself");
            Assert.AreEqual(-1, indexAtZero,
                "an empty script names a beat to display on the opening frame, pointing the surface at a line that does not exist");
            Assert.AreEqual(-1, indexMidway,
                "an empty script names a beat to display mid-playback, pointing the surface at a line that does not exist");
            Assert.AreEqual(-1, indexFarPast,
                "an empty script still names a beat long after any cutscene would have ended, so a stale scene keeps something pinned on screen");
            Assert.AreEqual(0f, timeInLine, DurationTolerance,
                "an empty script reports time spent on a beat it does not have, which would drive a typewriter over text that is not there");
            Assert.AreEqual(StageInterlude.PreRollSeconds + StageInterlude.TailSeconds, total, DurationTolerance,
                "an empty script does not cost exactly the opening hold plus the tail, so the player is held on a blank screen for a stretch nobody wrote");
            Assert.IsFalse(completeAtStart,
                "an empty script is over before it starts, so its fade never gets to run and the transition reads as a hard cut");
            Assert.IsTrue(completeAtRuntime,
                "an empty script never reports itself finished, so the controller waits forever on a cutscene with nothing in it and the player never reaches the stage");
        }

        // 10 ------------------------------------------------------------------------------

        [Test]
        public void TheTypewriter_RevealsAShippedBeatAsAGrowingPrefixAndLandsExactlyOnTheFullLine()
        {
            string text = ShippedNarrationLine();
            var typewriter = new NarrativeTypewriter(text, StageInterlude.TypeCharactersPerSecond);

            Assert.AreEqual(string.Empty, typewriter.VisibleText,
                "the beat opens with text already on screen, so the reveal the scene is built around has nothing left to play");
            Assert.AreEqual(0, typewriter.VisibleCharacterCount,
                "the beat opens with characters already counted as shown, so the reveal starts part-way into the line");
            Assert.IsFalse(typewriter.IsComplete,
                "guard: a line that is finished before the first frame would make the rest of this case vacuous");

            const float frame = 1f / 60f;
            // Enough frames to outrun the line at the shipped speed, plus headroom, then a hard
            // stop so a reveal that stalls fails as a stalled reveal instead of hanging the run.
            int maxFrames = Mathf.CeilToInt(StageInterlude.LineDurationSeconds(text) / frame) + 120;
            int previousCount = 0;
            int frames = 0;

            while (!typewriter.IsComplete && frames < maxFrames)
            {
                typewriter.Advance(frame);
                frames++;

                Assert.IsTrue(text.StartsWith(typewriter.VisibleText, StringComparison.Ordinal),
                    $"after {frames} frames the beat is showing \"{typewriter.VisibleText}\", which is not the opening of the line it is revealing — the player reads scrambled or shifted prose part-way through the reveal");
                Assert.GreaterOrEqual(typewriter.VisibleCharacterCount, previousCount,
                    $"after {frames} frames the reveal has un-shown characters it had already typed, so the line visibly shrinks back mid-sentence");
                Assert.LessOrEqual(typewriter.VisibleCharacterCount, text.Length,
                    $"after {frames} frames the reveal claims more characters than the line contains, running the surface off the end of the text it is drawing");

                previousCount = typewriter.VisibleCharacterCount;
            }

            Assert.IsTrue(typewriter.IsComplete,
                "the reveal never finishes within the time the beat is allotted, so the line is still typing itself when the cutscene cuts away from it");
            Assert.AreEqual(text, typewriter.VisibleText,
                "the finished reveal is not the whole line, so the beat settles on truncated prose and the player never reads the end of the sentence");
        }

        [Test]
        public void TheTypewriter_SurvivesAFrameLongEnoughToOverrunTheWholeLine()
        {
            string text = ShippedNarrationLine();
            var typewriter = new NarrativeTypewriter(text, StageInterlude.TypeCharactersPerSecond);

            // One frame that swallows far more of the line than remains: a GC pause, a loading
            // hitch, or the app coming back from the background. A steady 60Hz walk at the
            // shipped speed happens to land exactly on the final element, so this is the only
            // shape that ever pushes the reveal past the end of its own text.
            float overshootFrame = StageInterlude.LineDurationSeconds(text) * 2f;
            typewriter.Advance(overshootFrame);

            Assert.LessOrEqual(typewriter.VisibleCharacterCount, text.Length,
                "a single long frame leaves the reveal counting more characters than the line holds, so the surface is handed a position past the end of the text it is drawing");
            Assert.IsTrue(typewriter.IsComplete,
                "a single long frame leaves the beat unfinished even though its whole runtime has gone by, so the cutscene waits on a line that has already been fully read");
            Assert.AreEqual(text, typewriter.VisibleText,
                "a single long frame settles the beat on something other than its own line, so a stutter changes what the player reads");
        }

        [Test]
        public void TheTypewriter_IgnoresAFrameThatDidNotAdvance()
        {
            string text = ShippedNarrationLine();
            var typewriter = new NarrativeTypewriter(text, StageInterlude.TypeCharactersPerSecond);

            typewriter.Advance(0f);
            typewriter.Advance(-1f);
            Assert.AreEqual(string.Empty, typewriter.VisibleText,
                "a paused frame types characters anyway, so a line starts revealing itself while the game is holding still");

            typewriter.Advance(0.2f);
            string partway = typewriter.VisibleText;

            Assert.IsFalse(typewriter.IsComplete,
                "guard: the reveal finished in a fifth of a second, so this case cannot observe what a stalled frame does to a reveal in progress");
            Assert.AreNotEqual(string.Empty, partway,
                "guard: the reveal showed nothing at all, so the check below would compare two empty strings and prove nothing");

            typewriter.Advance(0f);
            typewriter.Advance(-5f);

            Assert.AreEqual(partway, typewriter.VisibleText,
                "a paused frame, or a clock that jumped backwards, moves a reveal that is already part-way through a line, so the text lurches while the game is stopped");
        }

        [Test]
        public void RevealAll_SkipsStraightToTheWholeBeat()
        {
            string text = ShippedNarrationLine();
            var typewriter = new NarrativeTypewriter(text, StageInterlude.TypeCharactersPerSecond);
            typewriter.Advance(0.1f);

            typewriter.RevealAll();

            Assert.IsTrue(typewriter.IsComplete,
                "asking for the rest of the line leaves the reveal still running, so a player who wants to read ahead is held on a half-typed sentence anyway");
            Assert.AreEqual(text, typewriter.VisibleText,
                "the skip stops short of the whole line, so the player who asked to see it all reads a truncated beat");

            typewriter.Advance(1f);

            Assert.IsTrue(typewriter.IsComplete,
                "a frame after the skip reopens a line that was already finished, restarting a reveal the player has read");
            Assert.AreEqual(text, typewriter.VisibleText,
                "a frame after the skip disturbs the settled line, so the finished text changes under the player's eyes");
            Assert.LessOrEqual(typewriter.VisibleCharacterCount, text.Length,
                "a frame after the skip counts more characters than the line holds, running the surface off the end of the text it is drawing");
        }

        [TestCase(0f)]
        [TestCase(-30f)]
        public void ABeatWithNoRevealSpeed_ShowsItsLineOutrightInsteadOfStayingBlank(float charactersPerSecond)
        {
            string text = ShippedNarrationLine();
            var typewriter = new NarrativeTypewriter(text, charactersPerSecond);

            Assert.IsTrue(typewriter.IsComplete,
                "a reveal configured with no speed never reports itself done, so anything waiting on the line to finish waits forever");
            Assert.AreEqual(text, typewriter.VisibleText,
                "a reveal configured with no speed leaves the beat blank for its whole runtime, so the player watches an empty text box and the scene moves on having said nothing");

            typewriter.Advance(1f);

            Assert.AreEqual(text, typewriter.VisibleText,
                "a later frame takes the line back off a beat that had already shown it, blanking prose the player was reading");
        }
    }
}
