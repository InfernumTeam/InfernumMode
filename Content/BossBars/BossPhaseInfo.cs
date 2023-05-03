using System.Collections.Generic;

namespace InfernumMode.Content.BossBars
{
    public struct BossPhaseInfo
    {
        public int NPCType;
        public int PhaseCount;
        public List<float> PhaseThresholds;

        public BossPhaseInfo(int npcType, List<float> phaseThresholds)
        {
            NPCType = npcType;
            PhaseCount = phaseThresholds.Count;
            PhaseThresholds = phaseThresholds;
        }
    }
}
