using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// What this turn can spend itself on, written as text the player can act on.
    ///
    /// Why this exists (usability request, 2026-08-13 — 대포 설치 / 턴에 행동해야 할 방법):
    /// the one-shot turn offers exactly two actions — fire the volley, or emplace the
    /// battery instead — but only the first was discoverable. The battery lived behind a
    /// button labelled `배치 모드 OFF (D)`, which names an internal mode and says nothing
    /// about what gets placed, what it costs, what unlocks it, or that placing it spends
    /// the turn. Every gate it sits behind (unlock turn, wall breaches, supply, cooldown)
    /// was invisible until the player happened to press D and got refused.
    ///
    /// The rule here: the label always names either the action available now, or the ONE
    /// condition standing in front of it. Gate order matches
    /// <see cref="DeploymentRules.Evaluate"/> — most-permanent first — so the player is
    /// told the thing they must actually solve rather than the first thing that failed.
    ///
    /// Pure: no engine state, no scene lookups. The runtime passes a snapshot in, so
    /// EditMode can pin every state a player can reach.
    /// </summary>
    public static class TurnActionPrompt
    {
        public enum Tone
        {
            /// <summary>Action is available right now.</summary>
            Ready,
            /// <summary>Placement is armed; the next board click spends the turn.</summary>
            Armed,
            /// <summary>A gate is unmet — the label names it.</summary>
            Blocked,
            /// <summary>Not the player's turn to act.</summary>
            Idle
        }

        public readonly struct Prompt
        {
            public readonly string label;
            public readonly Tone tone;
            /// <summary>False when pressing the button cannot do anything useful.</summary>
            public readonly bool interactable;

            public Prompt(string label, Tone tone, bool interactable)
            {
                this.label = label;
                this.tone = tone;
                this.interactable = interactable;
            }
        }

        /// <summary>
        /// Cannon prompt for the one-shot loop.
        /// </summary>
        /// <param name="playerCanAct">Player's turn, not resolving.</param>
        /// <param name="deployArmed">Placement mode is already armed.</param>
        /// <param name="turnCount">Current turn, for the unlock gate.</param>
        /// <param name="breaches">Enemy wall blocks the player has broken.</param>
        /// <param name="supply">Player supply.</param>
        /// <param name="cooldownRemaining">Seconds left on the battery cooldown.</param>
        public static Prompt ForCannon(
            bool playerCanAct,
            bool deployArmed,
            int turnCount,
            int breaches,
            float supply,
            float cooldownRemaining)
        {
            const DeployCard card = DeployCard.Cannon;
            string name = DeploymentRules.DisplayName(card);

            if (!playerCanAct)
            {
                return new Prompt($"{name} — 내 턴에 설치 가능", Tone.Idle, false);
            }

            if (deployArmed)
            {
                // The armed state changes what a board click MEANS, so it has to be
                // unmistakable and it has to name the way out.
                return new Prompt($"{name} 설치 위치 선택 · Esc 취소", Tone.Armed, true);
            }

            if (!DeploymentRules.IsUnlocked(card, turnCount))
            {
                int remaining = Mathf.Max(1, DeploymentRules.UnlockTurn(card) - turnCount);
                return new Prompt($"{name} — {remaining}턴 후 해금", Tone.Blocked, false);
            }

            if (!DeploymentRules.BreachSatisfied(card, breaches))
            {
                int need = Mathf.Max(1, DeploymentRules.CannonBreachRequirement - breaches);
                return new Prompt($"{name} — 적 성벽 {need}블록 더 파괴", Tone.Blocked, false);
            }

            if (cooldownRemaining > 0f)
            {
                return new Prompt($"{name} — 재사용 {cooldownRemaining:0.0}초", Tone.Blocked, false);
            }

            float cost = DeploymentRules.CostOf(card);
            if (supply + 1e-4f < cost)
            {
                return new Prompt($"{name} — 보급 {cost:0} 필요 (현재 {supply:0})", Tone.Blocked, false);
            }

            // Available. The label states the cost AND that it spends the turn, because
            // "one action per turn" is the rule players lose most often.
            return new Prompt($"{name} 설치 (D) · 보급 {cost:0} · 턴 소모", Tone.Ready, true);
        }

        /// <summary>Colour per tone, so the state is readable before the text is.</summary>
        public static Color ColorFor(Tone tone)
        {
            switch (tone)
            {
                case Tone.Ready: return new Color(0.62f, 1f, 0.76f, 1f);
                case Tone.Armed: return new Color(1f, 0.84f, 0.3f, 1f);
                case Tone.Blocked: return new Color(0.72f, 0.76f, 0.82f, 1f);
                default: return new Color(0.6f, 0.64f, 0.7f, 1f);
            }
        }
    }
}
