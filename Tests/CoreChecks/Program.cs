using System;
using CercleVR.Core;

internal static class Program
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        int puzzles = 0;
        foreach (int seed in new[] { 20261022, 1, 42, 123456 })
        {
            var campaign = new CampaignProgress(6);
            for (int level = 0; level < 6; level++)
            {
                string context = $"seed={seed}, level={level + 1}";
                var puzzle = PuzzleEngine.Generate(campaign.SeedForLevel(seed),
                    campaign.NpcCount, campaign.Lesson, campaign.EvilCount);
                Check(puzzle != null, $"Generation: {context}");
                puzzles++;
                Check(PuzzleEngine.Bits(puzzle.EvilMask) == puzzle.EvilCount, $"Evil count: {context}");
                Check(PuzzleEngine.Compatible(puzzle, puzzle.EvilMask, puzzle.CorruptMask), $"Clues: {context}");
                var solutions = PuzzleEngine.Solutions(puzzle);
                Check(solutions.Count == 1 && solutions[0] == puzzle.EvilMask, $"Unique solution: {context}");

                var session = new Session(puzzle);
                for (int i = 0; i < puzzle.Count; i++)
                {
                    Check(session.Reveal(i), $"Reveal {i}: {context}");
                    Check(!session.Reveal(i), $"Repeated reveal {i}: {context}");
                }
                for (int i = 0; i < puzzle.Count; i++)
                    if (PuzzleEngine.Has(puzzle.EvilMask, i))
                        Check(session.Execute(i), $"Execution {i}: {context}");
                Check(session.Outcome == Outcome.Won && session.Health == 10, $"Victory: {context}");
                Check(!session.Execute(0), $"Post-victory execution: {context}");

                var lost = new Session(puzzle);
                int killed = 0;
                for (int i = 0; i < puzzle.Count && killed < 2; i++)
                    if (!PuzzleEngine.Has(puzzle.EvilMask, i))
                    {
                        lost.Execute(i);
                        killed++;
                    }
                Check(lost.Outcome == Outcome.Lost && lost.Health == 0, $"Innocent penalty: {context}");
                Check(campaign.RecordVictory(), $"Record victory: {context}");
                Check(!campaign.RecordVictory(), $"Duplicate victory: {context}");
                if (level < 5)
                    Check(campaign.Advance(), $"Advance: {context}");
                else
                    Check(campaign.Completed && !campaign.Advance(), $"Campaign completion: {context}");
            }
            campaign.Restart();
            Check(campaign.LevelNumber == 1 && !campaign.Completed, $"Restart: seed={seed}");
        }
        Console.WriteLine($"PASS: {checks} checks, {puzzles} puzzles, 4 complete campaigns. " +
            "Pure C# only; no Unity play/build validation.");
    }
}
