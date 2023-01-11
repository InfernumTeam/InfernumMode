using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.TheDestroyerTail;

        public override bool PreAI(NPC npc) => DestroyerBodyBehaviorOverride.DoBehavior(npc);
    }
}