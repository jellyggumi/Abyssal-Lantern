using System;
using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// What a session actually did, recorded so a gate can be measured instead of argued.
    ///
    /// The gates that decide whether this game is good — G2 (win-rate inside 45–55%) and G7
    /// (loop repeat-rate ≥70%) — were both unmeasurable because nothing ever recorded a match.
    /// Balance was therefore tuned the way match length used to be: by feel. This type is the
    /// counterpart to <see cref="MatchLengthModel"/>; that one states what SHOULD happen, this
    /// one records what DID.
    ///
    /// Deliberately a pure static type, not a MonoBehaviour: EditMode can drive it with no
    /// scene, and — more importantly — an observer that cannot touch the simulation cannot
    /// perturb the thing it is measuring. <c>CLAUDE.md</c> §2 forbids presentation writing back
    /// into simulation; telemetry is held to the same rule, and
    /// <c>TelemetryTests.Record_DoesNotMutateGameState</c> pins it.
    ///
    /// Storage is a ring buffer because the deployment target is a static web build with no
    /// server: events accumulate in memory and flush to PlayerPrefs (IndexedDB under WebGL).
    /// Unbounded growth would eventually cost a frame and a quota. Gate measurement reads
    /// aggregates, never the full history, so the oldest events are the cheapest thing to drop.
    /// </summary>
    public static class Telemetry
    {
        /// <summary>Schema of `ops/telemetry-contract.md`. Names are the wire format — renaming
        /// one silently breaks every previously captured dump, so treat them as a contract.</summary>
        public enum EventKind
        {
            MatchStart,
            Volley,
            Collapse,
            MatchEnd,
            Session
        }

        /// <summary>
        /// One record. Field meanings are per-kind (see <see cref="EventKind"/>); the struct is
        /// flat rather than polymorphic so it round-trips through JsonUtility, which cannot
        /// serialize interfaces or derived types in a List.
        /// </summary>
        [Serializable]
        public struct Event
        {
            public string kind;
            public string label;   // stage id / unit name / winner
            public float a;        // power        | blocks     | turns
            public float b;        // angle        | chainDepth | coreHpDelta
            public float c;        // wind         | —          | stagesCleared
            public float d;        // —            | —          | retryCount

            public EventKind Kind => (EventKind)Enum.Parse(typeof(EventKind), kind);
        }

        /// <summary>Ring capacity. 500 covers several full sessions of turn-boundary events;
        /// nothing here fires per frame, so this is generous rather than tight.</summary>
        public const int Capacity = 500;

        private const string PrefsKey = "CastleBusters.Telemetry.v1";

        private static readonly List<Event> buffer = new List<Event>(Capacity);
        private static int dropped;

        /// <summary>Events currently retained (≤ <see cref="Capacity"/>).</summary>
        public static int Count => buffer.Count;

        /// <summary>How many events the ring has discarded. Reported in the dump so a reader
        /// can tell a short session from a truncated one — a silent truncation would make an
        /// aggregate look complete when it is not.</summary>
        public static int Dropped => dropped;

        /// <summary>Chronological copy, oldest first. A copy so a consumer cannot mutate the ring.</summary>
        public static IReadOnlyList<Event> Snapshot() => buffer.ToArray();

        public static void Clear()
        {
            buffer.Clear();
            dropped = 0;
        }

        // ---- Recording -------------------------------------------------------------------
        // Every overload funnels into Push so the ring rule has exactly one implementation.

        private static void Push(Event e)
        {
            if (buffer.Count >= Capacity)
            {
                buffer.RemoveAt(0);
                dropped++;
            }
            buffer.Add(e);
        }

        public static void MatchStart(string stageId, string deck) =>
            Push(new Event { kind = nameof(EventKind.MatchStart), label = stageId ?? string.Empty, a = 0f, b = 0f, c = 0f, d = 0f });

        public static void Volley(string unit, float power, float angle, float wind) =>
            Push(new Event { kind = nameof(EventKind.Volley), label = unit ?? string.Empty, a = power, b = angle, c = wind, d = 0f });

        /// <summary>One resolved collapse chain, not one block — the reward event G4/G7 care
        /// about is the cascade, and a per-block record would flood the ring during a big one.</summary>
        public static void Collapse(int blocks, int chainDepth) =>
            Push(new Event { kind = nameof(EventKind.Collapse), label = string.Empty, a = blocks, b = chainDepth, c = 0f, d = 0f });

        public static void MatchEnd(string winner, int turns, float coreHpDelta) =>
            Push(new Event { kind = nameof(EventKind.MatchEnd), label = winner ?? string.Empty, a = turns, b = coreHpDelta, c = 0f, d = 0f });

        public static void Session(int stagesCleared, int retryCount) =>
            Push(new Event { kind = nameof(EventKind.Session), label = string.Empty, a = 0f, b = 0f, c = stagesCleared, d = retryCount });

        // ---- Aggregates ------------------------------------------------------------------
        // These are what a gate reads. Kept here rather than in the QA harness so the same
        // arithmetic serves the in-game dump and the automated measurement — two implementations
        // of "win rate" would eventually disagree, and the gate would inherit the disagreement.

        /// <summary>G2. Player win share over recorded matches, or -1 when nothing is recorded.
        /// Negative rather than 0 because "no data" and "lost every match" must not read alike —
        /// a gate that cannot tell them apart would pass a silent instrumentation failure.</summary>
        public static float PlayerWinRate()
        {
            int wins = 0, total = 0;
            foreach (var e in buffer)
            {
                if (e.kind != nameof(EventKind.MatchEnd)) continue;
                total++;
                if (e.label == WinnerPlayer) wins++;
            }
            return total == 0 ? -1f : (float)wins / total;
        }

        /// <summary>Mean turns to decide a match, or -1 when nothing is recorded. Compare against
        /// <see cref="MatchLengthModel"/>'s predicted N to see whether the model still describes play.</summary>
        public static float AverageTurns()
        {
            float sum = 0f;
            int total = 0;
            foreach (var e in buffer)
            {
                if (e.kind != nameof(EventKind.MatchEnd)) continue;
                sum += e.a;
                total++;
            }
            return total == 0 ? -1f : sum / total;
        }

        /// <summary>G7 proxy. Share of sessions in which the player voluntarily re-entered the
        /// loop at least once. This is a proxy, not the gate itself: it counts re-entry, not
        /// enjoyment, and a session cut short by a crash is indistinguishable from a bounce.</summary>
        public static float RepeatRate()
        {
            int repeated = 0, total = 0;
            foreach (var e in buffer)
            {
                if (e.kind != nameof(EventKind.Session)) continue;
                total++;
                if (e.d >= 1f) repeated++;
            }
            return total == 0 ? -1f : (float)repeated / total;
        }

        public const string WinnerPlayer = "player";
        public const string WinnerEnemy = "enemy";

        // ---- Persistence -----------------------------------------------------------------

        [Serializable]
        private struct Payload
        {
            public List<Event> events;
            public int dropped;
        }

        /// <summary>JSON of the current ring. Also the console-dump body.</summary>
        public static string ToJson() =>
            JsonUtility.ToJson(new Payload { events = new List<Event>(buffer), dropped = dropped });

        /// <summary>Replaces the ring from JSON. Malformed input clears rather than throws —
        /// a corrupt pref must not brick a player's boot for the sake of a measurement.</summary>
        public static void FromJson(string json)
        {
            Clear();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var payload = JsonUtility.FromJson<Payload>(json);
                if (payload.events == null) return;
                dropped = payload.dropped;
                int start = Mathf.Max(0, payload.events.Count - Capacity);
                for (int i = start; i < payload.events.Count; i++) buffer.Add(payload.events[i]);
            }
            catch (Exception)
            {
                Clear();
            }
        }

        /// <summary>Writes the ring to PlayerPrefs. Called at match end, never per event —
        /// a PlayerPrefs write is an IndexedDB round-trip under WebGL.</summary>
        public static void Flush()
        {
            PlayerPrefs.SetString(PrefsKey, ToJson());
            PlayerPrefs.Save();
        }

        public static void Load() => FromJson(PlayerPrefs.GetString(PrefsKey, string.Empty));

        public static void ClearPersisted()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Human-readable aggregate line for the console dump. -1 renders as "n/a"
        /// so a reader is never handed a number that means "no data".</summary>
        public static string Summary()
        {
            string Pct(float v) => v < 0f ? "n/a" : $"{v * 100f:F1}%";
            string Num(float v) => v < 0f ? "n/a" : $"{v:F1}";
            return $"[telemetry] events={Count} dropped={Dropped} " +
                   $"winRate={Pct(PlayerWinRate())} avgTurns={Num(AverageTurns())} repeatRate={Pct(RepeatRate())}";
        }
    }
}
