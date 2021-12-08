using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Abyss;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            AbyssalCrash,
            HadalSpirits,
            PsychicBlasts,
            UndynesTail
        }

        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmHeadHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Use the default AI if SCal and Draedon are not both dead.
            if (!CalamityWorld.downedExoMechs || !CalamityWorld.downedSCal)
                return true;

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Disappear if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Set the whoAmI variable.
            CalamityGlobalNPC.adultEidolonWyrmHead = npc.whoAmI;

            // Do enrage checks.
            bool enraged = ArenaSpawnAndEnrageCheck(npc, target);
            npc.Calamity().CurrentlyEnraged = enraged;

            float generalDamageFactor = enraged ? 40f : 1f;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasInitialized = ref npc.localAI[0];

            // Do initializations.
            if (hasInitialized == 0f)
			{
                npc.Opacity = 1f;

                int Previous = npc.whoAmI;
                for (int i = 0; i < 41; i++)
                {
                    int lol;
                    if (i >= 0 && i < 40)
                    {
                        if (i % 2 == 0)
                            lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmBodyHuge>(), npc.whoAmI);
                        else
                            lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmBodyAltHuge>(), npc.whoAmI);
                    }
                    else
                        lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<EidolonWyrmTailHuge>(), npc.whoAmI);

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[2] = npc.whoAmI;
                    Main.npc[lol].ai[1] = Previous;
                    Main.npc[Previous].ai[0] = lol;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                    Previous = lol;
                    Main.npc[Previous].ai[3] = i / 2;
                }
                hasInitialized = 1f;
			}

            // Reset damage and other things.
            npc.damage = (int)(npc.defDamage * generalDamageFactor);

            switch ((AEWAttackType)(int)attackType)
            {
                case AEWAttackType.AbyssalCrash:
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float currentPhase = ref npc.Infernum().ExtraAI[5];
            ref float attackCycleType = ref npc.Infernum().ExtraAI[6];
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[7];
        }

        public static bool ArenaSpawnAndEnrageCheck(NPC npc, Player player)
        {
            ref float enraged01Flag = ref npc.ai[2];
            ref float spawnedArena01Flag = ref npc.ai[3];

            // Create the arena, but not as a multiplayer client.
            // In single player, the arena gets created and never gets synced because it's single player.
            if (spawnedArena01Flag == 0f)
            {
                spawnedArena01Flag = 1f;
                enraged01Flag = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int width = 9600;
                    npc.Infernum().arenaRectangle.X = (int)(player.Center.X - width * 0.5f);
                    npc.Infernum().arenaRectangle.Y = (int)(player.Center.Y - 160000f);
                    npc.Infernum().arenaRectangle.Width = width;
                    npc.Infernum().arenaRectangle.Height = 320000;
                    Vector2 spawnPosition = player.Center + new Vector2(width * 0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                    spawnPosition = player.Center + new Vector2(width * -0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                }

                // Force Yharon to send a sync packet so that the arena gets sent immediately
                npc.netUpdate = true;
            }
            // Enrage code doesn't run on frame 1 so that Yharon won't be enraged for 1 frame in multiplayer
            else
            {
                var arena = npc.Infernum().arenaRectangle;
                enraged01Flag = (!player.Hitbox.Intersects(arena)).ToInt();
                if (enraged01Flag == 1f)
                    return true;
            }
            return false;
        }
    }
}
