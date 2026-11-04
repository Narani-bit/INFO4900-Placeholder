using System;
using System.Collections.Generic;

namespace CercleVR.Core
{
    // Pure C#: no reference to Unity. Seats never move after an execution.
    public enum ClueKind { Neighbours, Distance, Direction, Witness }

    [Serializable]
    public sealed class Clue
    {
        public ClueKind Kind;
        public int Target;
        public int Value;
    }

    [Serializable]
    public sealed class Puzzle
    {
        public int Seed, Count, EvilCount, Tier;
        public int EvilMask, Poisoner = -1, Poisoned = -1, Drunk = -1;
        public Clue[] Clues;
        public int CorruptMask
        {
            get { return (Poisoned < 0 ? 0 : 1 << Poisoned) | (Drunk < 0 ? 0 : 1 << Drunk); }
        }
    }

    public struct RandomStream
    {
        private uint state;
        public RandomStream(int seed) { state = unchecked((uint)seed); if (state == 0) state = 1; }
        public int Next(int limit)
        {
            if (limit < 1) throw new ArgumentOutOfRangeException("limit");
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (int)(state % (uint)limit);
        }
    }

    public static class PuzzleEngine
    {
        public static bool Has(int mask, int seat) { return (mask & (1 << seat)) != 0; }
        public static int Bits(int mask)
        {
            int n = 0; while (mask != 0) { mask &= mask - 1; n++; } return n;
        }
        public static int Wrap(int seat, int count) { return (seat % count + count) % count; }

        // Truth is evaluated against the INITIAL board. Death does not rewrite clues.
        public static int Truth(Clue clue, int speaker, int count, int evilMask)
        {
            if (clue.Kind == ClueKind.Neighbours)
                return (Has(evilMask, Wrap(speaker - 1, count)) ? 1 : 0)
                     + (Has(evilMask, Wrap(speaker + 1, count)) ? 1 : 0);
            if (clue.Kind == ClueKind.Witness) return Has(evilMask, clue.Target) ? 1 : 0;
            int clockwise = count + 1, counter = count + 1;
            for (int j = 0; j < count; j++)
            {
                if (j == speaker || !Has(evilMask, j)) continue;
                clockwise = Math.Min(clockwise, Wrap(j - speaker, count));
                counter = Math.Min(counter, Wrap(speaker - j, count));
            }
            if (clockwise > count) return 0;
            if (clue.Kind == ClueKind.Distance) return Math.Min(clockwise, counter);
            return clockwise == counter ? 3 : (clockwise < counter ? 1 : 2);
        }

        public static bool Compatible(Puzzle p, int evilMask, int corruptMask)
        {
            if ((evilMask & corruptMask) != 0) return false;
            for (int i = 0; i < p.Count; i++)
            {
                bool correct = p.Clues[i].Value == Truth(p.Clues[i], i, p.Count, evilMask);
                bool shouldTellTruth = !Has(evilMask | corruptMask, i);
                if (correct != shouldTellTruth) return false;
            }
            return true;
        }

        // Candidate statuses are inferred from public rules; hidden fields are never read here.
        private static bool HasCompatibleStatuses(Puzzle p, int evilMask)
        {
            if (p.Tier == 0) return Compatible(p, evilMask, 0);
            for (int poisoner = 0; poisoner < p.Count; poisoner++)
            {
                if (!Has(evilMask, poisoner)) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    int target = Wrap(poisoner + side, p.Count);
                    if (Has(evilMask, target)) continue;
                    if (p.Tier == 1 && Compatible(p, evilMask, 1 << target)) return true;
                    if (p.Tier != 2) continue;
                    for (int drunk = 0; drunk < p.Count; drunk++)
                    {
                        if (drunk == target || Has(evilMask, drunk)) continue;
                        if (Compatible(p, evilMask, (1 << target) | (1 << drunk))) return true;
                    }
                }
            }
            return false;
        }

        public static List<int> Solutions(Puzzle p, int stopAfter = 2)
        {
            var result = new List<int>();
            for (int mask = 0; mask < (1 << p.Count); mask++)
            {
                if (Bits(mask) != p.EvilCount || !HasCompatibleStatuses(p, mask)) continue;
                result.Add(mask);
                if (result.Count >= stopAfter) break;
            }
            return result;
        }

        // One attempt: the presentation can yield one frame between attempts on the headset.
        public static Puzzle TryGenerate(int seed, int count, int tier, int evilCount)
        {
            if (count < 7 || count > 12 || tier < 0 || tier > 2 || evilCount < 1 || evilCount > 2)
                throw new ArgumentOutOfRangeException("Use 7..12 seats, tier 0..2 and 1..2 evils.");
            var random = new RandomStream(seed);
            var p = new Puzzle { Seed = seed, Count = count, Tier = tier, EvilCount = evilCount, Clues = new Clue[count] };
            while (Bits(p.EvilMask) < evilCount) p.EvilMask |= 1 << random.Next(count);
            if (tier > 0)
            {
                var evils = new List<int>();
                for (int i = 0; i < count; i++) if (Has(p.EvilMask, i)) evils.Add(i);
                p.Poisoner = evils[random.Next(evils.Count)];
                var targets = new List<int>();
                int left = Wrap(p.Poisoner - 1, count), right = Wrap(p.Poisoner + 1, count);
                if (!Has(p.EvilMask, left)) targets.Add(left);
                if (!Has(p.EvilMask, right)) targets.Add(right);
                if (targets.Count == 0) return null;
                p.Poisoned = targets[random.Next(targets.Count)];
            }
            if (tier > 1)
            {
                do { p.Drunk = random.Next(count); }
                while (Has(p.EvilMask, p.Drunk) || p.Drunk == p.Poisoned);
            }
            for (int i = 0; i < count; i++)
            {
                var clue = new Clue { Kind = (ClueKind)random.Next(4) };
                do { clue.Target = random.Next(count); } while (clue.Target == i);
                int truth = Truth(clue, i, count, p.EvilMask);
                int options = clue.Kind == ClueKind.Neighbours ? 3 :
                    clue.Kind == ClueKind.Distance ? count / 2 + 1 :
                    clue.Kind == ClueKind.Direction ? 4 : 2;
                clue.Value = Has(p.EvilMask | p.CorruptMask, i)
                    ? (truth + 1 + random.Next(options - 1)) % options : truth;
                p.Clues[i] = clue;
            }
            var solutions = Solutions(p);
            return solutions.Count == 1 && solutions[0] == p.EvilMask ? p : null;
        }

        public static Puzzle Generate(int seed, int count, int tier, int evilCount, int attempts = 256)
        {
            for (int k = 0; k < attempts; k++)
            {
                var p = TryGenerate(unchecked(seed + k * 7919), count, tier, evilCount);
                if (p != null) return p;
            }
            throw new InvalidOperationException("No unique puzzle found in the generation budget.");
        }

        public static string RoleName(ClueKind kind)
        {
            switch (kind) { case ClueKind.Neighbours: return "Guetteur"; case ClueKind.Distance: return "Pisteur";
                case ClueKind.Direction: return "Boussole"; default: return "Temoin"; }
        }
        public static string Statement(Puzzle p, int seat)
        {
            var c = p.Clues[seat];
            switch (c.Kind)
            {
                case ClueKind.Neighbours: return c.Value + " voisin(s) malefique(s).";
                case ClueKind.Distance: return c.Value == 0 ? "Aucun autre malefique dans le cercle." : "Le malefique le plus proche est a " + c.Value + " siege(s).";
                case ClueKind.Direction: return new[] { "Aucun autre malefique.", "Le plus proche est dans le sens horaire.", "Le plus proche est dans le sens antihoraire.", "Les deux sens sont a egale distance." }[c.Value];
                default: return "Le personnage " + (c.Target + 1) + (c.Value == 1 ? " est malefique." : " est innocent.");
            }
        }
    }

    public enum Outcome { Playing, Won, Lost }
    public enum PublicEventKind { Revealed, Executed }
    public sealed class PublicEvent
    {
        public readonly int Sequence, Seat, HealthAfter;
        public readonly PublicEventKind Kind;
        public readonly string Text;
        public PublicEvent(int sequence, int seat, PublicEventKind kind, string text, int health)
        { Sequence = sequence; Seat = seat; Kind = kind; Text = text; HealthAfter = health; }
    }
    public sealed class Session
    {
        public const int MaxHealth = 10;
        public const int InnocentExecutionDamage = 5;
        public readonly Puzzle Puzzle;
        public readonly bool[] Revealed, Eliminated;
        public readonly int[] Marks;
        public readonly int[] RevealOrder;
        private readonly List<PublicEvent> history = new List<PublicEvent>();
        private int revealCount;
        public IReadOnlyList<PublicEvent> History { get; private set; }
        public int Health { get; private set; }
        public int EvilsKilled
        {
            get { int result = 0; for (int i = 0; i < Puzzle.Count; i++)
                if (Eliminated[i] && PuzzleEngine.Has(Puzzle.EvilMask, i)) result++; return result; }
        }
        public Outcome Outcome { get; private set; }
        public Session(Puzzle puzzle)
        {
            Puzzle = puzzle; Health = MaxHealth; Outcome = Outcome.Playing;
            Revealed = new bool[puzzle.Count]; Eliminated = new bool[puzzle.Count]; Marks = new int[puzzle.Count];
            RevealOrder = new int[puzzle.Count]; History = history.AsReadOnly();
        }
        public bool Reveal(int seat)
        {
            if (seat < 0 || seat >= Puzzle.Count || Outcome != Outcome.Playing || Eliminated[seat] || Revealed[seat]) return false;
            Revealed[seat] = true; RevealOrder[seat] = ++revealCount;
            history.Add(new PublicEvent(history.Count + 1, seat, PublicEventKind.Revealed,
                PuzzleEngine.RoleName(Puzzle.Clues[seat].Kind) + " : " + PuzzleEngine.Statement(Puzzle, seat), Health));
            return true;
        }
        // Hidden identity is disclosed only after execution or at the end of the puzzle.
        public string PublicIdentity(int seat)
        {
            if (seat < 0 || seat >= Puzzle.Count) throw new ArgumentOutOfRangeException("seat");
            if (!Eliminated[seat] && Outcome == Outcome.Playing)
                return Revealed[seat] ? PuzzleEngine.RoleName(Puzzle.Clues[seat].Kind) : "Inconnu";
            if (seat == Puzzle.Poisoner) return "Empoisonneur (malefique)";
            if (PuzzleEngine.Has(Puzzle.EvilMask, seat)) return "Malefique";
            if (seat == Puzzle.Drunk) return "Ivre (innocent)";
            return PuzzleEngine.RoleName(Puzzle.Clues[seat].Kind) +
                (seat == Puzzle.Poisoned ? " (innocent empoisonne)" : " (innocent)");
        }
        public bool Execute(int seat)
        {
            if (seat < 0 || seat >= Puzzle.Count || Outcome != Outcome.Playing || Eliminated[seat]) return false;
            Eliminated[seat] = true;
            bool evil = PuzzleEngine.Has(Puzzle.EvilMask, seat);
            if (!evil) Health = Math.Max(0, Health - InnocentExecutionDamage);
            history.Add(new PublicEvent(history.Count + 1, seat, PublicEventKind.Executed,
                "Execution : " + (evil ? "malefique" : "innocent, -5 sante") + ".", Health));
            if (Health <= 0) { Outcome = Outcome.Lost; return true; }
            for (int i = 0; i < Puzzle.Count; i++)
                if (PuzzleEngine.Has(Puzzle.EvilMask, i) && !Eliminated[i]) return true;
            Outcome = Outcome.Won; return true;
        }
    }
}
