using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorHiveBehaviorOverride : NPCBehaviorOverride
    {
        public enum PerforatorHiveAttackState
        {
            HoverNearTarget,
            SwoopTowardsPlayer,
            ReleaseRegularBursts,
            IchorBlastsFromBelow
        }

        public const float Phase2LifeRatio = 0.75f;
        public const float Phase3LifeRatio = 0.4f;
        public const float Phase4LifeRatio = 0.15f;

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

            // Set damage.
            npc.defDamage = 74;
            npc.damage = npc.defDamage;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float summonAnimationCountdown = ref npc.ai[2];
            ref float enrageTimer = ref npc.ai[3];
            ref float animationState = ref npc.localAI[0];
            ref float wormSpawnState = ref npc.localAI[1];
            ref float wormContactDamageDelay = ref npc.localAI[2];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 6400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            bool anyWorms = NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadSmall>()) || NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadMedium>()) || NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadLarge>());

            // Disable contact damage for 2 seconds after a worm has been killed.
            wormContactDamageDelay = MathHelper.Clamp(wormContactDamageDelay + anyWorms.ToDirectionInt(), 0f, 120f);
            if (wormContactDamageDelay > 0f)
                npc.damage = 0;

            int spawnAnimationTime = BossRushEvent.BossRushActive ? 75 : 200;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lifeRatio < Phase2LifeRatio && animationState == 0f)
                {
                    animationState = 1f;
                    summonAnimationCountdown = spawnAnimationTime;
                    npc.netUpdate = true;
                }

                if (lifeRatio < Phase3LifeRatio && animationState == 1f)
                {
                    animationState = 2f;
                    summonAnimationCountdown = spawnAnimationTime;
                    npc.netUpdate = true;
                }

                if (lifeRatio < Phase4LifeRatio && animationState == 2f)
                {
                    animationState = 3f;
                    summonAnimationCountdown = spawnAnimationTime;
                    npc.netUpdate = true;
                }
            }

            bool outOfBiome = !target.ZoneCorrupt && !target.ZoneCrimson;
            if (outOfBiome && !BossRushEvent.BossRushActive)
                enrageTimer++;
            else
                enrageTimer = 0f;
            bool enraged = enrageTimer > 300f;

            npc.dontTakeDamage = anyWorms || enraged || summonAnimationCountdown > 0f;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

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

                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, wormTypeToSpawn, 1);
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
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.03f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center + new Vector2(200f, 450f);
                return false;
            }

            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.015f, 0f, 1f);

            if (npc.Opacity >= 1f)
            {
                switch ((PerforatorHiveAttackState)(int)attackState)
                {
                    case PerforatorHiveAttackState.HoverNearTarget:
                        DoAttack_HoverNearTarget(npc, target, lifeRatio < Phase4LifeRatio, ref attackTimer, enraged, anyWorms);
                        break;
                    case PerforatorHiveAttackState.SwoopTowardsPlayer:
                        DoAttack_SwoopTowardsPlayer(npc, target, ref attackTimer, enraged, anyWorms);
                        break;
                    case PerforatorHiveAttackState.ReleaseRegularBursts:
                        DoAttack_ReleaseRegularBursts(npc, target, lifeRatio < Phase4LifeRatio, ref attackTimer, enraged, anyWorms);
                        break;
                    case PerforatorHiveAttackState.IchorBlastsFromBelow:
                        DoAttack_IchorBlastsFromBelow(npc, target, ref attackTimer, enraged);
                        break;
                }
            }
            else
                npc.damage = 0;

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

        public static void SelectNextAttack(NPC npc)
        {
            Player target = Main.player[npc.target];

            PerforatorHiveAttackState nextAttack = (PerforatorHiveAttackState)(int)npc.ai[0];
            switch ((PerforatorHiveAttackState)(int)npc.ai[0])
            {
                case PerforatorHiveAttackState.HoverNearTarget:
                    nextAttack = npc.WithinRange(target.Center, 880f) ? PerforatorHiveAttackState.SwoopTowardsPlayer : PerforatorHiveAttackState.ReleaseRegularBursts;
                    if (npc.life / (float)npc.lifeMax < Phase4LifeRatio)
                        nextAttack = PerforatorHiveAttackState.IchorBlastsFromBelow;
                    break;
                case PerforatorHiveAttackState.SwoopTowardsPlayer:
                    nextAttack = PerforatorHiveAttackState.ReleaseRegularBursts;
                    break;
                case PerforatorHiveAttackState.ReleaseRegularBursts:
                    nextAttack = PerforatorHiveAttackState.SwoopTowardsPlayer;
                    break;
                case PerforatorHiveAttackState.IchorBlastsFromBelow:
                    nextAttack = PerforatorHiveAttackState.ReleaseRegularBursts;
                    break;
            }

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.TargetClosest();
            npc.netUpdate = true;
        }

        public static void DoAttack_SwoopTowardsPlayer(NPC npc, Player target, ref float attackTimer, bool enraged, bool anyWorms)
        {
            // Hover above the target before swooping.
            if (attackTimer < 90f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 360f;
                float hoverSpeed = BossRushEvent.BossRushActive ? 28f : 17f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * hoverSpeed, anyWorms ? 0.054f : 0.07f);

                if (npc.WithinRange(destination, 35f))
                {
                    attackTimer = 90f;
                    npc.netUpdate = true;
                }
            }

            // Play a roar sound before swooping.
            if (attackTimer == 90f)
            {
                Main.PlaySound(SoundID.Roar, target.Center, 0);
                npc.velocity = npc.SafeDirectionTo(target.Center) * new Vector2(8f, 24f);
                if (anyWorms)
                    npc.velocity *= 0.8f;
                npc.netUpdate = true;

                npc.TargetClosest();
            }

            // Swoop.
            if (attackTimer >= 90f && attackTimer <= 160f)
            {
                npc.velocity = npc.velocity.RotatedBy(MathHelper.PiOver2 / 70f * -npc.direction);
                if (enraged && Math.Abs(npc.velocity.X) < 15f)
                    npc.velocity.X *= 1.0345f;
                if (BossRushEvent.BossRushActive && Math.Abs(npc.velocity.X) < 24f)
                    npc.velocity.X *= 1.05f;
            }

            if (attackTimer > 160f)
                npc.velocity *= 0.97f;

            if (attackTimer >= 195f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_HoverNearTarget(NPC npc, Player target, bool finalWormDead, ref float attackTimer, bool enraged, bool anyWorms)
        {
            if (attackTimer % 120f > 85f && attackTimer > 120f)
            {
                npc.velocity *= 0.97f;

                // Release ichor everywhere.
                int shootRate = anyWorms ? 10 : 6;
                if (enraged)
                    shootRate = 5;
                if (finalWormDead && !anyWorms)
                    shootRate--;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8.4f);
                    Vector2 spawnPosition = npc.Center - Vector2.UnitY * 45f + Main.rand.NextVector2Circular(30f, 30f);
                    if (BossRushEvent.BossRushActive)
                        shootVelocity *= 2f;

                    int ichor = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                    if (Main.projectile.IndexInRange(ichor))
                        Main.projectile[ichor].ai[1] = 1f;
                }
            }
            else
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                float distanceFromDestination = npc.Distance(destination);
                float movementInterpolant = MathHelper.Lerp(0.055f, 0.1f, Utils.InverseLerp(100f, 30f, distanceFromDestination, true));
                float idealMovementSpeed = BossRushEvent.BossRushActive ? 28f : 15f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * MathHelper.Min(distanceFromDestination, idealMovementSpeed), movementInterpolant);
                npc.velocity -= npc.SafeDirectionTo(target.Center) * Utils.InverseLerp(235f, 115f, npc.Distance(target.Center), true) * 12f;
            }

            if (attackTimer >= 360f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ReleaseRegularBursts(NPC npc, Player target, bool finalWormDead, ref float attackTimer, bool enraged, bool anyWorms)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 270f;

            if (!finalWormDead || !npc.WithinRange(destination, 80f))
            {
                float movementSpeed = MathHelper.Lerp(4.6f, 7f, 1f - npc.life / (float)npc.lifeMax);
                float acceleration = 0.2f;
                if (npc.position.Y > destination.Y)
                {
                    if (npc.velocity.Y > 0f)
                    {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y -= acceleration;
                    if (npc.velocity.Y > movementSpeed * 0.45f)
                        npc.velocity.Y = movementSpeed * 0.45f;
                }
                else if (npc.position.Y < destination.Y - 100f)
                {
                    if (npc.velocity.Y < 0f)
                    {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y += acceleration;
                    if (npc.velocity.Y < -movementSpeed * 0.45f)
                        npc.velocity.Y = -movementSpeed * 0.45f;
                }

                if (npc.Center.X > destination.X + 230f)
                {
                    if (npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X -= acceleration;
                    if (npc.velocity.X > movementSpeed)
                        npc.velocity.X = movementSpeed;
                }
                else if (npc.Center.X < destination.X - 230f)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X < -movementSpeed)
                        npc.velocity.X = -movementSpeed;
                }
            }

            int shootRate = anyWorms ? 100 : 62;
            int totalBursts = finalWormDead ? 12 : 8;
            if (finalWormDead)
                shootRate -= 20;
            if (enraged)
                shootRate = 30;
            Vector2 blobSpawnPosition = new Vector2(npc.Center.X + Main.rand.NextFloat(-12f, 12f), npc.Center.Y + 30f);

            // Release blood teeth balls upward occasionally.
            if (finalWormDead && Main.netMode != NetmodeID.MultiplayerClient && attackTimer % (shootRate * 3f) == shootRate * 3f - 1f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 toothBallVelocity = -Vector2.UnitY.RotatedByRandom(0.55f) * 17f;
                    Utilities.NewProjectileBetter(npc.Center - toothBallVelocity * 7f, toothBallVelocity, ModContent.ProjectileType<SittingBlood>(), 80, 0f);
                }
            }

            // And release ichor shots upward more frequently.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
            {
                int totalProjectiles = (int)MathHelper.Lerp(22f, 30f, 1f - npc.life / (float)npc.lifeMax);
                float blobSpeed = anyWorms ? 6f : 8f;
                if (finalWormDead)
                    blobSpeed += 0.25f;
                Vector2 currentBlobVelocity = new Vector2(4f + Main.rand.NextFloat(-0.1f, 0.1f) + target.velocity.X * 0.12f, blobSpeed * -0.65f);

                npc.TargetClosest();

                for (int i = 0; i < totalProjectiles + 1; i++)
                {
                    Utilities.NewProjectileBetter(blobSpawnPosition, currentBlobVelocity, ModContent.ProjectileType<IchorShot>(), finalWormDead ? 110 : 95, 0f, Main.myPlayer, 0f, 0f);
                    currentBlobVelocity.X += blobSpeed / totalProjectiles * -1.54f + Main.rand.NextFloatDirection() * 0.08f;
                }
                Main.PlaySound(SoundID.NPCHit20, npc.position);
            }

            if (attackTimer >= shootRate * (totalBursts + 0.9f))
                SelectNextAttack(npc);
        }

        public static void DoAttack_IchorBlastsFromBelow(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 270f;

            if (!npc.WithinRange(destination, 80f) && attackTimer < 90f)
            {
                float movementSpeed = 7.25f;
                float acceleration = 0.3f;
                if (npc.position.Y > destination.Y)
                {
                    if (npc.velocity.Y > 0f)
                    {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y -= acceleration;
                    if (npc.velocity.Y > movementSpeed * 0.45f)
                        npc.velocity.Y = movementSpeed * 0.45f;
                }
                else if (npc.position.Y < destination.Y - 100f)
                {
                    if (npc.velocity.Y < 0f)
                    {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y += acceleration;
                    if (npc.velocity.Y < -movementSpeed * 0.45f)
                        npc.velocity.Y = -movementSpeed * 0.45f;
                }

                if (npc.Center.X > destination.X + 230f)
                {
                    if (npc.velocity.X > 0f)
                    {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X -= acceleration;
                    if (npc.velocity.X > movementSpeed)
                        npc.velocity.X = movementSpeed;
                }
                else if (npc.Center.X < destination.X - 230f)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X < -movementSpeed)
                        npc.velocity.X = -movementSpeed;
                }
            }

            // Play a telegraph sound prior to dashing into the ground.
            if (attackTimer == 45f)
                Main.PlaySound(SoundID.NPCDeath18, target.Center);

            if (attackTimer == 90f)
                Main.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);

            if (attackTimer > 90f && attackTimer < 300f)
            {
                if (Math.Abs(npc.velocity.X) > 7f)
                    npc.velocity.X *= 0.985f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 15f, 0.06f);

                if (Collision.SolidCollision(npc.position, npc.width, npc.height) && attackTimer > 105f)
                {
                    Main.PlaySound(SoundID.Item89, npc.Center);

                    float speedOffset = Main.rand.NextFloat(-3f, 3f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (float i = -16; i < 16; i += enraged ? 0.95f : 1.6f)
                        {
                            Vector2 shootVelocity = new Vector2(i + speedOffset, 13f);
                            Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ModContent.ProjectileType<BloodGlob>(), 95, 0f);
                        }
                    }

                    attackTimer = 300f;
                    npc.velocity.Y *= -1f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer > 360f)
                SelectNextAttack(npc);
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
