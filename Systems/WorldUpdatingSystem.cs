using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.GreatSandShark;
using InfernumMode.Achievements;
using InfernumMode.Achievements.UI;
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

                if (LostColosseum.HasBereftVassalBeenDefeated)
                {
                    LostColosseum.HasBereftVassalBeenDefeated = false;

                    // Register the great sand shark and bereft vassal as dead in the bestiary if they were successfully defeated in the subworld.
                    NPC fakeNPC = new();
                    fakeNPC.SetDefaults(ModContent.NPCType<BereftVassal>());
                    for (int i = 0; i < 100; i++)
                        Main.BestiaryTracker.Kills.RegisterKill(fakeNPC);

                    fakeNPC.SetDefaults(ModContent.NPCType<GreatSandShark>());
                    for (int i = 0; i < 100; i++)
                        Main.BestiaryTracker.Kills.RegisterKill(fakeNPC);
                }
            }

            if (!LostColosseum.HasBereftVassalAppeared && SubworldSystem.IsActive<LostColosseum>() && !Main.LocalPlayer.dead)
            {
                int x = Main.maxTilesX * 8 + 1200;
                int y = Main.maxTilesY * 8;
                NPC.NewNPC(new EntitySource_WorldEvent(), x, y, ModContent.NPCType<BereftVassal>(), 1);
                LostColosseum.HasBereftVassalAppeared = true;
            }

            AchievementManager.UpdateAchievements();
        }
    }
}