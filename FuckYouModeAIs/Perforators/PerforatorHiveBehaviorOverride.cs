using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
    public class PerforatorHiveBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHive>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

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

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            npc.damage = 0;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float summonAnimationCountdown = ref npc.ai[2];
            ref float outOfBiomeTimer = ref npc.ai[3];
            ref float animationState = ref npc.localAI[0];
            ref float wormSpawnState = ref npc.localAI[1];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                npc.TargetClosest(false);
                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
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

            if (!target.ZoneCorrupt && !target.ZoneCrimson)
                outOfBiomeTimer++;
            else
                outOfBiomeTimer = 0f;

            npc.dontTakeDamage = anyWorms || outOfBiomeTimer > 240f || summonAnimationCountdown > 0f;

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
                npc.timeLeft = 1800;
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
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 19f, anyWorms ? 0.054f : 0.07f);

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
                float distanceFromDestination = npc.Distance(destination);
                float movementInterpolant = MathHelper.Lerp(0.055f, 0.16f, Utils.InverseLerp(100f, 30f, distanceFromDestination, true));
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * MathHelper.Min(distanceFromDestination, 15f), movementInterpolant);
            }

            gotoNextAttack = attackTimer >= 240f;
        }

        public static void DoAttack_ReleaseRegularBursts(NPC npc, Player target, ref float attackTimer, bool anyWorms, out bool gotoNextAttack)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 270f;
            float distanceFromDestination = npc.Distance(destination);
            float movementInterpolant = MathHelper.Lerp(0.055f, 0.16f, Utils.InverseLerp(100f, 30f, distanceFromDestination, true));
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * MathHelper.Min(distanceFromDestination, 15f), movementInterpolant);

            int shootRate = anyWorms ? 100 : 62;
            Vector2 blobSpawnPosition = new Vector2(npc.Center.X + Main.rand.NextFloat(-12f, 12f), npc.Center.Y + 30f);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
            {
                int totalProjectiles = anyWorms ? 7 : (int)MathHelper.Lerp(17f, 22f, 1f - npc.life / (float)npc.lifeMax);
                float blobSpeed = anyWorms ? 6f : 8f;
                Vector2 currentBlobVelocity = new Vector2(4f + Main.rand.NextFloat(-0.1f, 0.1f) + target.velocity.X * 0.12f, -blobSpeed);

                npc.TargetClosest();

                for (int i = 0; i < totalProjectiles + 1; i++)
                {
                    Utilities.NewProjectileBetter(blobSpawnPosition, currentBlobVelocity, ModContent.ProjectileType<IchorShot>(), 80, 0f, Main.myPlayer, 0f, 0f);
                    currentBlobVelocity.X += blobSpeed / totalProjectiles * -1.12f;
                }
                Main.PlaySound(SoundID.NPCHit20, npc.position);
            }

            gotoNextAttack = attackTimer >= shootRate * 8f + shootRate * 0.5f;
        }

        #endregion Specific Attacks

        #endregion AI

        #region Drawcode

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
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
                Vector2 drawPosition = npc.Center - Main.screenPosition + (MathHelper.TwoPi * i / 6f).ToRotationVector2() * glowOutwardness + Vector2.UnitY * (glowOutwardness * 0.5f - 22f);
                spriteBatch.Draw(texture, drawPosition, npc.frame, glowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }
            return true;
        }
        #endregion
    }
}
