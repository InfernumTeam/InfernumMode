using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.FuckYouModeAIs.EyeOfCthulhu;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            ref float summonAnimationCountdown = ref npc.ai[2];
            ref float animationState = ref npc.localAI[0];
            ref float wormSpawnState = ref npc.localAI[1];

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

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lifeRatio < 0.75f && animationState == 0f)
                {
                    animationState = 1f;
                    summonAnimationCountdown = 200f;
                    npc.netUpdate = true;
                }

                if (lifeRatio < 0.4f && animationState == 1f)
                {
                    animationState = 2f;
                    summonAnimationCountdown = 200f;
                    npc.netUpdate = true;
                }

                if (lifeRatio < 0.15f && animationState == 2f)
                {
                    animationState = 3f;
                    summonAnimationCountdown = 200f;
                    npc.netUpdate = true;
                }
            }

            npc.dontTakeDamage = anyWorms || (!target.ZoneCorrupt && !target.ZoneCrimson) || summonAnimationCountdown > 0f;

            if (summonAnimationCountdown > 0f)
            {
                npc.velocity *= 0.96f;
                npc.rotation *= 0.96f;

                if (summonAnimationCountdown % 20f == 0f)
                {
                    for (int i = -4; i <= 4; i++)
                    {
                        if (i == 0)
                            continue;
                        Vector2 offsetDirection = Vector2.UnitY.RotatedBy(i * 0.22f + Main.rand.NextFloat(-0.32f, 0.32f));
                        Vector2 baseSpawnPosition = npc.Center + offsetDirection * 450f;
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 dustSpawnPosition = baseSpawnPosition + Main.rand.NextVector2Circular(9f, 9f);
                            Vector2 dustVelocity = (npc.Center - dustSpawnPosition) * 0.08f;

                            Dust blood = Dust.NewDustPerfect(dustSpawnPosition, 5);
                            blood.scale = Main.rand.NextFloat(2.6f, 3f);
                            blood.velocity = dustVelocity;
                            blood.noGravity = true;
                        }
                    }
                }

                summonAnimationCountdown--;

                if (summonAnimationCountdown == 0f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        wormSpawnState = animationState;
                        int wormTypeToSpawn = ModContent.NPCType<PerforatorHeadSmall>();
                        switch ((int)wormSpawnState)
                        {
                            case 1:
                                wormTypeToSpawn = ModContent.NPCType<PerforatorHeadSmall>();
                                break;
                            case 2:
                                wormTypeToSpawn = ModContent.NPCType<PerforatorHeadMedium>();
                                break;
                            case 3:
                                wormTypeToSpawn = ModContent.NPCType<PerforatorHeadLarge>();
                                break;
                        }

                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, wormTypeToSpawn);
                    }

                    float explosionSpeed = 10f;
                    switch ((int)npc.localAI[0])
                    {
                        case 2:
                            explosionSpeed += 5f;
                            break;
                        case 3:
                            explosionSpeed += 12f;
                            break;
                    }
                    Utilities.CreateGenericDustExplosion(npc.Center, 5, 20, explosionSpeed, 3f);
                }

                return false;
            }

            // Hide undergroud if any worms are present.
            if (anyWorms)
            {
                if (!Collision.SolidCollision(npc.position, npc.width, npc.height) || !Collision.SolidCollision(npc.Center - Vector2.UnitY * 550f, 2, 2))
                    npc.position.Y += 5f;
                npc.velocity *= 0.8f;
                npc.rotation *= 0.8f;
                return false;
            }

            if (attackState == 0f)
            {
                DoAttack_HoverNearTarget(npc, target, ref attackTimer, anyWorms, out bool gotoNextAttack);
                if (gotoNextAttack)
                {
                    attackTimer = 0f;
                    attackState = npc.WithinRange(target.Center, 880f) ? 1f : 2f;
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

                    int ichor = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
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
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 15f, 0.15f);

                if (!npc.WithinRange(destination, 200f))
                    npc.Center = Vector2.Lerp(npc.Center, destination, 0.02f);
            }
            else
                npc.velocity *= 0.95f;

            int shootRate = anyWorms ? 100 : 60;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
            {
                int totalProjectiles = anyWorms ? 7 : 17;
                float blobSpeed = anyWorms ? 6f : 8f;
                Vector2 blobSpawnPosition = new Vector2(npc.Center.X, npc.Center.Y + 30f);
                Vector2 currentBlobVelocity = Vector2.UnitY * -blobSpeed + Vector2.UnitX * npc.velocity.SafeNormalize(Vector2.Zero).X * 0.2f;

                npc.TargetClosest();

                for (int i = 0; i < totalProjectiles + 1; i++)
                {
                    Utilities.NewProjectileBetter(blobSpawnPosition, currentBlobVelocity, ModContent.ProjectileType<IchorShot>(), 80, 0f, Main.myPlayer, 0f, 0f);
                    currentBlobVelocity.X += blobSpeed / totalProjectiles * npc.direction * 0.88f;
                }
                Main.PlaySound(SoundID.NPCHit20, npc.position);
            }

            gotoNextAttack = attackTimer >= shootRate * 8f;
        }

        #endregion Specific Attacks

        #endregion Main Boss

        #region Worms

        [OverrideAppliesTo("PerforatorHeadSmall", typeof(PerforatorHiveAIClass), "PerforatorWormHeadSmallAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorWormHeadSmallAI(NPC npc)
		{
            ref float fallCountdown = ref npc.ai[0];
            ref float hasSummonedSegments = ref npc.localAI[0];

            npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
			{
                SpawnSegments(npc, 10, ModContent.NPCType<PerforatorBodySmall>(), ModContent.NPCType<PerforatorTailSmall>());
                hasSummonedSegments = 1f;
			}

            if (!NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>()))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[npc.target];

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            int bodyType = ModContent.NPCType<PerforatorBodySmall>();
            float moveSpeed = MathHelper.Lerp(0.13f, 0.3f, 1f - npc.life / (float)npc.lifeMax);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                if (Main.npc[i].type == bodyType && Main.npc[i].active && inAir)
                    totalSegmentsInAir++;
            }

            if (fallCountdown > 0f)
            {
                if (npc.Center.Y < target.Center.Y + 670f)
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + moveSpeed * 1.5f, -17f, 17f);
                else
                    npc.velocity.Y *= 0.93f;
                fallCountdown--;
            }
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - moveSpeed, -17f, 17f);
                npc.velocity.X = (npc.velocity.X * 5f + npc.SafeDirectionTo(target.Center).X * 8.5f) / 6f;

                if (totalSegmentsInAir >= 7)
                {
                    fallCountdown = 90f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 ichorVelocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
		}

        [OverrideAppliesTo("PerforatorHeadMedium", typeof(PerforatorHiveAIClass), "PerforatorWormHeadMediumAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorWormHeadMediumAI(NPC npc)
        {
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            shootTimer++;

            int shootRate = (int)MathHelper.Lerp(100f, 45f, 1f - npc.life / (float)npc.lifeMax);

            if (Main.netMode != NetmodeID.MultiplayerClient && shootTimer >= shootRate)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 ichorVelocity = (npc.velocity.ToRotation() + MathHelper.Lerp(-0.43f, 0.43f, i / 3f)).ToRotationVector2() * 12f;
                    Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                }
                shootTimer = 0f;
                npc.netUpdate = true;
            }
            return true;
        }

        [OverrideAppliesTo("PerforatorHeadLarge", typeof(PerforatorHiveAIClass), "PerforatorWormHeadLargeAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorWormHeadLargeAI(NPC npc)
        {
            ref float fallCountdown = ref npc.ai[0];
            ref float hasSummonedSegments = ref npc.localAI[0];

            npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
            {
                SpawnSegments(npc, 22, ModContent.NPCType<PerforatorBodyLarge>(), ModContent.NPCType<PerforatorTailLarge>());
                hasSummonedSegments = 1f;
            }

            if (!NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>()))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[npc.target];

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            int bodyType = ModContent.NPCType<PerforatorBodyLarge>();
            float moveSpeed = MathHelper.Lerp(0.09f, 0.36f, 1f - npc.life / (float)npc.lifeMax);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                if (Main.npc[i].type == bodyType && Main.npc[i].active && inAir)
                    totalSegmentsInAir++;
            }

            if (fallCountdown > 0f)
            {
                if (npc.Center.Y < target.Center.Y + 670f)
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + moveSpeed * 1.775f, -17f, 17f);
                else
                    npc.velocity.Y *= 0.93f;
                fallCountdown--;
            }
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - moveSpeed, -17f, 17f);
                npc.velocity.X = (npc.velocity.X * 5f + npc.SafeDirectionTo(target.Center).X * 8.5f) / 6f;

                if (totalSegmentsInAir >= 13)
                {
                    fallCountdown = 90f;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 ichorVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 ichorVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Lerp(-0.46f, 0.46f, i / 3f)) * 6f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<SittingBlood>(), 75, 0f);
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }

        public static void SpawnSegments(NPC npc, int segmentCount, int bodyType, int tailType)
		{
            int aheadSegment = npc.whoAmI;
            for (int i = 0; i < segmentCount; i++)
            {
                int meme;
                if (i < segmentCount - 1)
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

        #region Drawcode

        [OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool PerforatorPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Color glowColor = Color.Lerp(Color.Transparent, Color.Yellow, Utils.InverseLerp(200f, 160f, npc.ai[2], true) * Utils.InverseLerp(0f, 40f, npc.ai[2], true)) * 0.4f;
            glowColor.A = 0;

            float glowOutwardness = 4f;
            switch ((int)npc.localAI[0])
            {
                case 2:
                    glowOutwardness += 2f;
                    break;
                case 3:
                    glowOutwardness += 5f;
                    break;
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawPosition = npc.Center - Main.screenPosition + (MathHelper.TwoPi * i / 6f).ToRotationVector2() * glowOutwardness + Vector2.UnitY * glowOutwardness * 0.5f;
                spriteBatch.Draw(texture, drawPosition, npc.frame, glowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }
            return true;
        }
        #endregion
    }
}
