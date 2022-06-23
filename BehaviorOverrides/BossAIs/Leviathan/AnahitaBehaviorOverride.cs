using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
    public class AnahitaBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Anahita>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public enum AnahitaAttackType
        {
            FloatTowardsPlayer,
            BubbleBurst,
            Singing,
            MistBubble,
            AtlantisCharge
        }

        public static readonly AnahitaAttackType[] Phase1AttackPattern = new AnahitaAttackType[]
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

        public static readonly AnahitaAttackType[] Phase2AttackPattern = new AnahitaAttackType[]
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

        public const float LeviathanSummonLifeRatio = 0.5f;
        public const float AnahitaReturnLifeRatio = 0.5f;

        public override bool PreAI(NPC npc)
        {
            // Stay within the world you stupid fucking fish I swear to god
            npc.position.X = MathHelper.Clamp(npc.position.X, 360f, Main.maxTilesX * 16f - 360f);

            // Define afterimage variables.
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 5;

            // Select a target and reset damage and invulnerability.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            // Set the whoAmI variable.
            CalamityGlobalNPC.siren = npc.whoAmI;

            Vector2 headPosition = npc.Center + new Vector2(npc.direction * 16f, -42f);

            ref float summonedLeviathanFlag = ref npc.Infernum().ExtraAI[6];
            ref float leviathanMusicFade = ref npc.Infernum().ExtraAI[7];

            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.rotation = npc.velocity.X * 0.014f;

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
                    return false;
                }
            }

            ref float attackTimer = ref npc.ai[2];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldSummonLeviathan = lifeRatio < LeviathanSummonLifeRatio;
            bool leviathanAlive = Main.npc.IndexInRange(CalamityGlobalNPC.leviathan) && Main.npc[CalamityGlobalNPC.leviathan].active;
            bool enraged = !leviathanAlive && shouldSummonLeviathan;
            bool outOfOcean = target.position.X > 9400f && target.position.X < (Main.maxTilesX * 16 - 9400) && !BossRushEvent.BossRushActive;
            float? leviathanLifeRatio = !leviathanAlive ? null : new float?(Main.npc[CalamityGlobalNPC.leviathan].life / (float)Main.npc[CalamityGlobalNPC.leviathan].lifeMax);
            bool shouldWaitForLeviathan = (leviathanLifeRatio.HasValue && leviathanLifeRatio.Value >= AnahitaReturnLifeRatio) || Utilities.AnyProjectiles(ModContent.ProjectileType<LeviathanSpawner>());

            // Play idle water sounds.
            if (Main.rand.NextBool(180))
                SoundEngine.PlaySound(SoundID.Zombie35, npc.Center);

            if (leviathanAlive)
                npc.ModNPC.Music = Main.npc[CalamityGlobalNPC.leviathan].ModNPC.Music;

            if (shouldWaitForLeviathan)
            {
                npc.velocity = Vector2.Zero;
                npc.dontTakeDamage = true;
                npc.ai[0] = 0f;
                leviathanMusicFade++;
                if (!NPC.AnyNPCs(ModContent.NPCType<LeviathanNPC>()))
                {
                    target.Infernum().MusicMuffleFactor = Utils.GetLerpValue(10f, 330f, leviathanMusicFade, true);
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
                target.Infernum().MusicMuffleFactor = Utils.GetLerpValue(10f, 330f, leviathanMusicFade, true);
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

                if (Collision.WetCollision(npc.position, npc.width, npc.height) || npc.position.Y > Main.worldSurface * 16D || BossRushEvent.BossRushActive)
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
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;

            if (outOfOcean && (AnahitaAttackType)(int)npc.ai[1] != AnahitaAttackType.AtlantisCharge)
            {
                SelectNextAttack(npc);
                return false;
            }

            switch ((AnahitaAttackType)(int)npc.ai[1])
            {
                case AnahitaAttackType.FloatTowardsPlayer:
                    DoBehavior_FloatTowardsTarget(npc, target, leviathanAlive, enraged, ref attackTimer);
                    break;
                case AnahitaAttackType.BubbleBurst:
                    DoBehavior_BubbleBurst(npc, target, headPosition, leviathanAlive, enraged, ref attackTimer);
                    break;
                case AnahitaAttackType.Singing:
                    DoBehavior_Singing(npc, target, headPosition, leviathanAlive, enraged, ref attackTimer);
                    break;
                case AnahitaAttackType.MistBubble:
                    DoBehavior_MistBubble(npc, target, headPosition, enraged, ref attackTimer);
                    break;
                case AnahitaAttackType.AtlantisCharge:
                    DoBehavior_AtlantisCharge(npc, target, lifeRatio, outOfOcean, enraged, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_FloatTowardsTarget(NPC npc, Player target, bool leviathanAlive, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.velocity.X * 0.02f;
            npc.spriteDirection = npc.direction;

            DoDefaultMovement(npc, target.Center - Vector2.UnitY * 400f, Vector2.One * 7f, 0.14f);

            if (attackTimer >= (leviathanAlive ? (enraged ? 0f : 15f) : 5f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BubbleBurst(NPC npc, Player target, Vector2 headPosition, bool leviathanAlive, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.velocity.X * 0.02f;
            npc.spriteDirection = npc.direction;

            int totalBubbles = enraged ? 15 : 8;
            int bubbleShootRate = leviathanAlive ? 40 : 18;
            float bubbleShootSpeed = 14.5f;
            if (enraged)
            {
                bubbleShootRate = 13;
                bubbleShootSpeed += 4f;
            }
            if (BossRushEvent.BossRushActive)
            {
                bubbleShootRate = 11;
                bubbleShootSpeed *= 2.1f;
            }

            Vector2 destination = target.Center - Vector2.UnitY * 400f;
            destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 260f;
            DoDefaultMovement(npc, destination, Vector2.One * 6f, 0.14f);

            if (attackTimer % bubbleShootRate == bubbleShootRate - 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * bubbleShootSpeed, ModContent.ProjectileType<AnahitaBubble>(), 145, 0f);
                SoundEngine.PlaySound(SoundID.Zombie35, npc.Center);
            }

            if (attackTimer >= bubbleShootRate * totalBubbles + bubbleShootRate - 2f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_Singing(NPC npc, Player target, Vector2 headPosition, bool leviathanAlive, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.velocity.X * 0.02f;
            npc.spriteDirection = npc.direction;

            int singDelay = 90;
            int singClefFireRate = leviathanAlive ? 8 : 11;
            int singClefCount = leviathanAlive ? 25 : 15;
            float clefShootSpeed = leviathanAlive ? 15.5f : 19f;
            if (enraged)
            {
                singClefFireRate = 5;
                clefShootSpeed = 23f;
            }
            if (BossRushEvent.BossRushActive)
            {
                singClefFireRate = 4;
                clefShootSpeed *= 1.5f;
            }

            Vector2 destination = target.Center;
            if (attackTimer < singDelay)
            {
                destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 350f;
                destination.Y -= 325f;
            }

            else if (attackTimer % singClefFireRate == singClefFireRate - 1)
            {
                destination += (MathHelper.TwoPi * Utils.GetLerpValue(singDelay, singDelay + singClefFireRate * singClefCount, attackTimer, true)).ToRotationVector2() * 360f;
                Main.musicPitch = Main.rand.NextFloat(-0.25f, 0.25f);
                SoundEngine.PlaySound(SoundID.Item26, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * clefShootSpeed, ModContent.ProjectileType<SirenSong>(), 145, 0f);
            }

            DoDefaultMovement(npc, destination, Vector2.One * 12f, 0.16f);
            if (attackTimer >= singDelay + singClefFireRate * (singClefCount + 1) - 1)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MistBubble(NPC npc, Player target, Vector2 headPosition, bool enraged, ref float attackTimer)
        {
            npc.rotation = npc.velocity.X * 0.02f;
            npc.spriteDirection = npc.direction;

            float bubbleShootSpeed = 14f;
            Vector2 destination = target.Center - Vector2.UnitY * 300f;
            destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 300f;
            DoDefaultMovement(npc, destination, Vector2.One * 6f, 0.14f);

            if (attackTimer == 75f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(headPosition, (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * bubbleShootSpeed, ModContent.ProjectileType<AnahitaExpandingBubble>(), 150, 0f);

                SoundEngine.PlaySound(SoundID.Zombie35, npc.Center);
            }

            if (attackTimer == (enraged || BossRushEvent.BossRushActive ? 135f : 210f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AtlantisCharge(NPC npc, Player target, float lifeRatio, bool outOfOcean, bool enraged, ref float attackTimer)
        {
            // Force Anahita to use charging frames.
            npc.ai[0] = 3f;

            ref float atlantisCooldown = ref npc.Infernum().ExtraAI[0];

            int hoverTime = 35;
            int chargeTime = 36;
            float chargeSpeed = MathHelper.Lerp(25f, 29f, 1f - lifeRatio);
            float totalCharges = 3f;
            if (enraged)
            {
                chargeTime = 30;
                chargeSpeed += 6f;
            }
            if (outOfOcean)
            {
                chargeSpeed = 43.5f;
            }
            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed = 48f;
            }

            // Spin faster if the Leviathan isn't around.
            if (!NPC.AnyNPCs(ModContent.NPCType<LeviathanNPC>()))
            {
                totalCharges = 4f;
                chargeSpeed *= 1.22f;
                chargeTime -= 5;
            }

            if (attackTimer == 5f)
                SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot, target.Center);

            int wrappedAttackTimer = (int)(attackTimer % (hoverTime + chargeTime));
            if (wrappedAttackTimer < hoverTime)
            {
                float idealRotation = npc.AngleTo(target.Center);
                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;

                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.12f);

                Vector2 destination = target.Center;
                destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 400f;
                destination.Y -= 300f;
                destination -= npc.velocity;

                npc.Center = Vector2.Lerp(npc.Center, new Vector2(destination.X, npc.Center.Y), 0.01f);
                npc.Center = Vector2.Lerp(npc.Center, new Vector2(npc.Center.X, destination.Y), 0.03f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 22f, 1.85f);

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
            bool shouldCharge = wrappedAttackTimer == hoverTime;
            if (shouldCharge)
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

            // Use Atlantis after charging.
            if (wrappedAttackTimer > hoverTime)
            {
                // Do a bit more damage than usual when charging.
                npc.damage = (int)(npc.defDamage * 1.667);

                // Release idle dust.
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

                Vector2 currentDirection = (npc.position - npc.oldPos[1]).SafeNormalize(Vector2.Zero);
                Vector2 spearDirection = currentDirection.RotatedBy(npc.direction * MathHelper.Pi * -0.08f);

                npc.velocity *= 1.003f;
                npc.rotation = currentDirection.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;

                // Poke the target with Atlantis if close to them and pointing towards them.
                bool aimingAtPlayer = currentDirection.AngleBetween(npc.SafeDirectionTo(target.Center)) < MathHelper.ToRadians(54f);
                bool closeToPlayer = npc.WithinRange(target.Center, 180f);
                if (aimingAtPlayer && closeToPlayer && atlantisCooldown <= 0f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (float offset = 0f; offset < 110f; offset += 10f)
                            Utilities.NewProjectileBetter(npc.Center + spearDirection * (15f + offset), spearDirection * (70f + offset * 0.4f), ModContent.ProjectileType<AtlantisSpear>(), 175, 0f);
                    }
                    atlantisCooldown = 30f;
                }

                if (atlantisCooldown > 0)
                    atlantisCooldown--;
            }

            if (attackTimer >= (hoverTime + chargeTime) * totalCharges)
            {
                npc.ai[0] = 0f;
                npc.rotation = 0f;
                SelectNextAttack(npc);
            }
        }

        public static void DoDefaultMovement(NPC npc, Vector2 destination, Vector2 maxVelocity, float acceleration)
        {
            if (BossRushEvent.BossRushActive)
            {
                maxVelocity *= 2.4f;
                acceleration *= 2.7f;
            }

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

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[3]++;
            bool phase2 = npc.life < npc.lifeMax * 0.7f;

            AnahitaAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
            AnahitaAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Go to the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Reset the attack timer.
            npc.ai[2] = 0f;

            // And reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }
    }
}