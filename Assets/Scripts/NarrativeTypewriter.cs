using System;
using System.Globalization;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Deterministic unscaled-time text reveal that never splits a Unicode text element.
    /// </summary>
    public sealed class NarrativeTypewriter
    {
        private readonly float charactersPerSecond;
        private int[] textElementStarts = Array.Empty<int>();
        private float revealedElementProgress;
        private int visibleCharacterCount;

        public NarrativeTypewriter(string fullText, float charactersPerSecond)
        {
            this.charactersPerSecond = charactersPerSecond;
            Reset(fullText);
        }

        public string FullText { get; private set; } = string.Empty;
        public int VisibleCharacterCount => visibleCharacterCount;
        public bool IsComplete => visibleCharacterCount >= textElementStarts.Length;

        public string VisibleText
        {
            get
            {
                if (visibleCharacterCount <= 0) return string.Empty;
                if (IsComplete) return FullText;
                return FullText.Substring(0, textElementStarts[visibleCharacterCount]);
            }
        }

        public void Reset(string fullText)
        {
            FullText = fullText ?? string.Empty;
            textElementStarts = StringInfo.ParseCombiningCharacters(FullText);
            revealedElementProgress = 0f;
            visibleCharacterCount = 0;
            if (charactersPerSecond <= 0f) RevealAll();
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (IsComplete || unscaledDeltaTime <= 0f) return;

            revealedElementProgress += unscaledDeltaTime * charactersPerSecond;
            visibleCharacterCount = Mathf.Min(textElementStarts.Length, Mathf.FloorToInt(revealedElementProgress));
        }

        public void RevealAll()
        {
            revealedElementProgress = textElementStarts.Length;
            visibleCharacterCount = textElementStarts.Length;
        }
    }
}
