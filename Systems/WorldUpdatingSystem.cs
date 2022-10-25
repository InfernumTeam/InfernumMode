using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.BossRush;
using InfernumMode.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldUpdatingSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            CalamityMod.CalamityMod.sharkKillCount = 0;

            BossRushChanges.HandleTeleports();
            if (!NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                CalamityGlobalNPC.draedon = -1;

            if (!SubworldSystem.IsActive<LostColosseum>())
            {
                LostColosseum.HasBereftVassalAppeared = false;
                LostColosseum.HasBereftVassalBeenDefeated = false;
            }

            if (!LostColosseum.HasBereftVassalAppeared && SubworldSystem.IsActive<LostColosseum>())
            {
                int x = Main.maxTilesX * 8 + 1200;
                int y = Main.maxTilesY * 8;
                NPC.NewNPC(new EntitySource_WorldEvent(), x, y, ModContent.NPCType<BereftVassal>(), 1);
                LostColosseum.HasBereftVassalAppeared = true;
            }
        }
    }
}