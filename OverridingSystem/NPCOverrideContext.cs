using System;

namespace InfernumMode.OverridingSystem
{
    [Flags]
    public enum NPCOverrideContext
    {
        NPCAI = 1,
        NPCSetDefaults = 2,
        NPCPreDraw = 4,
        NPCFindFrame = 8
    }
}
