using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.BossRush;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldUpdatingSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            BossRushChanges.HandleTeleports();
            if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                CalamityGlobalNPC.draedon = -1;
        }
    }
}