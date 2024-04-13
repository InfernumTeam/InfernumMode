using System.Collections.Generic;

namespace InfernumMode.Content.BossBars
{
    public readonly struct BossPhaseInfo(int npcType, List<float> phaseThresholds)
    {
        public readonly int NPCType = npcType;

        public readonly int PhaseCount = phaseThresholds.Count;

        public readonly List<float> PhaseThresholds = phaseThresholds;
    }
}
