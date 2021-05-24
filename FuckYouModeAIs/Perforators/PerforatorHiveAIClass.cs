using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Projectiles.Boss;
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
            ref float attackTimer = ref npc.ai[1];
            ref float wormSpawnState = ref npc.localAI[0];

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
            bool anyWorms = NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadSmall>()) || NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadMedium>()) || NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadLarge>());
            npc.dontTakeDamage = anyWorms || (!target.ZoneCorrupt && !target.ZoneCrimson);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lifeRatio < 0.7f && wormSpawnState == 0f)
                {
                    NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<PerforatorHeadSmall>());
                    wormSpawnState = 1f;
                }

                if (lifeRatio < 0.5f && wormSpawnState == 1f)
                {
                    NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<PerforatorHeadMedium>());
                    wormSpawnState = 2f;
                }

                if (lifeRatio < 0.35f && wormSpawnState == 2f)
                {
                    NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<PerforatorHeadLarge>());
                    wormSpawnState = 3f;
                }

                if (lifeRatio < 0.15f && wormSpawnState == 3f)
                {
                    for (int i = 0; i < 3; i++)
                        NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<PerforatorHeadSmall>());
                    wormSpawnState = 4f;
                }
            }

            if (attackState == 0f)
            {
                DoAttack_HoverNearTarget(npc, target, ref attackTimer, anyWorms, out bool gotoNextAttack);
                if (gotoNextAttack)
                {
                    attackTimer = 0f;
                    attackState = 1f;
                    npc.netUpdate = true;
                }
            }
            else if (attackState == 1f)
            {
                DoAttack_SwoopTowardsPlayer(npc, target, ref attackTimer, anyWorms, out bool gotoNextAttack);
                if (gotoNextAttack)
                {
                    attackTimer = 0f;
                    attackState = 2f;
                    npc.netUpdate = true;
                }
            }
            else if (attackState == 2f)
            {
                DoAttack_ReleaseRegularBursts(npc, target, ref attackTimer, anyWorms, out bool gotoNextAttack);
                if (gotoNextAttack)
                {
                    attackTimer = 0f;
                    attackState = 0f;
                    npc.netUpdate = true;
                }
            }

            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.04f, -MathHelper.Pi / 6f, MathHelper.Pi / 6f);

            attackTimer++;
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

        public static void DoAttack_SwoopTowardsPlayer(NPC npc, Player target, ref float attackTimer, bool anyWorms, out bool gotoNextAttack)
		{
            // Hover above the target before swooping.
            if (attackTimer < 90f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 360f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 14f, anyWorms ? 0.054f : 0.07f);

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
                if (anyWorms)
                    npc.velocity *= 0.8f;
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

            gotoNextAttack = attackTimer >= 215f;
        }

        public static void DoAttack_HoverNearTarget(NPC npc, Player target, ref float attackTimer, bool anyWorms, out bool gotoNextAttack)
        {
            if (attackTimer % 120f > 85f)
            {
                npc.velocity *= 0.97f;

                // Release ichor everywhere.
                int shootRate = anyWorms ? 10 : 6;
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
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                if (!npc.WithinRange(destination, 145f))
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 15f, 0.055f);
                else
                    npc.velocity *= 0.95f;
            }

            gotoNextAttack = attackTimer >= 240f;
        }

        public static void DoAttack_ReleaseRegularBursts(NPC npc, Player target, ref float attackTimer, bool anyWorms, out bool gotoNextAttack)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 270f;
            if (!npc.WithinRange(destination, 145f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 15f, 0.07f);
            else
                npc.velocity *= 0.95f;

            int shootRate = anyWorms ? 100 : 60;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
            {
                int totalProjectiles = anyWorms ? 10 : 16;
                float blobSpeed = anyWorms ? 6f : 8f;
                Vector2 blobSpawnPosition = new Vector2(npc.Center.X, npc.Center.Y + 30f);
                Vector2 currentBlobVelocity = Vector2.UnitY * -blobSpeed + Vector2.UnitX * npc.velocity.SafeNormalize(Vector2.Zero).X;

                npc.TargetClosest();

                currentBlobVelocity.X -= blobSpeed * 0.5f;
                for (int i = 0; i < totalProjectiles + 1; i++)
                {
                    Utilities.NewProjectileBetter(blobSpawnPosition, currentBlobVelocity, ModContent.ProjectileType<IchorShot>(), 65, 0f, Main.myPlayer, 0f, 0f);
                    currentBlobVelocity.X += blobSpeed / totalProjectiles * npc.direction;
                }
                Main.PlaySound(SoundID.NPCHit20, npc.position);
            }

            gotoNextAttack = attackTimer >= 300f;
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
