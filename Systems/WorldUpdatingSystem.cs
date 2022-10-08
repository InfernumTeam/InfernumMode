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
            if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                CalamityGlobalNPC.draedon = -1;
        }
    }
}