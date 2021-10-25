using CalamityMod.NPCs.Calamitas;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;
using CalamitasCloneNPC = CalamityMod.NPCs.Calamitas.Calamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CalamitasCloneP1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasCloneNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            npc.Transform(ModContent.NPCType<CalamitasRun3>());
            return false;
        }
    }
}
