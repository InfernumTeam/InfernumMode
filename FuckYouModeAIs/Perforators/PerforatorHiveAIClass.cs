using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
    public class PerforatorHiveAIClass
    {
		#region AI

		#region Main Boss
		[OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorHiveAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorHiveAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            npc.damage = 0;

            ref float attackState = ref npc.ai[0];
            ref float time = ref npc.ai[1];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
				{
                    DoDespawnEffects(npc);
                    return false;
				}
            }

            Player target = Main.player[npc.target];

            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.04f, -MathHelper.Pi / 6f, MathHelper.Pi / 6f);

            time++;
            return false;
		}

        #region Specific Attacks
        public static void DoDespawnEffects(NPC npc)
		{
            npc.damage = 0;
            npc.velocity = Vector2.Lerp(npc.Center, Vector2.UnitY * 21f, 0.08f);
            if (npc.timeLeft > 225)
                npc.timeLeft = 225;
        }

        public static void DoAttack_SwoopTowardsPlayer(NPC npc, Player target, bool angy, ref float attackTimer, ref float attackType)
		{
            // Hover above the target before swooping.
            if (attackTimer < 90f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 360f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 14f, 0.12f);

                if (npc.WithinRange(destination, 35f))
				{
                    attackTimer = 90;
                    npc.netUpdate = true;
				}
            }

            // Play a roar sound before swooping.
            if (attackTimer == 90f)
            {
                Main.PlaySound(SoundID.Roar, target.Center, 0);
                npc.velocity = npc.SafeDirectionTo(target.Center) * new Vector2(8f, 20f);
                if (angy)
                    npc.velocity *= new Vector2(1.2f, 1.3f);
                npc.netUpdate = true;

                npc.TargetClosest();
            }

            // Swoop.
            if (attackTimer >= 90f && attackTimer <= 180f)
            {
                npc.velocity = npc.velocity.RotatedBy(MathHelper.PiOver2 / 90f * -npc.direction);
                npc.damage = 72;
            }

            if (attackTimer > 180f)
                npc.velocity *= 0.97f;

            if (attackTimer >= 215f)
			{
                attackType++;
                attackTimer = 0f;
                npc.netUpdate = true;
			}
		}

        public static void DoAttack_HoverNearTarget(NPC npc, Player target, bool angy, ref float attackTimer, ref float attackType)
        {
            Vector2 offset = (MathHelper.TwoPi * 2f * attackTimer / 180f).ToRotationVector2() * 300f;

            if (attackTimer % 120f > 85f)
            {
                // Play a roar sound before swooping.
                if (attackTimer % 120f == 90f)
                    Main.PlaySound(SoundID.Roar, target.Center, 0);

                npc.velocity *= 0.97f;

                // Release ichor everywhere.
                int shootRate = angy ? 4 : 6;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8.4f);
                    Vector2 spawnPosition = npc.Center - Vector2.UnitY * 45f + Main.rand.NextVector2Circular(30f, 30f);

                    int ichor = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), 65, 0f);
                    if (Main.projectile.IndexInRange(ichor))
                        Main.projectile[ichor].ai[1] = 1f;
                }
            }
            else
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center + offset) * 15f, angy ? 0.185f : 0.1f);

            if (attackTimer >= 240f)
            {
                attackType++;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        #endregion Specific Attacks
        #endregion Main Boss

        #region Worms

        [OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorHiveAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorWormHeadSmallAI(NPC npc)
		{
            ref float hasSummonedSegments = ref npc.localAI[0];

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
			{
                SpawnSegments(npc, 10, ModContent.NPCType<PerforatorBodySmall>(), ModContent.NPCType<PerforatorTailSmall>());
                hasSummonedSegments = 1f;
			}

            return false;
		}

        public static void SpawnSegments(NPC npc, int segmentCount, int bodyType, int tailType)
		{
            int aheadSegment = npc.whoAmI;
            for (int i = 0; i < segmentCount; i++)
            {
                int meme;
                if (i >= 0 && i < segmentCount - 1)
                    meme = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    meme = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[meme].realLife = npc.whoAmI;
                Main.npc[meme].ai[3] = npc.whoAmI;
                Main.npc[meme].ai[1] = aheadSegment;
                Main.npc[aheadSegment].ai[0] = meme;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, meme);
                aheadSegment = meme;
            }
        }
        #endregion Worms

        #endregion AI
    }
}
