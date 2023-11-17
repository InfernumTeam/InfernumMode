using System.Collections.Generic;

namespace InfernumMode.Content.BossBars
{
    public readonly struct BossPhaseInfo
    {
        public readonly int NPCType;

        public readonly int PhaseCount;

        public readonly List<float> PhaseThresholds;

        public BossPhaseInfo(int npcType, List<float> phaseThresholds)
        {
            NPCType = npcType;
            PhaseCount = phaseThresholds.Count;
            PhaseThresholds = phaseThresholds;
        }
    }
}
