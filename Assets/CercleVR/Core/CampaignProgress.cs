using System;

namespace CercleVR.Core
{
    // Progress survives between puzzles. A puzzle victory and the campaign ending are distinct.
    public sealed class CampaignProgress
    {
        public int TotalLevels { get; private set; }
        public int LevelIndex { get; private set; }
        public int LevelNumber { get { return LevelIndex + 1; } }
        public bool WaitingForNextLevel { get; private set; }
        public bool Completed { get; private set; }
        public int NpcCount { get { return Math.Min(12, 7 + LevelIndex); } }
        public int Lesson { get { return Math.Min(2, LevelIndex); } }
        public int EvilCount { get { return LevelIndex >= 4 ? 2 : 1; } }

        public CampaignProgress(int totalLevels)
        {
            TotalLevels = Math.Max(1, Math.Min(50, totalLevels));
        }
        public bool RecordVictory()
        {
            if (Completed || WaitingForNextLevel) return false;
            if (LevelNumber == TotalLevels) Completed = true;
            else WaitingForNextLevel = true;
            return true;
        }
        public bool Advance()
        {
            if (!WaitingForNextLevel || Completed) return false;
            LevelIndex++;
            WaitingForNextLevel = false;
            return true;
        }
        public void Restart()
        {
            LevelIndex = 0;
            WaitingForNextLevel = Completed = false;
        }
        public int SeedForLevel(int baseSeed)
        {
            return unchecked(baseSeed + LevelIndex * 1009);
        }
    }
}
