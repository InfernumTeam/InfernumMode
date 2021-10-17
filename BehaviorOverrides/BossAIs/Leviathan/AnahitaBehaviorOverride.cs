using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
	public class AnahitaBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Siren>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public enum AnahitaAttackType
        {
            FloatTowardsPlayer,
            BubbleBurst,
            Singing,
            MistBubble,
            AtlantisCharge
        }

        internal static readonly AnahitaAttackType[] Phase1AttackPattern = new AnahitaAttackType[]
        {
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.BubbleBurst,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.Singing,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.MistBubble,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.AtlantisCharge,
        };

        internal static readonly AnahitaAttackType[] Phase2AttackPattern = new AnahitaAttackType[]
        {
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.BubbleBurst,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.Singing,
            AnahitaAttackType.AtlantisCharge,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.AtlantisCharge,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.MistBubble,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.BubbleBurst,
            AnahitaAttackType.AtlantisCharge,
            AnahitaAttackType.FloatTowardsPlayer,
            AnahitaAttackType.AtlantisCharge,
        };

        public override bool PreAI(NPC npc)
        {
            npc.position.X = MathHelper.Clamp(npc.position.X, 360f, Main.maxTilesX * 16f - 360f);
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 5;

            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            CalamityGlobalNPC.siren = npc.whoAmI;

            Vector2 headPosition = npc.Center + new Vector2(npc.direction * 16f, -42f);

            ref float summonedLeviathanFlag = ref npc.Infernum().ExtraAI[6];
            ref float leviathanMusicFade = ref npc.Infernum().ExtraAI[7];

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 5600f))
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead || !npc.WithinRange(target.Center, 5600f))
                {
                    npc.rotation = npc.velocity.X * 0.014f;

                    // Descend back into the ocean.
                    float moveDirection = 1f;
                    if (Math.Abs(npc.Center.X - Main.maxTilesX * 16f) > Math.Abs(npc.Center.X))
                        moveDirection = -1f;
                    npc.velocity.X = moveDirection * 6f;
                    npc.spriteDirection = (int)-moveDirection;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.2f, -3f, 16f);

                    if (npc.position.Y > (Main.worldSurface - 90f) * 16.0)
                    {
                        for (int x = 0; x < Main.maxNPCs; x++)
                        {
                            if (Main.npc[x].type == ModContent.NPCType<LeviathanNPC>())
                            {
                                Main.npc[x].active = false;
                                Main.npc[x].netUpdate = true;
                            }
                        }
                        npc.active = false;
                        npc.netUpdate = true;
                    }

                    return false;
                }
            }

            ref float attackTimer = ref npc.ai[2];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.7f;
            bool shouldSummonLeviathan = lifeRatio < 0.5f;
            bool leviathanAlive = Main.npc.IndexInRange(CalamityGlobalNPC.leviathan) && Main.npc[CalamityGlobalNPC.leviathan].active;
            bool enraged = !leviathanAlive && shouldSummonLeviathan;
            bool outOfOcean = target.position.X > 9400f && target.position.X < (Main.maxTilesX * 16 - 9400);
            float? leviathanLifeRatio = !leviathanAlive ? null : new float?(Main.npc[CalamityGlobalNPC.leviathan].life / (float)Main.npc[CalamityGlobalNPC.leviathan].lifeMax);
            bool shouldWaitForLeviathan = (leviathanLifeRatio.HasValue && leviathanLifeRatio.Value >= 0.6f) || Utilities.AnyProjectiles(ModContent.ProjectileType<LeviathanSpawner>());

            // Play idle water sounds.
            if (Main.rand.NextBool(180))
                Main.PlaySound(SoundID.Zombie, (int)npc.position.X, (int)npc.position.Y, 35);

            if (leviathanAlive)
                npc.modNPC.music = Main.npc[CalamityGlobalNPC.leviathan].modNPC.music;

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                AnahitaAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
                AnahitaAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            void doSkeletronHeadMovementTo(Vector2 destination, Vector2 maxVelocity, float acceleration)
            {
                if (npc.Center.Y > destination.Y + 50f)
                {
                    if (npc.velocity.Y > 0f)
                        npc.velocity.Y *= 0.98f;
                    npc.velocity.Y -= acceleration;
                    if (npc.velocity.Y > maxVelocity.Y)
                        npc.velocity.Y = maxVelocity.Y;
                }
                else if (npc.Center.Y < destination.Y - 50f)
                {
                    if (npc.velocity.Y < 0f)
                        npc.velocity.Y *= 0.98f;
                    npc.velocity.Y += acceleration;
                    if (npc.velocity.Y < -maxVelocity.Y)
                        npc.velocity.Y = -maxVelocity.Y;
                }

                if (npc.Center.X > destination.X + 100f)
                {
                    if (npc.velocity.X > 0f)
                        npc.velocity.X *= 0.98f;
                    npc.velocity.X -= acceleration;
                    if (npc.velocity.X > maxVelocity.X)
                        npc.velocity.X = maxVelocity.X;
                }

                if (npc.Center.X < destination.X - 100f)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X *= 0.98f;
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X < -maxVelocity.X)
                        npc.velocity.X = -maxVelocity.X;
                }
            }

            if (shouldWaitForLeviathan)
            {
                npc.velocity = Vector2.Zero;
                npc.dontTakeDamage = true;
                npc.ai[0] = 0f;
                leviathanMusicFade++;
                if (!NPC.AnyNPCs(ModContent.NPCType<LeviathanNPC>()))
                {
                    target.Infernum().MusicMuffleFactor = Utils.InverseLerp(10f, 330f, leviathanMusicFade, true);
                    leviathanMusicFade++;
                }
                return false;
            }
            if (summonedLeviathanFlag == 0f && shouldSummonLeviathan)
            {
                // Force Anahita to use charging frames.
                npc.ai[0] = 3f;

                npc.rotation = npc.velocity.X * 0.014f;

                // Descend back into the ocean.
                npc.direction = (npc.Center.X < Main.maxTilesX * 8f).ToDirectionInt();
                target.Infernum().MusicMuffleFactor = Utils.InverseLerp(10f, 330f, leviathanMusicFade, true);
                leviathanMusicFade++;

                if (npc.alpha <= 0)
                {
                    float moveDirection = 1f;
                    if (Math.Abs(npc.Center.X - Main.maxTilesX * 16f) > Math.Abs(npc.Center.X))
                        moveDirection = -1f;
                    npc.velocity.X = moveDirection * 6f;
                    npc.spriteDirection = (int)-moveDirection;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.2f, -3f, 16f);
                }

                float idealRotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;

                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.08f);

                if (Collision.WetCollision(npc.position, npc.width, npc.height) || npc.position.Y > Main.worldSurface * 16.0)
                {
                    int oldAlpha = npc.alpha;
                    npc.alpha = Utils.Clamp(npc.alpha + 9, 0, 255);
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.alpha >= 255 && oldAlpha < 255)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LeviathanSpawner>(), 0, 0f);
                        summonedLeviathanFlag = 1f;
                    }

                    npc.velocity *= 0.9f;
                }
                npc.dontTakeDamage = true;
                return false;
            }

            // If the above checks of no activity are passed, fade back in again.
            npc.alpha = Utils.Clamp(npc.alpha - 14, 0, 255);

            npc.dontTakeDamage = outOfOcean;

            if (outOfOcean && (AnahitaAttackType)(int)npc.ai[1] != AnahitaAttackType.AtlantisCharge)
            {
                goToNextAIState();
                return false;
            }

            switch ((AnahitaAttackType)(int)npc.ai[1])
            {
                case AnahitaAttackType.FloatTowardsPlayer:
                    npc.TargetClosest();
                    npc.rotation = npc.velocity.X * 0.02f;
                    npc.spriteDirection = npc.direction;

                    doSkeletronHeadMovementTo(target.Center - Vector2.UnitY * 400f, Vector2.One * 7f, 0.14f);

                    if (attackTimer >= (leviathanAlive ? (enraged ? 0f : 15f) : 5f))
                        goToNextAIState();
                    break;
                case AnahitaAttackType.BubbleBurst:
                    npc.TargetClosest();
                    npc.rotation = npc.velocity.X * 0.02f;
                    npc.spriteDirection = npc.direction;
                    target = Main.player[npc.target];

                    int totalBubbles = enraged ? 11 : 8;
                    int bubbleShootRate = leviathanAlive ? 45 : 22;
                    float bubbleShootSpeed = 10f;
                    if (enraged)
                    {
                        bubbleShootRate -= 6;
                        bubbleShootSpeed += 3f;
                    }

                    Vector2 destination = target.Center - Vector2.UnitY * 400f;
                    destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 260f;
                    doSkeletronHeadMovementTo(destination, Vector2.One * 6f, 0.14f);

                    if (attackTimer % bubbleShootRate == bubbleShootRate - 1)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * bubbleShootSpeed, ModContent.ProjectileType<AnahitaBubble>(), 130, 0f);
                        Main.PlaySound(SoundID.Zombie, (int)npc.position.X, (int)npc.position.Y, 35);
                    }

                    if (attackTimer >= bubbleShootRate * totalBubbles + bubbleShootRate - 2f)
                        goToNextAIState();
                    break;
                case AnahitaAttackType.Singing:
                    npc.TargetClosest();
                    npc.rotation = npc.velocity.X * 0.02f;
                    npc.spriteDirection = npc.direction;
                    target = Main.player[npc.target];

                    int singDelay = 90;
                    int singClefFireRate = leviathanAlive ? 8 : 11;
                    int singClefCount = leviathanAlive ? 25 : 15;
                    float clefShootSpeed = leviathanAlive ? 13f : 15.5f;
                    if (enraged)
                    {
                        singClefFireRate -= 2;
                        clefShootSpeed += 2f;
                    }

                    destination = target.Center;
                    if (attackTimer < singDelay)
                    {
                        destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 350f;
                        destination.Y -= 325f;
                    }

                    else if (attackTimer % singClefFireRate == singClefFireRate - 1)
                    {
                        destination += (MathHelper.TwoPi * Utils.InverseLerp(singDelay, singDelay + singClefFireRate * singClefCount, attackTimer, true)).ToRotationVector2() * 360f;
                        Main.harpNote = Main.rand.NextFloat(-0.25f, 0.25f);
                        Main.PlaySound(SoundID.Item26, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * clefShootSpeed, ModContent.ProjectileType<SirenSong>(), 130, 0f);
                    }

                    doSkeletronHeadMovementTo(destination, Vector2.One * 12f, 0.16f);
                    if (attackTimer >= singDelay + singClefFireRate * (singClefCount + 1) - 1)
                        goToNextAIState();
                    break;
                case AnahitaAttackType.MistBubble:
                    npc.TargetClosest();
                    npc.rotation = npc.velocity.X * 0.02f;
                    npc.spriteDirection = npc.direction;
                    target = Main.player[npc.target];

                    bubbleShootSpeed = 10f;
                    destination = target.Center - Vector2.UnitY * 300f;
                    destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 300f;
                    doSkeletronHeadMovementTo(destination, Vector2.One * 6f, 0.14f);

                    if (attackTimer == 75f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * bubbleShootSpeed, ModContent.ProjectileType<AnahitaExpandingBubble>(), 135, 0f);
                        Main.PlaySound(SoundID.Zombie, (int)npc.position.X, (int)npc.position.Y, 35);
                    }

                    if (attackTimer == (enraged ? 175f : 210f))
                        goToNextAIState();
                    break;
                case AnahitaAttackType.AtlantisCharge:
                    // Force Anahita to use charging frames.
                    npc.ai[0] = 3f;

                    ref float atlantisCooldown = ref npc.Infernum().ExtraAI[0];

                    int hoverTime = 50;
                    int spinTime = 250;
                    int chargeTime = 40;
                    float chargeSpeed = MathHelper.Lerp(25f, 31f, 1f - lifeRatio);
                    float totalSpins = 2f;
                    if (enraged)
                    {
                        hoverTime = 40;
                        spinTime = 300;
                        chargeSpeed += 4f;
                    }
                    if (outOfOcean)
                    {
                        hoverTime = 30;
                        chargeTime = 20;
                        chargeSpeed = 37f;
                        totalSpins = 2f;
                    }

                    float spinAngularVelocity = MathHelper.TwoPi * totalSpins / spinTime;

                    bool shouldJustCharge = (attackTimer >= hoverTime + spinTime / 2 && attackTimer <= hoverTime + spinTime / 2 + chargeTime) || attackTimer <= hoverTime + 45;

                    if (attackTimer < hoverTime)
                    {
                        float idealRotation = npc.AngleTo(target.Center);
                        if (npc.spriteDirection == 1)
                            idealRotation += MathHelper.Pi;

                        npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.04f);

                        destination = target.Center;
                        destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 400f;
                        destination.Y -= 500f;
                        destination -= npc.velocity;

                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 15f, 1.85f);
                        int idealDirection = Math.Sign(target.Center.X - npc.Center.X);
                        if (idealDirection != 0)
                        {
                            if (attackTimer == 0f && idealDirection != npc.direction)
                                npc.rotation += MathHelper.Pi;

                            npc.direction = idealDirection;

                            if (npc.spriteDirection != -npc.direction)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }
                    }

                    // Do the actual charge.
                    if (attackTimer == hoverTime || attackTimer == hoverTime + spinTime / 2)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        int idealDirection = Math.Sign(target.Center.X - npc.Center.X);
                        if (idealDirection != 0)
                        {
                            npc.direction = idealDirection;

                            if (npc.spriteDirection == 1)
                                npc.rotation += MathHelper.Pi;

                            npc.spriteDirection = -npc.direction;
                        }
                    }

                    // Spin around and actually use Atlantis.
                    if (attackTimer > hoverTime)
                    {
                        // Do a bit more damage than usual when charging.
                        npc.damage = (int)(npc.defDamage * 1.667);

                        // Idle dust.
                        if (!Main.dedServ)
                        {
                            int dustCount = 7;
                            for (int i = 0; i < dustCount; i++)
                            {
                                Vector2 dustSpawnOffset = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy((i - (dustCount / 2 - 1)) * MathHelper.Pi / dustCount);
                                Vector2 dustVelocity = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(6f, 16f);
                                dustSpawnOffset += dustVelocity * 0.5f;

                                Dust water = Dust.NewDustDirect(npc.Center + dustSpawnOffset, 0, 0, 172, dustVelocity.X, dustVelocity.Y, 100, default, 1.4f);
                                water.velocity /= 4f;
                                water.velocity -= npc.velocity;
                                water.noGravity = true;
                                water.noLight = true;
                            }
                        }

                        if (!shouldJustCharge)
                        {
                            npc.velocity = npc.velocity.RotatedBy(spinAngularVelocity * npc.direction);
                            npc.rotation += spinAngularVelocity * npc.direction;
                            if (!npc.WithinRange(target.Center, 120f))
                                npc.Center += npc.SafeDirectionTo(target.Center) * chargeSpeed * 0.3f;
                        }

                        Vector2 currentDirection = (npc.position - npc.oldPos[1]).SafeNormalize(Vector2.Zero);
                        Vector2 spearDirection = currentDirection.RotatedBy(npc.direction * MathHelper.Pi * -0.08f);

                        npc.rotation = currentDirection.ToRotation();

                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        bool aimingAtPlayer = Vector2.Dot(currentDirection, npc.SafeDirectionTo(target.Center)) > 0.6f;
                        bool closeToPlayer = npc.WithinRange(target.Center, 180f);
                        if (aimingAtPlayer && closeToPlayer && Main.netMode != NetmodeID.MultiplayerClient && atlantisCooldown <= 0f)
                        {
                            for (float offset = 0f; offset < 110f; offset += 10f)
                                Utilities.NewProjectileBetter(npc.Center + spearDirection * (15f + offset), spearDirection * (70f + offset * 0.4f), ModContent.ProjectileType<AtlantisSpear>(), 120, 0f);
                            atlantisCooldown = 30f;
                        }

                        if (atlantisCooldown > 0)
                            atlantisCooldown--;
                    }

                    if (attackTimer >= hoverTime + spinTime)
                    {
                        npc.ai[0] = 0f;
                        npc.rotation = 0f;
                        goToNextAIState();
                    }
                    break;
            }

            attackTimer++;
            return false;
        }
    }
}