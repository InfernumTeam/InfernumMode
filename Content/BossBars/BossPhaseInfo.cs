using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
