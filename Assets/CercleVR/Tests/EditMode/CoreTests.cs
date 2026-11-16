using System.Collections.Generic;
using System.Linq;
using CercleVR.Core;
using NUnit.Framework;

namespace CercleVR.Tests
{
    public sealed class CoreTests
    {
        public static IEnumerable<TestCaseData> Matrix()
        {
            for (int count = 7; count <= 12; count++)
                for (int tier = 0; tier <= 2; tier++)
                    for (int evil = 1; evil <= 2; evil++)
                        for (int seed = 0; seed < 20; seed++)
                            yield return new TestCaseData(count, tier, evil, seed)
                                .SetName($"Unique_N{count}_T{tier}_E{evil}_S{seed}");
        }

        [TestCaseSource(nameof(Matrix))]
        public void GenerationHasExactlyTheIntendedSolution(int count, int tier, int evil, int seed)
        {
            var puzzle = PuzzleEngine.Generate(seed, count, tier, evil);
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.Count, Is.EqualTo(count));
            Assert.That(puzzle.Tier, Is.EqualTo(tier));
            Assert.That(PuzzleEngine.Bits(puzzle.EvilMask), Is.EqualTo(evil));
            Assert.That(PuzzleEngine.Bits(puzzle.CorruptMask), Is.EqualTo(tier));
            Assert.That(puzzle.EvilMask & puzzle.CorruptMask, Is.Zero);
            Assert.That(PuzzleEngine.Compatible(puzzle, puzzle.EvilMask, puzzle.CorruptMask), Is.True);
            Assert.That(PuzzleEngine.Solutions(puzzle), Is.EqualTo(new[] { puzzle.EvilMask }));
        }

        [Test]
        public void SameSeedAndParametersProduceIdenticalPuzzles(
            [Range(7, 12)] int count, [Range(0, 2)] int tier, [Values(1, 2)] int evil)
        {
            var first = PuzzleEngine.Generate(20261104, count, tier, evil);
            var again = PuzzleEngine.Generate(20261104, count, tier, evil);
            Assert.That(again.Seed, Is.EqualTo(first.Seed));
            Assert.That(again.EvilMask, Is.EqualTo(first.EvilMask));
            Assert.That(again.Poisoner, Is.EqualTo(first.Poisoner));
            Assert.That(again.Poisoned, Is.EqualTo(first.Poisoned));
            Assert.That(again.Drunk, Is.EqualTo(first.Drunk));
            Assert.That(again.Clues.Select(c => $"{c.Kind}:{c.Target}:{c.Value}"),
                Is.EqualTo(first.Clues.Select(c => $"{c.Kind}:{c.Target}:{c.Value}")));
        }

        [Test]
        public void CampaignProgressionAndRestartFollowTheSixLevelContract()
        {
            var campaign = new CampaignProgress(6);
            int[] counts = { 7, 8, 9, 10, 11, 12 };
            int[] lessons = { 0, 1, 2, 2, 2, 2 };
            int[] evils = { 1, 1, 1, 1, 2, 2 };
            for (int i = 0; i < 6; i++)
            {
                Assert.That(campaign.LevelIndex, Is.EqualTo(i));
                Assert.That(campaign.LevelNumber, Is.EqualTo(i + 1));
                Assert.That(campaign.NpcCount, Is.EqualTo(counts[i]));
                Assert.That(campaign.Lesson, Is.EqualTo(lessons[i]));
                Assert.That(campaign.EvilCount, Is.EqualTo(evils[i]));
                Assert.That(campaign.Advance(), Is.False, "Cannot advance before winning.");
                Assert.That(campaign.RecordVictory(), Is.True);
                Assert.That(campaign.RecordVictory(), Is.False, "Victory is recorded once.");
                Assert.That(campaign.Completed, Is.EqualTo(i == 5));
                Assert.That(campaign.WaitingForNextLevel, Is.EqualTo(i < 5));
                Assert.That(campaign.Advance(), Is.EqualTo(i < 5));
            }
            campaign.Restart();
            Assert.That(campaign.LevelNumber, Is.EqualTo(1));
            Assert.That(campaign.Completed, Is.False);
            Assert.That(campaign.WaitingForNextLevel, Is.False);
            Assert.That(campaign.NpcCount, Is.EqualTo(7));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void InnocentExecutionCostsFiveAndTwoInnocentsLose(int evilCount)
        {
            var puzzle = PuzzleEngine.Generate(42, 7, 0, evilCount);
            var session = new Session(puzzle);
            var innocents = Enumerable.Range(0, 7).Where(i => !PuzzleEngine.Has(puzzle.EvilMask, i)).ToArray();
            Assert.That(session.Execute(innocents[0]), Is.True);
            Assert.That(session.Health, Is.EqualTo(5));
            Assert.That(session.Outcome, Is.EqualTo(Outcome.Playing));
            Assert.That(session.Execute(innocents[0]), Is.False);
            Assert.That(session.Health, Is.EqualTo(5));
            Assert.That(session.Execute(innocents[1]), Is.True);
            Assert.That(session.Health, Is.Zero);
            Assert.That(session.Outcome, Is.EqualTo(Outcome.Lost));
            Assert.That(session.Execute(innocents[2]), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void OnlyEliminatingAllEvilsWins(int evilCount)
        {
            var puzzle = PuzzleEngine.Generate(42, 7, 0, evilCount);
            var session = new Session(puzzle);
            var evils = Enumerable.Range(0, 7).Where(i => PuzzleEngine.Has(puzzle.EvilMask, i)).ToArray();
            for (int i = 0; i < evils.Length; i++)
            {
                Assert.That(session.Execute(evils[i]), Is.True);
                Assert.That(session.Outcome, Is.EqualTo(i == evils.Length - 1 ? Outcome.Won : Outcome.Playing));
            }
            Assert.That(session.Health, Is.EqualTo(10));
            int innocent = Enumerable.Range(0, 7).First(i => !PuzzleEngine.Has(puzzle.EvilMask, i));
            Assert.That(session.Execute(innocent), Is.False);
        }
    }
}
