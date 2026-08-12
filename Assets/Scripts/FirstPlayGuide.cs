namespace CastleBusters
{
    /// <summary>
    /// Pure state machine for the first-play guided objectives (첫 출정 안내).
    ///
    /// Why this exists: first-contact playtest feedback (2026-08-12, professor review) was
    /// "튜토리얼이 없고 현재 UI가 뭐가 뭔지 모르겠다 / 처음 해보는 사람은 이게 무슨 게임인지
    /// 파악 자체가 안 된다 / 단계별로 한개씩 목적 주면서 진행하도록 해야". The title card's
    /// one-line how-to was not enough: a brand-new player needs ONE objective at a time,
    /// each advanced by the actual play action it teaches, not by reading.
    ///
    /// This type owns the step order, the advance rules, and the instruction text. It holds
    /// no engine state and reads only an <see cref="Observation"/> snapshot per frame, so
    /// EditMode pins the whole contract. The runtime surface (banner, arrow, skip button,
    /// persistence) lives in <see cref="FirstPlayCoachController"/>.
    ///
    /// Transition design rule: post-shot steps anchor on <see cref="Observation.TurnCount"/>,
    /// never on the transient <c>IsResolvingTurn</c>/<c>IsAiming</c> flags alone. Hit-stops
    /// freeze the clock mid-resolve and a fast volley can resolve inside one swallowed
    /// sampling window — live QA (2026-08-12) caught the flag-sampled version regressing to
    /// the draw step after a fully committed shot precisely because the resolve window was
    /// never sampled. The turn counter is durable: whatever frames were missed, "the player's
    /// turn ended" and "the enemy's turn happened" remain readable facts.
    /// </summary>
    public sealed class FirstPlayGuide
    {
        public enum Step
        {
            /// <summary>What game this is + win condition. Advances on acknowledge.</summary>
            Goal,
            /// <summary>Point at the slingshot: press inside the ring. Advances when a draw starts.</summary>
            Draw,
            /// <summary>Pull back and release. Advances when the player's turn actually ends
            /// (shot committed and resolved, however briefly the flags flickered).</summary>
            Release,
            /// <summary>The enemy answers. Advances when control returns to the player.</summary>
            EnemyReply,
            /// <summary>Hand the match over. Advances on acknowledge, then the coach leaves.</summary>
            FreePlay,
            Done
        }

        /// <summary>Everything the guide is allowed to know about a frame.</summary>
        public readonly struct Observation
        {
            /// <summary>Player pressed/clicked/spaced this frame, or the step's dwell elapsed.</summary>
            public readonly bool Acknowledged;
            public readonly bool IsPlayerTurn;
            /// <summary>True while the player is actively drawing the sling.</summary>
            public readonly bool IsAiming;
            /// <summary>True while a committed volley resolves (GameManager.IsResolvingTurn).</summary>
            public readonly bool IsResolvingTurn;
            public readonly bool IsGameOver;
            /// <summary>GameManager.TurnCount — increments at every turn boundary.</summary>
            public readonly int TurnCount;

            public Observation(
                bool acknowledged,
                bool isPlayerTurn,
                bool isAiming,
                bool isResolvingTurn,
                bool isGameOver,
                int turnCount)
            {
                Acknowledged = acknowledged;
                IsPlayerTurn = isPlayerTurn;
                IsAiming = isAiming;
                IsResolvingTurn = isResolvingTurn;
                IsGameOver = isGameOver;
                TurnCount = turnCount;
            }
        }

        /// <summary>How long the Goal card stays before it advances on its own.</summary>
        public const float GoalAutoAdvanceSeconds = 6f;
        /// <summary>How long the FreePlay card stays before the coach dismisses itself.</summary>
        public const float FreePlayAutoAdvanceSeconds = 4.5f;
        /// <summary>
        /// Upper bound on how long the coach may keep topping the turn clock up. The hold
        /// exists so the first turn is never forfeited mid-instruction; the cap exists so a
        /// walked-away session still ends through the normal forfeit path.
        /// </summary>
        public const float MaxTimerHoldSeconds = 45f;

        public Step Current { get; private set; } = Step.Goal;
        public bool IsFinished => Current == Step.Done;

        // The player turn the Release step was entered on. A weak pull leaves this turn
        // running (back to Draw); any turn advance past it proves the shot was spent.
        private int releaseTurn;
        // The turn the shot was committed on. EnemyReply completes when the count reaches
        // shotTurn + 2: +1 is the enemy's reply, +2 is control back with the player — true
        // even when every intermediate frame was swallowed by a hit-stop.
        private int shotTurn;

        /// <summary>
        /// While true the runtime may keep the player's turn clock from expiring: the player
        /// is being told what to do and must not be punished for reading it.
        /// Enemy turns and free play run on the real clock.
        /// </summary>
        public bool HoldsTurnClock =>
            Current == Step.Goal || Current == Step.Draw || Current == Step.Release;

        /// <summary>Feed one frame of game state. Returns true when the step changed.</summary>
        public bool Advance(in Observation obs)
        {
            if (Current == Step.Done) return false;

            // A finished match ends coaching from any step: the results card owns the screen
            // and the next match starts with the guide already marked seen by the runtime.
            if (obs.IsGameOver)
            {
                Current = Step.Done;
                return true;
            }

            switch (Current)
            {
                case Step.Goal:
                    if (obs.Acknowledged)
                    {
                        Current = Step.Draw;
                        return true;
                    }
                    return false;

                case Step.Draw:
                    if (obs.IsAiming && obs.IsPlayerTurn)
                    {
                        Current = Step.Release;
                        releaseTurn = obs.TurnCount;
                        return true;
                    }
                    // Keyboard path: Space commits a shot without ever entering the drag
                    // state. A resolving player volley is proof a shot happened.
                    if (obs.IsResolvingTurn && obs.IsPlayerTurn)
                    {
                        Current = Step.EnemyReply;
                        shotTurn = obs.TurnCount;
                        return true;
                    }
                    return false;

                case Step.Release:
                    // The committed shot observed live (flags caught mid-resolve).
                    if (obs.IsResolvingTurn && obs.IsPlayerTurn)
                    {
                        Current = Step.EnemyReply;
                        shotTurn = releaseTurn;
                        return true;
                    }
                    // The committed shot proven after the fact: the turn advanced past the
                    // one the pull started on. Covers resolve windows swallowed whole by
                    // hit-stop sampling gaps AND a forfeit mid-pull — either way the enemy
                    // reply is now the honest next beat.
                    if (obs.TurnCount > releaseTurn)
                    {
                        Current = Step.EnemyReply;
                        shotTurn = releaseTurn;
                        return true;
                    }
                    // Released too weak (the launcher refused the shot): same turn, no
                    // resolve, no pull. Back to the draw instruction.
                    if (!obs.IsAiming)
                    {
                        Current = Step.Draw;
                        return true;
                    }
                    return false;

                case Step.EnemyReply:
                    // shotTurn+1 was the enemy's turn; at shotTurn+2 control is back with
                    // the player. The count carries this fact even across missed frames.
                    if (obs.IsPlayerTurn && !obs.IsResolvingTurn && obs.TurnCount >= shotTurn + 2)
                    {
                        Current = Step.FreePlay;
                        return true;
                    }
                    return false;

                case Step.FreePlay:
                    if (obs.Acknowledged)
                    {
                        Current = Step.Done;
                        return true;
                    }
                    return false;
            }

            return false;
        }

        /// <summary>Progress label ("1/5"). Empty once done — the banner is gone by then.</summary>
        public static string StepLabel(Step step)
        {
            switch (step)
            {
                case Step.Goal: return "1/5";
                case Step.Draw: return "2/5";
                case Step.Release: return "3/5";
                case Step.EnemyReply: return "4/5";
                case Step.FreePlay: return "5/5";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// One objective per step, Korean, single line. Each names the concrete thing to do
        /// or watch right now — never two instructions at once.
        /// </summary>
        public static string Instruction(Step step)
        {
            switch (step)
            {
                case Step.Goal:
                    return "턴제 공성전 — 적 성의 심장(황금 코어)을 먼저 부수면 승리";
                case Step.Draw:
                    return "새총의 푸른 링 안을 누르세요";
                case Step.Release:
                    return "뒤로 당겼다 놓으면 당긴 반대쪽으로 발사됩니다";
                case Step.EnemyReply:
                    return "적의 반격 — 내 성의 심장은 왼쪽입니다";
                case Step.FreePlay:
                    return "이제 자유롭게 — 한 턴에 한 발씩, 적 심장을 부수세요";
                default:
                    return string.Empty;
            }
        }
    }
}
