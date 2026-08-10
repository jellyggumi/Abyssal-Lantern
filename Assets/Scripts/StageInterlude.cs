using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Which narrative beat a cutscene is playing. The kind decides the backdrop tone and
    /// what the surface hands control back to, never the text itself — script content lives
    /// in <see cref="StageInterlude"/> so the two can be tested apart.
    /// </summary>
    public enum InterludeKind
    {
        /// <summary>Cold open, once per app session, before the title card.</summary>
        Opening,
        /// <summary>Between stages: the campaign's connective tissue after a stage is cleared.</summary>
        StageEntry,
        /// <summary>Campaign closer after the final stage falls.</summary>
        Epilogue
    }

    /// <summary>
    /// A single narration beat: one line of prose, optionally attributed to a speaker.
    /// </summary>
    public readonly struct InterludeLine
    {
        /// <summary>Narration prose. Always present.</summary>
        public readonly string text;
        /// <summary>Speaker name, or null for impersonal narration.</summary>
        public readonly string speaker;

        public InterludeLine(string text, string speaker = null)
        {
            this.text = text ?? string.Empty;
            this.speaker = string.IsNullOrWhiteSpace(speaker) ? null : speaker;
        }

        public bool IsNarration => speaker == null;
    }

    /// <summary>
    /// A complete cutscene script: heading, backdrop key, and the ordered narration beats.
    /// </summary>
    public readonly struct InterludeScript
    {
        public readonly InterludeKind kind;
        public readonly string heading;
        /// <summary>Resources key under `Webtoon/` for the backdrop panel, or null for a plain tone wash.</summary>
        public readonly string backdropKey;
        public readonly Color tone;
        public readonly Color accent;
        /// <summary>
        /// Ordered narration beats. May be null on a `default(InterludeScript)` — a struct's
        /// default skips the constructor entirely, so the constructor's null-coalescing does
        /// NOT protect that path. Read it through <see cref="Lines"/>, never directly.
        /// </summary>
        private readonly InterludeLine[] lines;

        /// <summary>Null-safe view of the beats; empty for a default-constructed script.</summary>
        public InterludeLine[] Lines => lines ?? Empty;

        private static readonly InterludeLine[] Empty = new InterludeLine[0];

        public InterludeScript(InterludeKind kind, string heading, string backdropKey,
            Color tone, Color accent, InterludeLine[] lines)
        {
            this.kind = kind;
            this.heading = heading ?? string.Empty;
            this.backdropKey = string.IsNullOrWhiteSpace(backdropKey) ? null : backdropKey;
            this.tone = tone;
            this.accent = accent;
            this.lines = lines ?? Empty;
        }

        public bool HasContent => Lines.Length > 0;
    }

    /// <summary>
    /// Cutscene scripts and their playback timing — pure data + arithmetic, no engine state,
    /// so EditMode pins both the campaign's narrative coverage and the pacing contract.
    ///
    /// Why per-stage interludes exist: the campaign previously cut from a results screen
    /// straight into the next battlefield, so three visually distinct stages read as "the
    /// same fight again, different rocks". A short narration beat on entry is what turns a
    /// stage list into a campaign — it states where you now are and what changed.
    /// </summary>
    public static class StageInterlude
    {
        /// <summary>Seconds a fully-typed line holds before the next one starts.</summary>
        public const float LineHoldSeconds = 1.9f;
        /// <summary>Cross-fade between lines.</summary>
        public const float LineFadeSeconds = 0.35f;
        /// <summary>Characters revealed per second by the typewriter.</summary>
        public const float TypeCharactersPerSecond = 30f;
        /// <summary>Black hold before the first line, so the cut never lands as a jump.</summary>
        public const float PreRollSeconds = 0.7f;
        /// <summary>Fade to black after the last line, before handing control back.</summary>
        public const float TailSeconds = 0.6f;

        /// <summary>Seconds one line occupies, typing included.</summary>
        public static float LineDurationSeconds(string text)
        {
            int chars = string.IsNullOrEmpty(text) ? 0 : text.Length;
            float typing = TypeCharactersPerSecond > 0f ? chars / TypeCharactersPerSecond : 0f;
            return typing + LineHoldSeconds + LineFadeSeconds;
        }

        /// <summary>Total runtime of a script, pre-roll and tail included.</summary>
        public static float TotalDurationSeconds(InterludeScript script)
        {
            float total = PreRollSeconds + TailSeconds;
            for (int i = 0; i < script.Lines.Length; i++)
            {
                total += LineDurationSeconds(script.Lines[i].text);
            }
            return total;
        }

        /// <summary>
        /// Index of the line showing at <paramref name="elapsed"/>, clamped into range.
        /// Returns -1 during pre-roll (nothing is showing yet) and the last index once the
        /// script has run out, so a caller can hold the final frame during the tail fade.
        /// </summary>
        public static int LineIndexAt(InterludeScript script, float elapsed)
        {
            if (script.Lines.Length == 0) return -1;
            if (elapsed < PreRollSeconds) return -1;

            float cursor = PreRollSeconds;
            for (int i = 0; i < script.Lines.Length; i++)
            {
                cursor += LineDurationSeconds(script.Lines[i].text);
                if (elapsed < cursor) return i;
            }
            return script.Lines.Length - 1;
        }

        /// <summary>Seconds spent inside the line currently showing (0 during pre-roll).</summary>
        public static float TimeInLine(InterludeScript script, float elapsed)
        {
            int index = LineIndexAt(script, elapsed);
            if (index < 0) return 0f;

            float cursor = PreRollSeconds;
            for (int i = 0; i < index; i++)
            {
                cursor += LineDurationSeconds(script.Lines[i].text);
            }
            return Mathf.Max(0f, elapsed - cursor);
        }

        /// <summary>True once the whole script (including the tail fade) has played out.</summary>
        public static bool IsComplete(InterludeScript script, float elapsed)
        {
            return elapsed >= TotalDurationSeconds(script);
        }

        // ---- Scripts ----

        private static readonly Color PlainsTone = new Color(0.15f, 0.17f, 0.22f, 1f);
        private static readonly Color DuneTone = new Color(0.24f, 0.19f, 0.13f, 1f);
        private static readonly Color AbyssTone = new Color(0.20f, 0.11f, 0.12f, 1f);

        /// <summary>
        /// The beat played when a stage begins. Each one names the place, states what makes
        /// this battlefield different in play terms, and ends on a line of character voice —
        /// so the cutscene teaches the stage as well as framing it.
        /// </summary>
        public static InterludeScript ForStageEntry(StageId stage)
        {
            switch (stage)
            {
                case StageId.Stage2:
                    return new InterludeScript(InterludeKind.StageEntry,
                        "제2막 · 황량한 모래언덕",
                        "Webtoon/panel-05",
                        DuneTone, new Color(1f, 0.78f, 0.42f, 1f),
                        new[]
                        {
                            new InterludeLine("평원의 성이 무너지자, 적은 모래 너머로 물러났다."),
                            new InterludeLine("여기서는 사거리가 짧다. 성벽은 더 높고, 전장은 더 빨리 변한다."),
                            new InterludeLine("가까워졌다는 건, 저쪽도 우리를 볼 수 있다는 뜻이야.", "돌격병"),
                        });

                case StageId.Stage3:
                    return new InterludeScript(InterludeKind.StageEntry,
                        "제3막 · 화산 심연",
                        "Webtoon/panel-08",
                        AbyssTone, new Color(1f, 0.55f, 0.32f, 1f),
                        new[]
                        {
                            new InterludeLine("모래언덕을 넘자 땅이 갈라지고, 뜨거운 바람이 화살을 집어삼켰다."),
                            new InterludeLine("협곡은 넓다. 한 발 한 발이 멀어지고, 바람은 더 사납다."),
                            new InterludeLine("여기서는 바람을 읽지 못하면 아무것도 맞출 수 없어.", "궁수"),
                            new InterludeLine("적의 마지막 성이 저 아래에 있다. 이번이 끝이다."),
                        });

                default:
                    return new InterludeScript(InterludeKind.StageEntry,
                        "제1막 · 공성 평원",
                        "Webtoon/panel-02",
                        PlainsTone, new Color(0.72f, 0.9f, 1f, 1f),
                        new[]
                        {
                            new InterludeLine("첫 성이 지평선 위에 서 있다. 넓은 평원, 숨을 곳은 없다."),
                            new InterludeLine("새총에 병사를 걸고, 성심부를 노려라."),
                            new InterludeLine("정면은 내가 연다.", "돌격병"),
                        });
            }
        }

        /// <summary>Campaign closer, played once the final stage's keep falls.</summary>
        public static InterludeScript Epilogue()
        {
            return new InterludeScript(InterludeKind.Epilogue,
                "종막 · 무너진 심연",
                "Webtoon/panel-11",
                AbyssTone, new Color(1f, 0.88f, 0.56f, 1f),
                new[]
                {
                    new InterludeLine("마지막 성이 무너졌다. 화산 위로 먼지가 천천히 가라앉는다."),
                    new InterludeLine("남은 건 부서진 벽과, 끝까지 서 있던 병사들뿐이다."),
                    new InterludeLine("끝났다. 이제 집으로 돌아가자.", "돌격병"),
                });
        }

        /// <summary>
        /// True when entering <paramref name="stage"/> should play its entry cutscene.
        /// Stage 1's beat belongs to the opening (the prologue already frames it), so the
        /// interlude is what plays when the campaign MOVES — never on a rematch of the same
        /// stage, which would put a cutscene between a player and their retry.
        /// </summary>
        public static bool ShouldPlayOnEntry(StageId entering, StageId previous, bool advancedFromClear)
        {
            if (!advancedFromClear) return false;
            return entering != previous;
        }
    }
}
