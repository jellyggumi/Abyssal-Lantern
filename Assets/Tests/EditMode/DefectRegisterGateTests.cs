using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Makes the harness's blocking predicate executable.
    ///
    /// The contract says "Any open S1 defect blocks every gate". Applying it needs to know which
    /// defects are open, and on 2026-08-18 that was unknowable: `qa/ux-defect-list.md` listed four
    /// S1 defects and had no status column at all. Four registers' worth of severity with no way to
    /// resolve any of it, and the gate reviews had been proceeding anyway.
    ///
    /// The contract already chose a default for the neighbouring case — "Missing evidence path =
    /// FAIL regardless of claimed value" — so absence is not read favourably there. These tests
    /// extend the same direction to status: a row that carries a severity and no status reads as
    /// OPEN, and a register that cannot express status is a defect in the register.
    ///
    /// Everything here walks the DISK. A declared list of register files would reproduce, for the
    /// fifth time in this repository, the failure that the same list-walking shape has already
    /// caused four times (see CLAUDE.md §5): a register added later would be outside every check
    /// permanently, which is exactly how `ux-defect-list.md` went sixteen entries deep without a
    /// status column while a suite ran green beside it.
    /// </summary>
    public sealed class DefectRegisterGateTests
    {
        private const string WorkspaceRelative = "../_workspace/current";

        /// <summary>
        /// A severity cell: S1/S2/S3, tolerating bold and a trailing gloss.
        ///
        /// The gloss tolerance is not cosmetic. `ux-defect-list.md`'s rollup writes `S1 (치명)`, and
        /// an anchored `^\**\s*(S[123])\s*\**$` did not match it — so that table escaped this gate by
        /// ACCIDENT of its cell decoration, and tidying the cell to `| S1 |` would have turned the
        /// gate red on a rollup that was always shaped that way. Detection that depends on
        /// formatting is detection that moves when someone reformats. A peer lane found this.
        ///
        /// Rollups legitimately have no per-row status (they count rows that have one), so they are
        /// excluded by row shape below rather than by failing to be recognised.
        /// </summary>
        private static readonly Regex SeverityCell = new Regex(
            @"^\**\s*(S[123])\b", RegexOptions.Compiled);

        /// <summary>
        /// A rollup row: a severity, then a bare count. It aggregates rows that carry status rather
        /// than tracking a defect itself, so requiring a status column of it is a category error.
        /// Recognised by shape, not by which table it sits in.
        /// </summary>
        private static readonly Regex RollupCount = new Regex(@"^\**\s*\d+\s*\**$", RegexOptions.Compiled);

        /// <summary>Header cells that mean "this row's lifecycle state".</summary>
        private static readonly string[] StatusHeaders = { "status", "상태", "처리", "state" };

        /// <summary>
        /// The role line a register must carry. Without it the machine cannot tell which file is
        /// authoritative when two registers disagree, and on 2026-08-18 two of them were disjoint —
        /// `defect-register.md` held D-001..D-017 and no UX entry, while `ux-defect-list.md` held
        /// sixteen UX entries and no D entry.
        /// </summary>
        private static readonly Regex RoleLine = new Regex(
            @"^\s*[-*]?\s*register-role:\s*(canonical|derived|rollup)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static string WorkspaceRoot => Path.GetFullPath(Path.Combine(Application.dataPath, WorkspaceRelative));

        private sealed class Row
        {
            public string File;
            public int Line;
            public string Severity;
            public string Id;
            public string Status;
            public bool HasStatusColumn;
        }

        private sealed class Table
        {
            public string File;
            public int HeaderLine;
            public List<string> Headers = new List<string>();
            public List<Row> Rows = new List<Row>();
            public int StatusIndex = -1;
            public int IdIndex = -1;
        }

        private static List<string> Cells(string line)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("|")) return null;
            var parts = trimmed.Split('|').ToList();
            // A markdown row starts and ends with the delimiter, producing empty outer entries.
            if (parts.Count > 0) parts.RemoveAt(0);
            if (parts.Count > 0 && string.IsNullOrWhiteSpace(parts[parts.Count - 1])) parts.RemoveAt(parts.Count - 1);
            return parts.Select(p => p.Trim()).ToList();
        }

        private static bool IsSeparator(List<string> cells)
            => cells != null && cells.Count > 0 && cells.All(c => c.Length > 0 && c.All(ch => ch == '-' || ch == ':' || ch == ' '));

        /// <summary>
        /// Whether this file is a register — a document that OWNS defect lifecycle — as opposed to
        /// one that merely quotes a severity while judging something.
        ///
        /// The distinction is load-bearing and the first version of this gate did not have it. An
        /// audit's one-line verdict table (`결함 | 등급 | 한 줄 판정`) legitimately has no status
        /// column: it is not tracking the defect, it is stating a finding about it. Demanding one
        /// there failed this gate on five documents that were correct — the same false-positive
        /// shape a peer lane had just caught in the HUD pin, and a test that fails on a clean
        /// repository cannot prove anything when it fails on a dirty one.
        /// </summary>
        private static bool IsRegisterFile(string path)
        {
            var n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return n.Contains("register") || n.Contains("defect-list");
        }

        /// <summary>
        /// Whether this register declares itself CANONICAL — the file to believe for its ID range.
        ///
        /// Only a canonical register owns status. A `derived` audit view cites defects and reasons
        /// about them, and its prose tables carry severities inside sentences ("this row is S2
        /// because…"); demanding a status column of those is the same category error as demanding
        /// one of an audit verdict table, one level in. The role line the sibling test requires is
        /// what makes this distinction machine-readable instead of a filename guess.
        /// </summary>
        private static bool IsCanonicalRegister(string path)
        {
            if (!IsRegisterFile(path)) return false;
            var m = RoleLine.Match(File.ReadAllText(path));
            return m.Success && m.Groups[1].Value.Equals("canonical", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Every markdown table in a register file that carries at least one severity cell.
        /// Structural within that scope: a table qualifies by containing S1/S2/S3 in a cell, not by
        /// being on a list somebody remembered to update. A register added tomorrow is covered the
        /// moment its name says register.
        /// </summary>
        private static List<Table> DiscoverSeverityTables()
        {
            var found = new List<Table>();
            if (!Directory.Exists(WorkspaceRoot)) return found;

            foreach (var path in Directory.GetFiles(WorkspaceRoot, "*.md", SearchOption.AllDirectories))
            {
                if (!IsCanonicalRegister(path)) continue;

                var lines = File.ReadAllLines(path);
                Table open = null;
                bool sawSeparator = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var cells = Cells(lines[i]);
                    if (cells == null || cells.Count < 2)
                    {
                        if (open != null && open.Rows.Any(r => r.Severity != null)) found.Add(open);
                        open = null;
                        sawSeparator = false;
                        continue;
                    }

                    if (open == null)
                    {
                        open = new Table { File = path, HeaderLine = i + 1, Headers = cells };
                        sawSeparator = false;
                        continue;
                    }

                    if (!sawSeparator)
                    {
                        if (IsSeparator(cells)) { sawSeparator = true; continue; }
                        // Two consecutive non-separator rows: the first was not a header.
                        open = new Table { File = path, HeaderLine = i + 1, Headers = cells };
                        continue;
                    }

                    var sevIndex = cells.FindIndex(c => SeverityCell.IsMatch(c));
                    if (sevIndex < 0) continue;

                    // A rollup row (`| S1 (치명) | 4 | UX-001, ... |`) counts defects that carry
                    // status; it does not track one. Excluded by shape so the exclusion survives
                    // reformatting, unlike the accident that used to hide it from detection.
                    if (cells.Skip(sevIndex + 1).Any(c => RollupCount.IsMatch(c))) continue;

                    if (open.StatusIndex < 0)
                    {
                        open.StatusIndex = open.Headers.FindIndex(
                            h => StatusHeaders.Any(s => h.ToLowerInvariant().Contains(s)));
                        open.IdIndex = open.Headers.FindIndex(
                            h => h.ToLowerInvariant().Contains("id") || h.Trim() == "#");
                    }

                    open.Rows.Add(new Row
                    {
                        File = path,
                        Line = i + 1,
                        Severity = SeverityCell.Match(cells[sevIndex]).Groups[1].Value,
                        Id = open.IdIndex >= 0 && open.IdIndex < cells.Count ? cells[open.IdIndex] : $"(row {i + 1})",
                        Status = open.StatusIndex >= 0 && open.StatusIndex < cells.Count ? cells[open.StatusIndex] : null,
                        HasStatusColumn = open.StatusIndex >= 0,
                    });
                }

                if (open != null && open.Rows.Any(r => r.Severity != null)) found.Add(open);
            }
            return found;
        }

        private static string Rel(string path)
        {
            var root = WorkspaceRoot;
            return path.StartsWith(root) ? path.Substring(root.Length).TrimStart('/', '\\') : path;
        }

        /// <summary>
        /// A register that records severity must be able to record status.
        ///
        /// Without this, "any open S1 blocks every gate" is unapplicable rather than satisfied —
        /// and unapplicable reads as satisfied to anyone in a hurry, which is how sixteen UX entries
        /// accumulated behind a predicate nobody could evaluate.
        /// </summary>
        [Test]
        public void EverySeverityTable_CanExpressStatus()
        {
            var tables = DiscoverSeverityTables();
            Assert.That(tables, Is.Not.Empty,
                $"No severity-bearing table found anywhere under '{WorkspaceRoot}'. Either the "
                + "registers moved or this walk is broken; an empty walk asserts nothing.");

            var mute = tables
                .Where(t => t.Rows.Any() && t.Rows.All(r => !r.HasStatusColumn))
                .Select(t => $"{Rel(t.File)}:{t.HeaderLine} — columns [{string.Join(" | ", t.Headers)}], "
                           + $"{t.Rows.Count} severity row(s) including {string.Join(", ", t.Rows.Take(4).Select(r => r.Id))}")
                .ToList();

            Assert.That(mute, Is.Empty,
                "These tables assign severities they cannot resolve. The harness contract blocks "
                + "every gate on an open S1, so a severity with no status column means the predicate "
                + "cannot be evaluated at all - and an unevaluable blocker reads as no blocker. Add a "
                + "status column (or a header matching one of: "
                + string.Join(", ", StatusHeaders) + "). Offenders: " + string.Join("; ", mute));
        }

        /// <summary>
        /// A severity row with an empty status is OPEN, and this names them.
        ///
        /// Same direction the contract already chose one line earlier for evidence paths: absence is
        /// not interpreted in the project's favour. A blank status is not a quiet pass.
        /// </summary>
        [Test]
        public void NoSeverityRow_LeavesItsStatusBlank()
        {
            var blank = DiscoverSeverityTables()
                .SelectMany(t => t.Rows)
                .Where(r => r.HasStatusColumn && string.IsNullOrWhiteSpace(r.Status))
                .Select(r => $"{Rel(r.File)}:{r.Line} [{r.Id}] {r.Severity}")
                .ToList();

            Assert.That(blank, Is.Empty,
                "These rows carry a severity and a blank status, which this gate reads as OPEN "
                + "because the contract reads a missing evidence path as FAIL and absence is treated "
                + "the same way here. Write the status, even if the status is 'open'. Rows: "
                + string.Join("; ", blank));
        }

        /// <summary>
        /// While any S1 is open, no gate review may claim PASS.
        ///
        /// This is the contract sentence itself, executable. It was violated for the life of
        /// `ux-defect-list.md`'s missing status column - not by anyone deciding to override it, but
        /// because nothing could check it.
        /// </summary>
        [Test]
        public void WhileAnyS1IsOpen_NoGateReviewClaimsPass()
        {
            var openS1 = DiscoverSeverityTables()
                .SelectMany(t => t.Rows)
                .Where(r => r.Severity == "S1")
                .Where(r => !r.HasStatusColumn
                         || string.IsNullOrWhiteSpace(r.Status)
                         || Regex.IsMatch(r.Status, @"open|미해결|미실시|blocked", RegexOptions.IgnoreCase))
                .Select(r => $"{Rel(r.File)}:{r.Line} [{r.Id}]")
                .ToList();

            // Absence of the review directory is a failure, not a quiet pass. The first version of
            // this test wrote `Assert.That(openS1, Is.Not.Null)` here — and `openS1` is a
            // `.ToList()` result, so that can never be false. The contract's central sentence would
            // have gone unasserted the moment the directory was renamed, which is the same vacuous
            // -pass hole `Assert.Greater(checkedCount, 0)` exists to close in the HUD pin. Written
            // in the same cycle that fixed it there, and caught by a peer lane reading this file.
            var reviewRoot = Path.Combine(WorkspaceRoot, "production", "gate-reviews");
            Assert.That(Directory.Exists(reviewRoot), Is.True,
                $"'{Rel(reviewRoot)}' does not exist, so this test cannot see any verdict and would "
                + "pass while asserting nothing. The contract requires the director to record gate "
                + "verdicts there; if that moved, this walk has to move with it.");

            // Verdicts are read as STRUCTURE, not as prose. The first version searched for the word
            // PASS and carried a blacklist of phrases to excuse (`PASS / FIX`, `가능`, `후보`, …) —
            // a declared list, which is the failure shape this cycle recorded four layers of. It
            // would have mis-read `PASS 조건`, `PASS 기준`, and the heading `## G4 PASS` the moment
            // anyone wrote a real review. A key line has no such ambiguity.
            var verdictLine = new Regex(@"^\s*[-*]?\s*verdict:\s*\**\s*([A-Za-z]+)", RegexOptions.IgnoreCase);
            var passing = new List<string>();
            foreach (var path in Directory.GetFiles(reviewRoot, "*.md", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    var m = verdictLine.Match(lines[i]);
                    if (m.Success && m.Groups[1].Value.Equals("PASS", StringComparison.OrdinalIgnoreCase))
                    {
                        passing.Add($"{Rel(path)}:{i + 1} — {lines[i].Trim()}");
                    }
                }
            }

            if (openS1.Count > 0)
            {
                Assert.That(passing, Is.Empty,
                    $"{openS1.Count} S1 defect(s) read as open, and the contract blocks every gate "
                    + "while that is true, but these gate reviews claim PASS. Either close the S1s or "
                    + "withdraw the verdicts - a PASS recorded under an open S1 is a claim the "
                    + "contract forbids.\n  Open S1: " + string.Join(", ", openS1)
                    + "\n  Claiming PASS: " + string.Join("; ", passing));
            }
        }

        /// <summary>
        /// Each register declares whether it is authoritative.
        ///
        /// Two registers were disjoint on 2026-08-18 - `defect-register.md` (D-001..D-017, zero UX
        /// entries) and `ux-defect-list.md` (sixteen UX entries, zero D entries) - and neither said
        /// which one a reader should believe about a given defect. A machine cannot pick either, and
        /// a human picked whichever they opened first.
        ///
        /// Scoped to files whose NAME claims to be a register, deliberately. A severity table can
        /// legitimately appear inside an audit or a survey, and demanding a role line from every
        /// document that quotes a severity would make this gate noise.
        /// </summary>
        [Test]
        public void EveryRegisterFile_DeclaresWhetherItIsAuthoritative()
        {
            if (!Directory.Exists(WorkspaceRoot)) Assert.Ignore("no live workspace");

            var registers = Directory.GetFiles(WorkspaceRoot, "*.md", SearchOption.AllDirectories)
                .Where(IsRegisterFile)
                .ToList();

            Assert.That(registers, Is.Not.Empty,
                $"No file under '{WorkspaceRoot}' names itself a register or defect list, so this "
                + "test asserted nothing. If the registers were renamed, this walk needs to know.");

            var silent = registers
                .Where(p => !RoleLine.IsMatch(File.ReadAllText(p)))
                .Select(Rel)
                .ToList();

            Assert.That(silent, Is.Empty,
                "These registers do not declare their role, so nothing resolves a disagreement "
                + "between them. Add a header line 'register-role: canonical' (the file to believe "
                + "for its ID range), 'derived' (an audit view whose IDs must exist in a canonical "
                + "register), or 'rollup' (an index). Silent: " + string.Join(", ", silent));
        }
    }
}
