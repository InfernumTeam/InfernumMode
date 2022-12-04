using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.GreatSandShark;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.BossRush;
using InfernumMode.Miscellaneous;
using InfernumMode.Projectiles.Wayfinder;
using InfernumMode.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Map;
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

            if (WorldSaveSystem.WayfinderGateLocation != Vector2.Zero)
            {              
                bool gateExists = false;
                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile projectile = Main.projectile[i];

                    if (projectile.type == ModContent.ProjectileType<WayfinderGate>() && projectile.active)
                    {
                        gateExists = true;
                        break;
                    }
                }

                if (!gateExists && Main.netMode is not NetmodeID.MultiplayerClient)
                    Projectile.NewProjectileDirect(Entity.GetSource_None(), WorldSaveSystem.WayfinderGateLocation, Vector2.Zero, ModContent.ProjectileType<WayfinderGate>(), 0, 0, Main.myPlayer);
            }
        }
    }
}