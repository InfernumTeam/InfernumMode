using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldUpdatingSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            // Disable natural GSS spawns.
            if (InfernumMode.CanUseCustomAIs)
                CalamityMod.CalamityMod.sharkKillCount = 0;

            if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                CalamityGlobalNPC.draedon = -1;
        }
    }
}