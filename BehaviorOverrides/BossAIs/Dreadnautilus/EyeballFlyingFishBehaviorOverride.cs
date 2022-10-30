using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class EyeballFlyingFishBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EyeballFlyingFish;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (NPC.AnyNPCs(NPCID.BloodNautilus))
                npc.damage = 100;
            return true;
        }
    }
}
