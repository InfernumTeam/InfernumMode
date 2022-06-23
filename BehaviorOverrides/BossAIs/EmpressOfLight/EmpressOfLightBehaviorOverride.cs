using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using CalamityMod;
using Terraria.Graphics.Effects;
using CalamityMod.Events;
using Terraria.Audio;
using System.Linq;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressOfLightBehaviorOverride : NPCBehaviorOverride
    {
        public enum EmpressOfLightAttackType
        {
            SpawnAnimation,
            PrismaticBoltCircle,
            MesmerizingMagic,
            HorizontalCharge,
            LanceOctagon,
            EnterSecondPhase,
            RainbowWispForm,
            DanceOfSwords,
            LightOverload,
            ShimmeringDiamondLanceBarrage,
            LaserStorm,
            InfiniteBrilliance
        }

        public override int NPCOverrideType => NPCID.HallowBoss;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Constants and Attack Patterns

        public static bool ShouldBeEnraged => Main.dayTime;

        public const int SecondPhaseFadeoutTime = 90;

        public const int SecondPhaseFadeBackInTime = 90;

        public const float Phase2LifeRatio = 0.7f;

        public const float Phase3LifeRatio = 0.4f;

        public const float Phase4LifeRatio = 0.15f;

        public const float BorderWidth = 6000f;

        public static int PrismaticBoltDamage => ShouldBeEnraged ? 350 : 175;

        public static int LanceDamage => ShouldBeEnraged ? 375 : 185;

        public static int SwordDamage => ShouldBeEnraged ? 400 : 200;

        public static int CloudDamage => ShouldBeEnraged ? 400 : 200;

        public static int LaserbeamDamage => ShouldBeEnraged ? 700 : 300;

        public static EmpressOfLightAttackType[] Phase1AttackCycle => new EmpressOfLightAttackType[]
        {
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.LanceOctagon,
            EmpressOfLightAttackType.MesmerizingMagic,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.LanceOctagon,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.MesmerizingMagic,
        };

        public static EmpressOfLightAttackType[] Phase2AttackCycle => new EmpressOfLightAttackType[]
        {
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.MesmerizingMagic,
            EmpressOfLightAttackType.DanceOfSwords,
            EmpressOfLightAttackType.RainbowWispForm,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.DanceOfSwords,
            EmpressOfLightAttackType.MesmerizingMagic,
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.RainbowWispForm,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.DanceOfSwords,
        };

        public static EmpressOfLightAttackType[] Phase3AttackCycle => new EmpressOfLightAttackType[]
        {
            EmpressOfLightAttackType.ShimmeringDiamondLanceBarrage,
            EmpressOfLightAttackType.LightOverload,
            EmpressOfLightAttackType.LaserStorm,
            EmpressOfLightAttackType.LanceOctagon,
            EmpressOfLightAttackType.RainbowWispForm,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.ShimmeringDiamondLanceBarrage,
            EmpressOfLightAttackType.LightOverload,
            EmpressOfLightAttackType.RainbowWispForm,
            EmpressOfLightAttackType.LanceOctagon,
            EmpressOfLightAttackType.ShimmeringDiamondLanceBarrage,
            EmpressOfLightAttackType.LaserStorm,
            EmpressOfLightAttackType.PrismaticBoltCircle,
            EmpressOfLightAttackType.HorizontalCharge,
        };

        public static EmpressOfLightAttackType[] Phase4AttackCycle => new EmpressOfLightAttackType[]
        {
            EmpressOfLightAttackType.InfiniteBrilliance,
            EmpressOfLightAttackType.LaserStorm,
            EmpressOfLightAttackType.LightOverload,
            EmpressOfLightAttackType.InfiniteBrilliance,
            EmpressOfLightAttackType.DanceOfSwords,
            EmpressOfLightAttackType.LaserStorm,
            EmpressOfLightAttackType.InfiniteBrilliance,
            EmpressOfLightAttackType.LightOverload,
            EmpressOfLightAttackType.HorizontalCharge,
            EmpressOfLightAttackType.InfiniteBrilliance,
            EmpressOfLightAttackType.DanceOfSwords,
        };
        #endregion Constants and Attack Patterns

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];
            ref float wingFrameCounter = ref npc.localAI[0];
            ref float leftArmFrame = ref npc.localAI[1];
            ref float rightArmFrame = ref npc.localAI[1];
            ref float ScreenShaderStrength = ref npc.localAI[3];

            npc.damage = 0;
            npc.spriteDirection = 1;
            leftArmFrame = 0f;
            rightArmFrame = 0f;
            npc.dontTakeDamage = false;

            // Disappear if the target is dead.
            if (!target.active || target.dead)
            {
                npc.active = false;
                return false;
            }

            // Enter new phases.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (currentPhase == 0f && lifeRatio < Phase2LifeRatio)
            {
                SelectNextAttack(npc);
                ClearAwayEntities();
                npc.Infernum().ExtraAI[5] = 0f;
                attackType = (int)EmpressOfLightAttackType.EnterSecondPhase;
                currentPhase = 1f;
                npc.netUpdate = true;
            }

            if (currentPhase == 1f && lifeRatio < Phase3LifeRatio)
            {
                currentPhase = 2f;
                npc.Opacity = 1f;
                SelectNextAttack(npc);
                ClearAwayEntities();
                npc.Infernum().ExtraAI[5] = 0f;
                attackType = (int)EmpressOfLightAttackType.LightOverload;
                npc.netUpdate = true;
            }

            if (currentPhase == 2f && lifeRatio < Phase4LifeRatio)
            {
                currentPhase = 3f;
                npc.Opacity = 1f;
                npc.Infernum().ExtraAI[5] = 0f;
                SelectNextAttack(npc);
                ClearAwayEntities();
                npc.netUpdate = true;
            }

            // Restrict the player's position.
            float initialXPosition = npc.Infernum().ExtraAI[6];

            if (initialXPosition != 0f)
            {
                float left = initialXPosition - BorderWidth * 0.5f + 30f;
                float right = initialXPosition + BorderWidth * 0.5f - 30f;
                target.Center = Vector2.Clamp(target.Center, new Vector2(left + target.width * 0.5f, -100f), new Vector2(right - target.width * 0.5f, Main.maxTilesY * 16f + 100f));
                if (target.Center.X < left + 160f)
                {
                    Dust magic = Dust.NewDustPerfect(new Vector2(left - 12f, target.Center.Y), 261);
                    magic.velocity = Main.rand.NextVector2Circular(10f, 5f);
                    magic.velocity.X = Math.Abs(magic.velocity.X);
                    magic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.72f);
                    magic.scale = 1.1f;
                    magic.fadeIn = 1.4f;
                    magic.noGravity = true;
                }
                if (target.Center.X > right - 160f)
                {
                    Dust magic = Dust.NewDustPerfect(new Vector2(right + 12f, target.Center.Y), 261);
                    magic.velocity = Main.rand.NextVector2Circular(10f, 5f);
                    magic.velocity.X = -Math.Abs(magic.velocity.X);
                    magic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.72f);
                    magic.scale = 1.1f;
                    magic.fadeIn = 1.4f;
                    magic.noGravity = true;
                }
            }

            if (ShouldBeEnraged)
                npc.Calamity().CurrentlyEnraged = true;

            switch ((EmpressOfLightAttackType)attackType)
            {
                case EmpressOfLightAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.PrismaticBoltCircle:
                    DoBehavior_PrismaticBoltCircle(npc, target, ref attackTimer, ref leftArmFrame);
                    break;
                case EmpressOfLightAttackType.MesmerizingMagic:
                    DoBehavior_MesmerizingMagic(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.HorizontalCharge:
                    DoBehavior_HorizontalCharge(npc, target, ref attackTimer);
                    break;
                case EmpressOfLightAttackType.EnterSecondPhase:
                    DoBehavior_EnterSecondPhase(npc, target, ref attackTimer);
                    break;
                case EmpressOfLightAttackType.LanceOctagon:
                    DoBehavior_LanceOctagon(npc, target, ref attackTimer);
                    break;
                case EmpressOfLightAttackType.RainbowWispForm:
                    DoBehavior_RainbowWispForm(npc, target, ref attackTimer, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.DanceOfSwords:
                    DoBehavior_DanceOfSwords(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.LightOverload:
                    DoBehavior_LightOverload(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.ShimmeringDiamondLanceBarrage:
                    DoBehavior_ShimmeringDiamondLanceBarrage(npc, target, ref attackTimer, ref leftArmFrame, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.LaserStorm:
                    DoBehavior_LaserStorm(npc, target, ref attackTimer, ref rightArmFrame);
                    break;
                case EmpressOfLightAttackType.InfiniteBrilliance:
                    DoBehavior_InfiniteBrilliance(npc, target, ref attackTimer);
                    break;
            }

            // Manage the screen shader.
            if (Main.netMode != NetmodeID.Server)
            {
                ScreenShaderStrength = 1f;
                if (attackType == (int)EmpressOfLightAttackType.EnterSecondPhase)
                    ScreenShaderStrength = Utils.GetLerpValue(SecondPhaseFadeoutTime, SecondPhaseFadeoutTime + SecondPhaseFadeBackInTime, attackTimer, true);

                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage("Images/Misc/noise");
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsTexture").Value, 1);
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseImage("Images/Misc/Perlin", 2);
                Filters.Scene["InfernumMode:EmpressOfLight"].GetShader().UseIntensity(ScreenShaderStrength);
            }

            wingFrameCounter++;
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)
        {
            int spawnAnimationTime = EmpressAurora.Lifetime - 30;

            // Fade in.
            npc.Opacity = MathHelper.Clamp((float)Math.Sqrt(attackTimer / spawnAnimationTime), 0f, 1f);

            // Disable damage.
            npc.dontTakeDamage = true;

            // Summon auroras and descend.
            if (attackTimer == 1f)
            {
                npc.Infernum().ExtraAI[6] = target.Center.X;
                npc.velocity = Vector2.UnitY * 5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 auroraSpawnPosition = npc.Center - Vector2.UnitY * 80f;
                    Utilities.NewProjectileBetter(auroraSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EmpressAurora>(), 0, 0f);
                }

                npc.netUpdate = true;
            }

            npc.velocity *= 0.95f;

            // Scream shortly after being summoned.
            if (attackTimer == 10f)
                SoundEngine.PlaySound(SoundID.Item161, npc.Center);

            // Hold hands together for the first part of the animation.
            if (attackTimer < spawnAnimationTime * 0.67f)
            {
                leftArmFrame = 5f;
                rightArmFrame = 5f;
            }

            // Cast shimmers.
            if (attackTimer > 10f && attackTimer < spawnAnimationTime - 10f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float dustPersistence = MathHelper.Lerp(1.3f, 0.7f, npc.Opacity) * Utils.GetLerpValue(0f, spawnAnimationTime - 60f, attackTimer, true);
                    Color newColor = Main.hslToRgb((attackTimer / spawnAnimationTime + Main.rand.NextFloat(0.1f)) % 1f, 1f, 0.5f);
                    Dust rainbowMagic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 0, newColor, 1f);
                    rainbowMagic.position = npc.Center + Main.rand.NextVector2Circular(npc.width * 12f, npc.height * 12f) + new Vector2(0f, -150f);
                    rainbowMagic.velocity *= Main.rand.NextFloat(0.8f);
                    rainbowMagic.noGravity = true;
                    rainbowMagic.fadeIn = 0.7f + Main.rand.NextFloat(dustPersistence * 0.7f);
                    rainbowMagic.velocity += Vector2.UnitY * 3f;
                    rainbowMagic.scale = 0.6f;

                    rainbowMagic = Dust.CloneDust(rainbowMagic);
                    rainbowMagic.scale /= 2f;
                    rainbowMagic.fadeIn *= 0.85f;
                }
            }

            if (attackTimer >= spawnAnimationTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_PrismaticBoltCircle(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame)
        {
            int boltReleaseDelay = 90;
            int boltReleaseTime = 74;
            int boltReleaseRate = 2;
            int attackSwitchDelay = 300;
            float boltSpeed = 10.5f;
            Vector2 handOffset = new(-55f, -30f);

            if (InPhase2(npc))
            {
                boltReleaseRate--;
                boltSpeed += 2f;
            }

            if (BossRushEvent.BossRushActive)
                boltSpeed *= 1.5f;

            // Hover to the top left/right of the target.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 400f, -250f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 13.5f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity, 0.75f);

            // Play a magic sound.
            if (attackTimer == boltReleaseDelay)
                SoundEngine.PlaySound(SoundID.Item164, npc.Center);

            // Fade out and teleport to the opposite side of the target halfway through the attack.
            if (attackTimer >= boltReleaseDelay + boltReleaseTime / 2 - 10 && attackTimer <= boltReleaseDelay + boltReleaseTime / 2)
            {
                npc.Opacity = Utils.GetLerpValue(0f, -10f, attackTimer - (boltReleaseDelay + boltReleaseTime / 2), true);
                if (npc.Opacity <= 0f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                    float horizontalDistanceFromtarget = target.Center.X - npc.Center.X;
                    if (Math.Abs(horizontalDistanceFromtarget) < 600f)
                        horizontalDistanceFromtarget = Math.Sign(horizontalDistanceFromtarget) * 600f;
                    if (Math.Abs(horizontalDistanceFromtarget) > 1200f)
                        horizontalDistanceFromtarget = Math.Sign(horizontalDistanceFromtarget) * 1200f;

                    npc.Opacity = 1f;
                    npc.position.X += horizontalDistanceFromtarget * 2f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                    npc.netUpdate = true;
                }
            }

            // Release bolts.
            if (attackTimer >= boltReleaseDelay && attackTimer < boltReleaseDelay + boltReleaseTime)
            {
                leftArmFrame = 3f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % boltReleaseRate == 0f)
                {
                    float castCompletionInterpolant = Utils.GetLerpValue(boltReleaseDelay, boltReleaseDelay + boltReleaseTime, attackTimer, true);
                    Vector2 boltVelocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * castCompletionInterpolant) * boltSpeed;
                    int bolt = Utilities.NewProjectileBetter(npc.Center + handOffset, boltVelocity, ModContent.ProjectileType<PrismaticBolt>(), PrismaticBoltDamage, 0f);
                    if (Main.projectile.IndexInRange(bolt))
                    {
                        Main.projectile[bolt].ai[0] = npc.target;
                        Main.projectile[bolt].ai[1] = castCompletionInterpolant;
                    }
                }
            }

            if (attackTimer >= boltReleaseDelay + boltReleaseTime + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MesmerizingMagic(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)
        {
            int shootRate = 75;
            int shootCount = 6;
            float wrappedattackTimer = attackTimer % shootRate;
            float slowdownFactor = Utils.GetLerpValue(shootRate - 8f, shootRate - 24f, wrappedattackTimer, true);
            float boltShootSpeed = 17f;
            ref float telegraphRotation = ref npc.Infernum().ExtraAI[0];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float boltCount = ref npc.Infernum().ExtraAI[2];
            ref float totalHandsToShootFrom = ref npc.Infernum().ExtraAI[3];
            ref float shootCounter = ref npc.Infernum().ExtraAI[4];

            // Initialize things.
            if (totalHandsToShootFrom == 0f)
            {
                boltCount = 25f;
                totalHandsToShootFrom = 1f;
                if (InPhase2(npc))
                {
                    boltCount = 18f;
                    totalHandsToShootFrom = 2f;
                }

                if (ShouldBeEnraged)
                {
                    boltCount = 21f;
                    totalHandsToShootFrom = 2f;
                }

                npc.netUpdate = true;
            }

            // Calculate the telegraph interpolant.
            telegraphInterpolant = Utils.GetLerpValue(24f, shootRate - 18f, wrappedattackTimer);

            // Hover to the top left/right of the target.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 120f, -300f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 8f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity * slowdownFactor, slowdownFactor * 0.7f);
            else
                npc.velocity *= 0.93f;

            // Determinine the initial rotation of the telegraphs.
            if (wrappedattackTimer == 4f)
            {
                telegraphRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                npc.netUpdate = true;
            }

            // Rotate the telegraphs.
            telegraphRotation += CalamityUtils.Convert01To010(telegraphInterpolant) * MathHelper.Pi / 75f;

            // Release magic on hands and eventually create bolts.
            int magicDustCount = (int)Math.Round(MathHelper.Lerp(1f, 5f, telegraphInterpolant));
            for (int i = 0; i < 2; i++)
            {
                if (i >= totalHandsToShootFrom)
                    break;

                int handDirection = (i == 0).ToDirectionInt();
                Vector2 handOffset = new(55f, -30f);
                Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                // Create magic dust.
                for (int j = 0; j < magicDustCount; j++)
                {
                    float magicHue = (attackTimer / 45f + Main.rand.NextFloat(0.2f)) % 1f;
                    Dust rainbowMagic = Dust.NewDustPerfect(handPosition, 267);
                    rainbowMagic.color = Main.hslToRgb(magicHue, 1f, 0.5f);
                    rainbowMagic.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 4f);
                    rainbowMagic.scale *= 0.9f;
                    rainbowMagic.noGravity = true;
                }

                // Raise hands.
                if (i == 0)
                    rightArmFrame = 3;
                else
                    leftArmFrame = 3;

                // Release bolts outward and create hand explosions.
                if (wrappedattackTimer == shootRate - 1f)
                {
                    if (i == 0)
                        SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 2.6f}, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int j = 0; j < boltCount; j++)
                        {
                            Vector2 boltShootVelocity = (MathHelper.TwoPi * j / boltCount + telegraphRotation).ToRotationVector2() * boltShootSpeed;
                            int bolt = Utilities.NewProjectileBetter(handPosition, boltShootVelocity, ModContent.ProjectileType<AcceleratingPrismaticBolt>(), PrismaticBoltDamage, 0f);
                            if (Main.projectile.IndexInRange(bolt))
                                Main.projectile[bolt].ai[1] = j / boltCount;
                        }
                        Utilities.NewProjectileBetter(handPosition, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                        if (i == 0)
                            shootCounter++;

                        npc.netUpdate = true;
                    }
                }
            }

            if (shootCounter >= shootCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalCharge(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 6;
            int redirectTime = 40;
            int chargeTime = 45;
            int attackTransitionDelay = 8;
            float chargeSpeed = 56f;
            float hoverSpeed = 25f;
            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            if (chargeCounter == 0f)
                redirectTime += 16;

            if (InPhase2(npc))
                chargeSpeed += 4f;

            if (InPhase3(npc))
                chargeSpeed += 5f;

            if (ShouldBeEnraged)
                chargeSpeed += 8f;

            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed *= 1.3f;
                hoverSpeed *= 1.5f;
            }

            // Initialize the charge direction.
            if (attackTimer == 1f)
            {
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Hover into position before charging.
            if (attackTimer <= redirectTime)
            {
                // Scream prior to charging.
                if (attackTimer == redirectTime / 2)
                    SoundEngine.PlaySound(SoundID.Item160, npc.Center);

                Vector2 hoverDestination = target.Center + Vector2.UnitX * chargeDirection * -420f;
                npc.Center = npc.Center.MoveTowards(hoverDestination, 12.5f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed * 0.16f);
                if (attackTimer == redirectTime)
                    npc.velocity *= 0.3f;
            }
            else if (attackTimer <= redirectTime + chargeTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.15f);
                if (attackTimer == redirectTime + chargeTime)
                    npc.velocity *= 0.7f;

                // Do damage and become temporarily invulnerable. This is done to prevent dash-cheese.
                npc.damage = npc.defDamage;
                if (ShouldBeEnraged)
                    npc.damage *= 2;

                npc.dontTakeDamage = true;
            }
            else
                npc.velocity *= 0.92f;

            if (attackTimer >= redirectTime + chargeTime + attackTransitionDelay)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_EnterSecondPhase(NPC npc, Player target, ref float attackTimer)
        {
            int reappearTime = 35;

            // Don't take damage when transitioning.
            npc.dontTakeDamage = true;

            // Slow down.
            npc.velocity *= 0.8f;

            // Scream before fading out.
            if (attackTimer == 10f)
                SoundEngine.PlaySound(SoundID.Item161, npc.Center);

            // Fade out.
            if (attackTimer <= SecondPhaseFadeoutTime)
                npc.Opacity = MathHelper.Lerp(1f, 0f, attackTimer / SecondPhaseFadeoutTime);

            // Fade back in and teleport above the target.
            else if (attackTimer <= SecondPhaseFadeoutTime + reappearTime)
            {
                if (attackTimer == SecondPhaseFadeoutTime + 1f)
                {
                    npc.Center = target.Center - Vector2.UnitY * 300f;
                    npc.netUpdate = true;
                }

                npc.Opacity = Utils.GetLerpValue(0f, reappearTime, attackTimer - SecondPhaseFadeoutTime, true);
            }

            if (attackTimer >= SecondPhaseFadeoutTime + SecondPhaseFadeBackInTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_LanceOctagon(NPC npc, Player target, ref float attackTimer)
        {
            int lanceCount = 10;
            int lanceCreationRate = 50;
            int lanceBarrageCount = 8;
            float lanceSpawnOffset = 1000f;
            float targetCircleOffset = 350f;
            ref float lanceBarrageCounter = ref npc.Infernum().ExtraAI[0];

            if (InPhase2(npc))
                lanceCreationRate -= 7;

            if (InPhase3(npc))
                lanceCreationRate -= 12;

            if (ShouldBeEnraged)
                lanceCreationRate = Utils.Clamp(lanceCreationRate - 8, 28, 150);

            // Hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 310f;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 7f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity, 0.75f);
            else
                npc.velocity *= 0.96f;

            // Release lance telegraphs at the target. They will fire short afterward.
            if (attackTimer % lanceCreationRate == lanceCreationRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item162, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lanceOffset = (MathHelper.TwoPi * lanceBarrageCounter / lanceBarrageCount).ToRotationVector2() * lanceSpawnOffset;
                    Vector2 offsetOrthogonalDirection = lanceOffset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);

                    for (int i = 0; i < lanceCount; i++)
                    {
                        Vector2 orthogonalOffset = offsetOrthogonalDirection * MathHelper.Lerp(-1f, 1f, i / (float)(lanceCount - 1f));
                        Vector2 lanceDestination = target.Center + orthogonalOffset.RotatedBy(MathHelper.PiOver2) * targetCircleOffset;
                        Vector2 lanceSpawnPosition = target.Center + lanceOffset + orthogonalOffset * lanceSpawnOffset * 0.8f;

                        int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f);
                        if (Main.projectile.IndexInRange(lance))
                        {
                            Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                            Main.projectile[lance].ai[1] = i / (float)lanceCount;
                        }

                        lanceSpawnPosition = target.Center + lanceOffset - orthogonalOffset * lanceSpawnOffset * 0.8f;

                        lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f);
                        if (Main.projectile.IndexInRange(lance))
                        {
                            Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                            Main.projectile[lance].ai[1] = i / (float)lanceCount;
                        }
                    }
                    lanceBarrageCounter++;
                    if (lanceBarrageCounter >= lanceBarrageCount)
                        SelectNextAttack(npc);

                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_RainbowWispForm(NPC npc, Player target, ref float attackTimer, ref float rightArmFrame)
        {
            int wispFormEnterTime = 60;
            int spinTime = 210;
            int prismCreationRate = 15;
            int laserReleaseDelay = 30;
            int boltReleaseRate = 35;
            int attackTransitionDelay = 360;
            float spinSpeed = 30f;
            float spinOffset = 540f;
            ref float wispColorInterpolant = ref npc.Infernum().ExtraAI[0];

            if (InPhase3(npc))
            {
                prismCreationRate -= 5;
                boltReleaseRate -= 5;
            }

            if (ShouldBeEnraged)
                boltReleaseRate -= 7;

            // Hover above the target and enter the wisp form.
            if (attackTimer <= wispFormEnterTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 480f;
                npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, 16f);
                wispColorInterpolant = attackTimer / wispFormEnterTime;
            }

            else
            {
                float spinSlowdownFactor = Utils.GetLerpValue(spinTime, spinTime - prismCreationRate, attackTimer - wispFormEnterTime, true);
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(MathHelper.TwoPi / spinTime * 2f * attackTimer) * spinOffset;
                npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, spinSlowdownFactor * spinSpeed);

                // Release prisms periodically.
                if (attackTimer % prismCreationRate == prismCreationRate - 1f && spinSlowdownFactor > 0f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);
                        int prism = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressPrism>(), 0, 0f);
                        if (Main.projectile.IndexInRange(prism))
                        {
                            Main.projectile[prism].ai[0] = attackTimer - (wispFormEnterTime + spinTime) - laserReleaseDelay;
                            Main.projectile[prism].ai[1] = npc.target;
                        }
                    }
                }

                // Raise the right hand upward.
                rightArmFrame = 3f;

                // Release prismatic bolts at the target occasionally.
                if (attackTimer % boltReleaseRate == boltReleaseRate - 1f)
                {
                    Vector2 handOffset = new(55f, -30f);
                    Vector2 handPosition = npc.Center + handOffset;
                    int bolt = Utilities.NewProjectileBetter(handPosition, -Vector2.UnitY.RotatedByRandom(0.6f) * 16f, ModContent.ProjectileType<PrismaticBolt>(), PrismaticBoltDamage, 0f);
                    if (Main.projectile.IndexInRange(bolt))
                        Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                }
            }

            // Fade in and out.
            if (attackTimer >= wispFormEnterTime + spinTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 425f;
                npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, 30f);
                npc.Opacity = MathHelper.Lerp(0.3f, 1f, Utils.GetLerpValue(150f, 240f, npc.Distance(target.Center), true));

                wispColorInterpolant = Utils.GetLerpValue(wispFormEnterTime + spinTime + 60f, wispFormEnterTime + spinTime, attackTimer, true);
            }

            if (attackTimer >= wispFormEnterTime + spinTime + attackTransitionDelay)
            {
                npc.Opacity = 1f;
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_DanceOfSwords(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)
        {
            int swordCount = 9;
            int totalSwordsThatShouldAttack = 3;
            int swordSummonDelay = 50;
            int attackTimePerSword = 115;
            int lanceReleaseRate = 125;
            int lanceCount = 15;
            float lanceSpawnOffset = 1000f;
            float lanceWallSize = 900f;

            if (InPhase3(npc) || ShouldBeEnraged)
            {
                swordCount += 3;
                totalSwordsThatShouldAttack++;
                lanceCount += 5;
                lanceWallSize += 120f;
            }

            if (InPhase4(npc) || (ShouldBeEnraged && InPhase3(npc)))
            {
                totalSwordsThatShouldAttack += 2;
                lanceReleaseRate -= 35;
                lanceCount += 3;
            }

            if (BossRushEvent.BossRushActive)
                lanceWallSize += 250f;

            int swordAttackCount = swordCount / totalSwordsThatShouldAttack;

            // Define the adjusted attack timer.
            npc.Infernum().ExtraAI[0] = attackTimer - swordSummonDelay;

            // Hover to the top left/right of the target.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 150f, -300f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 13.5f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(idealVelocity, 0.75f);
            else
                npc.velocity *= 0.95f;

            // Perform a cast animation and release swords.
            if (attackTimer < swordSummonDelay)
            {
                // Press hands together at first.
                if (attackTimer < swordSummonDelay)
                    leftArmFrame = rightArmFrame = 1f;

                // And then raise hands upwards.
                // Raise magic from the hands as well.
                else
                {
                    leftArmFrame = rightArmFrame = 3f;
                    for (int i = 0; i < 2; i++)
                    {
                        int handDirection = (i == 0).ToDirectionInt();
                        Vector2 handOffset = new(55f, -30f);
                        Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                        // Create magic dust.
                        for (int j = 0; j < 4; j++)
                        {
                            Dust rainbowMagic = Dust.NewDustPerfect(handPosition, 267);
                            rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.5f);
                            rainbowMagic.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 4f);
                            rainbowMagic.scale *= 0.9f;
                            rainbowMagic.noGravity = true;
                        }
                    }
                }
            }

            // Summon swords.
            if (attackTimer == swordSummonDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < swordCount; i++)
                    {
                        int sword = Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 4f, ModContent.ProjectileType<EmpressSword>(), SwordDamage, 0f);
                        if (Main.projectile.IndexInRange(sword))
                        {
                            Main.projectile[sword].ai[0] = npc.whoAmI;
                            Main.projectile[sword].ai[1] = i / (float)swordCount;
                            Main.projectile[sword].ModProjectile<EmpressSword>().SwordIndex = i;
                            Main.projectile[sword].ModProjectile<EmpressSword>().SwordCount = swordCount;
                            Main.projectile[sword].ModProjectile<EmpressSword>().TotalSwordsThatShouldAttack = totalSwordsThatShouldAttack;
                            Main.projectile[sword].ModProjectile<EmpressSword>().AttackTimePerSword = attackTimePerSword;
                        }
                    }
                    npc.netUpdate = true;
                }
            }

            // Summon lance walls.
            if (attackTimer >= swordSummonDelay)
            {
                float adjustedattackTimer = attackTimer % lanceReleaseRate;

                // Raise hands and cast magic before summoning lances.
                if (adjustedattackTimer >= lanceReleaseRate - 50f)
                {
                    leftArmFrame = rightArmFrame = 3f;
                    for (int i = 0; i < 2; i++)
                    {
                        int handDirection = (i == 0).ToDirectionInt();
                        Vector2 handOffset = new(55f, -30f);
                        Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                        // Create magic dust.
                        for (int j = 0; j < 4; j++)
                        {
                            Dust rainbowMagic = Dust.NewDustPerfect(handPosition, 267);
                            rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.5f);
                            rainbowMagic.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 4f);
                            rainbowMagic.scale *= 0.9f;
                            rainbowMagic.noGravity = true;
                        }
                    }
                }

                // Summon lances.
                if (adjustedattackTimer == lanceReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item162, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = MathHelper.TwoPi * Main.rand.Next(4) / 4f;
                        Vector2 baseSpawnPosition = target.Center + offsetAngle.ToRotationVector2() * lanceSpawnOffset;
                        Vector2 lanceDestination = target.Center + target.velocity * 20f;
                        for (int i = 0; i < lanceCount; i++)
                        {
                            Vector2 lanceSpawnPosition = baseSpawnPosition + (offsetAngle + MathHelper.PiOver2).ToRotationVector2() * MathHelper.Lerp(-1f, 1f, i / (float)lanceCount) * lanceWallSize;
                            int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f);
                            if (Main.projectile.IndexInRange(lance))
                            {
                                Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                                Main.projectile[lance].ai[1] = i / (float)lanceCount;
                            }
                        }
                    }
                }
            }

            // Make swords fade away.
            if (attackTimer >= swordSummonDelay + attackTimePerSword * swordAttackCount + EmpressSword.AttackDelay)
            {
                foreach (Projectile sword in Utilities.AllProjectilesByID(ModContent.ProjectileType<EmpressSword>()))
                    sword.timeLeft = 30;

                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_LightOverload(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)
        {
            int hoverTime = 120;
            int orbCastDelay = 45;
            int orbGrowDelay = 25;
            int orbGrowTime = 45;
            int orbAttackTime = 240;
            float smallOrbSize = 12f;
            float bigOrbSize = 400f;
            Vector2 orbSummonSpawnPosition = npc.Center + Vector2.UnitY * 8f;
            ref float orbSize = ref npc.Infernum().ExtraAI[0];
            ref float lightOrb = ref npc.Infernum().ExtraAI[1];
            ref float fadeAwayInterpolant = ref npc.Infernum().ExtraAI[2];

            // Hover in place at first before slowing down.
            if (attackTimer < hoverTime && !npc.WithinRange(target.Center, 200f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 15f, 0.1f);
            else
                npc.velocity *= 0.92f;

            // Cast magic prior to creating the orb.
            if (attackTimer >= hoverTime && attackTimer <= hoverTime + orbCastDelay)
            {
                // Hold hands together.
                leftArmFrame = rightArmFrame = 1f;
                orbSize = smallOrbSize;

                for (int i = 0; i < 6; i++)
                {
                    Vector2 dustOffsetDirection = Main.rand.NextVector2Unit();
                    Dust rainbowMagic = Dust.NewDustPerfect(orbSummonSpawnPosition + dustOffsetDirection * Main.rand.NextFloat(smallOrbSize), 267);
                    rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.7f);
                    rainbowMagic.velocity = dustOffsetDirection.RotatedBy(Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2) * Main.rand.NextFloat(0.8f, 4f);
                    rainbowMagic.noGravity = true;
                    rainbowMagic.scale = 1.2f;
                }
            }

            // Create the orb.
            if (attackTimer == hoverTime + orbCastDelay)
            {
                SoundEngine.PlaySound(SoundID.Item163, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    lightOrb = Utilities.NewProjectileBetter(orbSummonSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LightOrb>(), 0, 0f);
                    if (Main.projectile.IndexInRange((int)lightOrb))
                        Main.projectile[(int)lightOrb].ai[1] = npc.whoAmI;
                    npc.netUpdate = true;
                }
            }

            // Rise upward.
            if (attackTimer == hoverTime + orbCastDelay + orbGrowDelay + 10f)
                npc.velocity = -Vector2.UnitY * 27f;

            // Make the orb grow and cast magic towards it.
            if (attackTimer >= hoverTime + orbCastDelay + orbGrowDelay)
            {
                orbSize = MathHelper.SmoothStep(smallOrbSize, bigOrbSize, Utils.GetLerpValue(0f, orbGrowTime, attackTimer - (hoverTime + orbCastDelay + orbGrowDelay), true));

                leftArmFrame = rightArmFrame = 2f;

                Projectile lightOrbProj = Main.projectile[(int)lightOrb];
                for (int i = 0; i < 2; i++)
                {
                    int handDirection = (i == 0).ToDirectionInt();
                    Vector2 handOffset = new(handDirection * 60f, 4f);
                    Vector2 handPosition = npc.Center + handOffset;

                    for (int j = 0; j < 4; j++)
                    {
                        Dust rainbowMagic = Dust.NewDustPerfect(handPosition, 267);
                        rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.65f);
                        rainbowMagic.velocity = (lightOrbProj.Center + Main.rand.NextVector2Circular(85f, 85f) - handPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(5f, 15f);
                        rainbowMagic.scale = 1.1f;
                        rainbowMagic.noGravity = true;
                    }
                }
            }

            // Eventually make the light orb fade away.
            fadeAwayInterpolant = Utils.GetLerpValue(0f, 60f, attackTimer - (hoverTime + orbCastDelay + orbGrowDelay + orbGrowTime + LightOrb.LaserReleaseDelay + orbAttackTime), true);

            if (attackTimer >= hoverTime + orbCastDelay + orbGrowDelay + orbGrowTime + LightOrb.LaserReleaseDelay + orbAttackTime + 180f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ShimmeringDiamondLanceBarrage(NPC npc, Player target, ref float attackTimer, ref float leftArmFrame, ref float rightArmFrame)
        {
            int dissapearTime = 45;
            int handClapDelay = 60;
            int lanceCount = 4;
            int lanceBurstReleaseRate = 64;
            int lanceBurstCount = 6;
            float lanceSpawnOffset = 850f;
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 360f;

            if (ShouldBeEnraged)
                lanceBurstReleaseRate -= 8;

            if (BossRushEvent.BossRushActive)
            {
                lanceBurstReleaseRate -= 8;
                lanceSpawnOffset += 300f;
            }

            // Disappear before doing anything else.
            if (attackTimer <= dissapearTime)
            {
                npc.Opacity = 1f - attackTimer / dissapearTime;
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 5f, 0.15f);

                // Hold hands together while disappearing.
                leftArmFrame = rightArmFrame = 1f;

                int dustCount = (int)MathHelper.Lerp(2f, 8f, 1f - npc.Opacity);
                for (int i = 0; i < dustCount; i++)
                {
                    Dust rainbowShimmer = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(150f, 150f), 261);
                    rainbowShimmer.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.65f);
                    rainbowShimmer.scale = 1.05f;
                    rainbowShimmer.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 6f);
                    rainbowShimmer.noGravity = true;
                }
            }

            // Teleport above the target.
            if (attackTimer == dissapearTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = hoverDestination;
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);

                    npc.netUpdate = true;
                }

                // Create a ring of rainbow dust.
                for (int i = 0; i < 24; i++)
                {
                    Dust rainbowDust = Dust.NewDustPerfect(npc.Center, 267);
                    rainbowDust.color = Main.hslToRgb(i / 24f, 1f, 0.6f);
                    rainbowDust.velocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 4f;
                    rainbowDust.scale = 1.25f;
                    rainbowDust.fadeIn = 1.2f;
                    rainbowDust.noLight = true;
                    rainbowDust.noGravity = true;
                }

                // Scream after teleporting.
                SoundEngine.PlaySound(SoundID.Item160, npc.Center);
            }

            // Hover above the target after teleporting.
            if (attackTimer > dissapearTime)
            {
                // Fade back in.
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);

                if (!npc.WithinRange(hoverDestination, 35f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 16f, 0.85f);

                // Raise hands and create magic before clapping.
                leftArmFrame = rightArmFrame = 3f;
                for (int i = 0; i < 2; i++)
                {
                    if (attackTimer >= dissapearTime + handClapDelay)
                        break;

                    int handDirection = (i == 0).ToDirectionInt();
                    Vector2 handOffset = new(55f, -30f);
                    Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                    // Create magic dust.
                    for (int j = 0; j < 4; j++)
                    {
                        Dust rainbowMagic = Dust.NewDustPerfect(handPosition, 267);
                        rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.5f);
                        rainbowMagic.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.4f, 4.2f);
                        rainbowMagic.scale *= 0.9f;
                        rainbowMagic.noGravity = true;
                    }
                }

                // Clap hands.
                if (attackTimer >= dissapearTime + handClapDelay)
                {
                    if (attackTimer == dissapearTime + handClapDelay)
                    {
                        SoundEngine.PlaySound(SoundID.Item122, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 8f, Vector2.Zero, ModContent.ProjectileType<ShimmeringLightWave>(), 0, 0f);
                            npc.netUpdate = true;
                        }
                    }
                    leftArmFrame = rightArmFrame = 1f;
                }
            }

            // Summon lances.
            if (attackTimer > dissapearTime + handClapDelay && attackTimer % lanceBurstReleaseRate == lanceBurstReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item162, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float universalOffsetAngle = Main.rand.NextBool() ? MathHelper.PiOver4 : 0f;
                    Vector2 lanceDestination = target.Center + target.velocity * 20f;

                    // Diamond lances.
                    for (int i = 0; i < 4; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / 4f + universalOffsetAngle;
                        Vector2 baseSpawnPosition = target.Center + offsetAngle.ToRotationVector2() * lanceSpawnOffset;
                        for (int j = 0; j < lanceCount; j++)
                        {
                            Vector2 lanceSpawnPosition = baseSpawnPosition + (offsetAngle + MathHelper.PiOver2).ToRotationVector2() * MathHelper.Lerp(-1f, 1f, j / (float)lanceCount) * lanceSpawnOffset;
                            int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f);
                            if (Main.projectile.IndexInRange(lance))
                            {
                                Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                                Main.projectile[lance].ai[1] = j / (float)lanceCount;
                                Main.projectile[lance].localAI[1] = lanceSpawnOffset * 4f;
                            }
                        }
                    }

                    // Circular lances.
                    lanceCount *= 3;
                    for (int i = 0; i < lanceCount; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / lanceCount;
                        Vector2 lanceSpawnPosition = target.Center + offsetAngle.ToRotationVector2() * lanceSpawnOffset * 1.414f;
                        int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), 185, 0f);
                        if (Main.projectile.IndexInRange(lance))
                        {
                            Main.projectile[lance].ai[0] = (lanceDestination - lanceSpawnPosition).ToRotation();
                            Main.projectile[lance].ai[1] = i / (float)lanceCount;
                            Main.projectile[lance].localAI[1] = lanceSpawnOffset * 4f;
                        }
                    }
                }
            }

            if (attackTimer >= dissapearTime + handClapDelay + lanceBurstReleaseRate * lanceBurstCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_LaserStorm(NPC npc, Player target, ref float attackTimer, ref float rightArmFrame)
        {
            int shootDelay = 35;

            // Fade in and slow down.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);
            npc.velocity *= 0.95f;

            // Teleport above the target and descend.
            if (attackTimer == 1f)
            {
                npc.Center = target.Center - Vector2.UnitY * 420f;
                if (npc.position.Y < 2000f)
                    npc.position.Y = 2000f;

                npc.velocity = Vector2.UnitY * 5f;
                npc.Opacity = 0f;
                npc.netUpdate = true;
            }

            // Raise the right arm pointer finger in the air, towards the sky.
            if (attackTimer < shootDelay + 15f)
                rightArmFrame = 4f;

            // Summon the cloud.
            if (attackTimer == shootDelay - 1f)
            {
                Vector2 fingerPosition = npc.Center + new Vector2(35f, -45f);
                Vector2 cloudSpawnPosition = fingerPosition - Vector2.UnitY * 450f;
                while (target.WithinRange(cloudSpawnPosition, 270f))
                    cloudSpawnPosition.X++;

                SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, fingerPosition);
                Dust.QuickDustLine(fingerPosition, cloudSpawnPosition, 250f, Color.LightSkyBlue);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(cloudSpawnPosition, Vector2.Zero, ModContent.ProjectileType<LightCloud>(), CloudDamage, 0f);
                    Utilities.NewProjectileBetter(fingerPosition, Vector2.Zero, ModContent.ProjectileType<EmpressExplosion>(), 0, 0f);
                }
            }

            if (attackTimer >= shootDelay + LightCloud.CloudLifetime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_InfiniteBrilliance(NPC npc, Player target, ref float attackTimer)
        {
            int telegraphTime = 175;
            int laserbeamCount = 30;
            int lanceReleaseRate = 72;
            int lanceCount = 6;
            int boltReleaseRate = 50;
            float maxTelegraphTilt = 0.253f;
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float telegraphRotation = ref npc.Infernum().ExtraAI[1];

            if (ShouldBeEnraged)
            {
                lanceReleaseRate -= 10;
                boltReleaseRate -= 8;
            }

            // Reset the telegraph interpolant.
            telegraphInterpolant = 0f;

            // Hover into position very quickly and create telegraphs.
            if (attackTimer < telegraphTime)
            {
                telegraphInterpolant = MathHelper.Clamp(attackTimer / telegraphTime, 0f, 1f);
                telegraphRotation = MathHelper.Lerp(MathHelper.PiOver2, maxTelegraphTilt, Utils.GetLerpValue(0f, 0.4f, attackTimer / telegraphTime, true));

                float slowdownFactor = Utils.GetLerpValue(telegraphTime - 24f, telegraphTime - 70f, attackTimer, true);
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, -120f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, slowdownFactor * 6f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * slowdownFactor * 16f, 1f);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero.MoveTowards(hoverDestination - npc.Center, slowdownFactor * 16f), 0.1f);
            }

            // Release lasers.
            if (attackTimer == telegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item160, npc.Center);

                int laserDamage = (int)(LaserbeamDamage * 0.7f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < laserbeamCount; i++)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            int laser = Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 8f, Vector2.Zero, ModContent.ProjectileType<SpinningPrismLaserbeam2>(), laserDamage, 0f);
                            if (Main.projectile.IndexInRange(laser))
                            {
                                Main.projectile[laser].ModProjectile<SpinningPrismLaserbeam2>().LaserbeamIDRatio = MathHelper.Lerp(-0.5f, 0.5f, i / (float)laserbeamCount) * laserbeamCount;
                                Main.projectile[laser].ModProjectile<SpinningPrismLaserbeam2>().VerticalSpinDirection = j;
                                Main.projectile[laser].ModProjectile<SpinningPrismLaserbeam2>().AngularOffset = MathHelper.PiOver2 - maxTelegraphTilt;
                                Main.projectile[laser].ModProjectile<SpinningPrismLaserbeam2>().LaserCount = laserbeamCount;
                            }
                        }
                    }
                }

                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }

            // Summon various things after the lasers have been fired.
            if (attackTimer >= telegraphTime)
            {
                // Release bursts of lances.
                if (attackTimer % lanceReleaseRate == lanceReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item162, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float hueOffset = Main.rand.NextFloat();
                        for (int i = 0; i < lanceCount; i++)
                        {
                            Vector2 lanceDirection = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.12f, 0.12f, i / (float)(lanceCount - 1f)));
                            Vector2 lanceSpawnPosition = npc.Center + lanceDirection * 50f;

                            int lance = Utilities.NewProjectileBetter(lanceSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EtherealLance>(), LanceDamage, 0f);
                            if (Main.projectile.IndexInRange(lance))
                            {
                                Main.projectile[lance].ai[0] = lanceDirection.ToRotation();
                                Main.projectile[lance].ai[1] = (i / (float)lanceCount + hueOffset) % 1f;
                            }
                        }
                    }
                }

                // Summon prismatic bolts from the sky.
                if (attackTimer % boltReleaseRate == boltReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item28, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 boltSpawnPosition = target.Center - Vector2.UnitY.RotatedBy(0.6f) * 960f;
                        Vector2 boltVelocity = (target.Center - boltSpawnPosition).SafeNormalize(Vector2.UnitY) * 9f;
                        int bolt = Utilities.NewProjectileBetter(boltSpawnPosition, boltVelocity, ModContent.ProjectileType<PrismaticBolt>(), PrismaticBoltDamage, 0f);
                        if (Main.projectile.IndexInRange(bolt))
                        {
                            Main.projectile[bolt].ai[0] = npc.target;
                            Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                        }
                    }
                }
            }

            if (attackTimer >= telegraphTime + SpinningPrismLaserbeam2.Lifetime)
                SelectNextAttack(npc);
        }

        public static void ClearAwayEntities()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Clear any clones or other things that might remain from other attacks.
            int[] projectilesToClearAway = new int[]
            {
                ModContent.ProjectileType<AcceleratingPrismaticBolt>(),
                ModContent.ProjectileType<EmpressPrism>(),
                ModContent.ProjectileType<EmpressSparkle>(),
                ModContent.ProjectileType<EmpressSword>(),
                ModContent.ProjectileType<EtherealLance>(),
                ModContent.ProjectileType<LightBolt>(),
                ModContent.ProjectileType<LightCloud>(),
                ModContent.ProjectileType<LightOrb>(),
                ModContent.ProjectileType<LightOverloadBeam>(),
                ModContent.ProjectileType<PrismaticBolt>(),
                ModContent.ProjectileType<PrismLaserbeam>(),
                ModContent.ProjectileType<SpinningPrismLaserbeam>(),
                ModContent.ProjectileType<SpinningPrismLaserbeam2>(),
            };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (projectilesToClearAway.Contains(Main.projectile[i].type) && Main.projectile[i].active)
                    Main.projectile[i].active = false;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            int phaseCycleIndex = (int)npc.Infernum().ExtraAI[5];

            npc.ai[0] = (int)Phase1AttackCycle[phaseCycleIndex % Phase1AttackCycle.Length];
            if (InPhase2(npc))
                npc.ai[0] = (int)Phase2AttackCycle[phaseCycleIndex % Phase2AttackCycle.Length];
            if (InPhase3(npc))
                npc.ai[0] = (int)Phase3AttackCycle[phaseCycleIndex % Phase3AttackCycle.Length];
            if (InPhase4(npc))
                npc.ai[0] = (int)Phase4AttackCycle[phaseCycleIndex % Phase4AttackCycle.Length];

            npc.Infernum().ExtraAI[5]++;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames
        public static void PrepareShader()
        {
            Main.graphics.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsTexture").Value;
        }

        public static void DrawBorder(NPC npc, SpriteBatch spriteBatch)
        {
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Cultist/Border").Value;
            float initialXPosition = npc.Infernum().ExtraAI[6];
            float left = initialXPosition - BorderWidth * 0.5f;
            float right = initialXPosition + BorderWidth * 0.5f;
            float leftBorderOpacity = Utils.GetLerpValue(left + 850f, left + 300f, Main.LocalPlayer.Center.X, true);
            float rightBorderOpacity = Utils.GetLerpValue(right - 850f, right - 300f, Main.LocalPlayer.Center.X, true);
            Color startingBorderColor = Color.HotPink;
            Color endingBorderColor = Main.hslToRgb(Main.GlobalTimeWrappedHourly % 1f, 1f, 0.5f);
            if (ShouldBeEnraged)
                endingBorderColor = Main.OurFavoriteColor;

            spriteBatch.SetBlendState(BlendState.Additive);
            if (leftBorderOpacity > 0f)
            {
                Vector2 baseDrawPosition = new Vector2(left, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, leftBorderOpacity, true) * MathHelper.Lerp(400f, 455f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, startingBorderColor, leftBorderOpacity);

                for (int i = 0; i < 80; i++)
                {
                    float fade = 1f - Math.Abs(i - 40f) / 40f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 40f) / 40f * borderOutwardness;
                    spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, endingBorderColor, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
                }
                spriteBatch.Draw(borderTexture, baseDrawPosition, null, Color.Lerp(borderColor, endingBorderColor, 0.5f), 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
            }

            if (rightBorderOpacity > 0f)
            {
                Vector2 baseDrawPosition = new Vector2(right, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, rightBorderOpacity, true) * MathHelper.Lerp(400f, 455f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, startingBorderColor, rightBorderOpacity);

                for (int i = 0; i < 80; i++)
                {
                    float fade = 1f - Math.Abs(i - 40f) / 40f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 40f) / 40f * borderOutwardness;
                    spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, endingBorderColor, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
                }
                spriteBatch.Draw(borderTexture, baseDrawPosition, null, Color.Lerp(borderColor, endingBorderColor, 0.5f), 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
            }

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            EmpressOfLightAttackType attackType = (EmpressOfLightAttackType)npc.ai[0];
            float attackTimer = npc.ai[1];
            float WingFrameCounter = npc.localAI[0];

            // Draw the border.
            DrawBorder(npc, spriteBatch);

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            Color baseColor = Color.White * npc.Opacity;
            Texture2D wingOutlineTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWingsOutline").Value;
            Texture2D leftArmTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightLeftArm").Value;
            Texture2D rightArmTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightRightArm").Value;
            Texture2D wingTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightWings").Value;
            Texture2D tentacleTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightTentacles").Value;
            Texture2D dressGlowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/EmpressOfLight/EmpressOfLightGlowmask").Value;

            Rectangle tentacleFrame = tentacleTexture.Frame(1, 8, 0, (int)(WingFrameCounter / 5f) % 8);
            Rectangle wingFrame = wingOutlineTexture.Frame(1, 11, 0, (int)(WingFrameCounter / 5f) % 11);
            Rectangle leftArmFrame = leftArmTexture.Frame(1, 7, 0, (int)npc.localAI[1]);
            Rectangle rightArmFrame = rightArmTexture.Frame(1, 7, 0, (int)npc.localAI[2]);
            Vector2 origin = leftArmFrame.Size() / 2f;
            Vector2 origin2 = rightArmFrame.Size() / 2f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int leftArmDrawOrder = 0;
            int rightArmDrawOrder = 0;
            if (npc.localAI[1] == 5f)
                leftArmDrawOrder = 1;

            if (npc.localAI[2] == 5f)
                rightArmDrawOrder = 1;

            float baseColorOpacity = 1f;
            int laggingAfterimageCount = 0;
            int baseDuplicateCount = 0;
            float afterimageOffsetFactor = 0f;
            float opacity = 0f;
            float duplicateFade = 0f;

            // Define variables for the horizontal charge state.
            if (attackType == EmpressOfLightAttackType.HorizontalCharge)
            {
                afterimageOffsetFactor = Utils.GetLerpValue(0f, 30f, attackTimer, true) * Utils.GetLerpValue(90f, 30f, attackTimer, true);
                opacity = Utils.GetLerpValue(0f, 30f, attackTimer, true) * Utils.GetLerpValue(90f, 70f, attackTimer, true);
                duplicateFade = Utils.GetLerpValue(0f, 15f, attackTimer, true) * Utils.GetLerpValue(45f, 30f, attackTimer, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - duplicateFade;
                laggingAfterimageCount = 4;
                baseDuplicateCount = 3;
            }

            if (attackType == EmpressOfLightAttackType.EnterSecondPhase)
            {
                afterimageOffsetFactor = Utils.GetLerpValue(30f, SecondPhaseFadeoutTime, attackTimer, true) *
                    Utils.GetLerpValue(SecondPhaseFadeBackInTime, 0f, attackTimer - SecondPhaseFadeoutTime, true);
                opacity = Utils.GetLerpValue(0f, 60f, attackTimer, true) *
                    Utils.GetLerpValue(SecondPhaseFadeBackInTime, SecondPhaseFadeBackInTime - 60f, attackTimer - SecondPhaseFadeoutTime, true);
                duplicateFade = Utils.GetLerpValue(0f, 60f, attackTimer, true) *
                    Utils.GetLerpValue(SecondPhaseFadeBackInTime, SecondPhaseFadeBackInTime - 60f, attackTimer - SecondPhaseFadeoutTime, true);
                baseColor = Color.Lerp(baseColor, Color.White, afterimageOffsetFactor);
                baseColorOpacity *= 1f - duplicateFade;
                baseDuplicateCount = 4;
            }

            if (attackType is EmpressOfLightAttackType.RainbowWispForm or EmpressOfLightAttackType.InfiniteBrilliance)
            {
                float brightness = npc.Infernum().ExtraAI[0];
                if (attackType == EmpressOfLightAttackType.InfiniteBrilliance && brightness <= 0f && npc.ai[1] >= 1f)
                    brightness = 1f;

                afterimageOffsetFactor = opacity = brightness;

                if (opacity > 0f)
                    baseColorOpacity = opacity;

                baseColor = Color.Lerp(baseColor, new Color(1f, 1f, 1f, 0f), baseColorOpacity);
                baseDuplicateCount = 2;
                laggingAfterimageCount = 4;
                if (attackType == EmpressOfLightAttackType.InfiniteBrilliance)
                {
                    baseDuplicateCount = 6;
                    laggingAfterimageCount = 0;
                }
            }

            if (baseDuplicateCount + laggingAfterimageCount > 0)
            {
                for (int i = -baseDuplicateCount; i <= baseDuplicateCount + laggingAfterimageCount; i++)
                {
                    if (i == 0)
                        continue;

                    Color duplicateColor = Color.White;
                    Vector2 drawPosition = baseDrawPosition;

                    // Create cool afterimages while charging at the target.
                    if (attackType == EmpressOfLightAttackType.HorizontalCharge)
                    {
                        float hue = (i + 5f) / 10f;
                        float drawOffsetFactor = 80f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationX((Main.GlobalTimeWrappedHourly - 0.3f + i * 0.1f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationY((Main.GlobalTimeWrappedHourly - 0.8f + i * 0.3f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationZ((Main.GlobalTimeWrappedHourly + i * 0.5f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.GetLerpValue(-1f, 1f, offsetInformation.Z, true) * 150f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor * afterimageOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * attackTimer / 180f);

                        float luminanceInterpolant = Utils.GetLerpValue(90f, 0f, attackTimer, true);
                        duplicateColor = Main.hslToRgb(hue, 1f, MathHelper.Lerp(0.5f, 1f, luminanceInterpolant)) * opacity * 0.8f;
                        duplicateColor.A /= 3;
                        drawPosition += drawOffset;
                    }

                    // Handle the wisp form afterimages.
                    if (attackType == EmpressOfLightAttackType.RainbowWispForm)
                    {
                        float hue = (i + 5f) / 10f % 1f;
                        float drawOffsetFactor = 80f;
                        Vector3 offsetInformation = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationX((Main.GlobalTimeWrappedHourly * 1.3f - 0.4f + i * 0.16f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationY((Main.GlobalTimeWrappedHourly * 1.3f - 0.7f + i * 0.32f) * 0.7f * MathHelper.TwoPi) *
                            Matrix.CreateRotationZ((Main.GlobalTimeWrappedHourly * 1.3f + 0.3f + i * 0.6f) * 0.1f * MathHelper.TwoPi));
                        drawOffsetFactor += Utils.GetLerpValue(-1f, 1f, offsetInformation.Z, true) * 30f;
                        Vector2 drawOffset = new Vector2(offsetInformation.X, offsetInformation.Y) * drawOffsetFactor * afterimageOffsetFactor;
                        drawOffset = drawOffset.RotatedBy(MathHelper.TwoPi * attackTimer / 180f);

                        duplicateColor = Main.hslToRgb(hue, 1f, 0.6f) * opacity;
                        if (i > baseDuplicateCount)
                            duplicateColor = Main.hslToRgb((i - baseDuplicateCount - 1f) / laggingAfterimageCount % 1f, 1f, 0.5f) * opacity;

                        duplicateColor.A /= 12;
                        drawPosition += drawOffset;
                    }

                    // Do the transition visuals for phase 2.
                    if (attackType == EmpressOfLightAttackType.EnterSecondPhase)
                    {
                        // Fade in.
                        if (attackTimer >= SecondPhaseFadeoutTime)
                        {
                            int offsetIndex = i;
                            if (offsetIndex < 0)
                                offsetIndex++;

                            Vector2 circularOffset = ((offsetIndex + 0.5f) * MathHelper.PiOver4 + Main.GlobalTimeWrappedHourly * MathHelper.Pi * 1.333f).ToRotationVector2();
                            drawPosition += circularOffset * afterimageOffsetFactor * new Vector2(600f, 150f);
                        }

                        // Fade out and create afterimages that dissipate.
                        else
                            drawPosition += Vector2.UnitX * i * afterimageOffsetFactor * 200f;

                        duplicateColor = Color.White * opacity * baseColorOpacity * 0.8f;
                        duplicateColor.A /= 3;
                    }

                    // Create lagging afterimages.
                    if (i > baseDuplicateCount)
                    {
                        float lagBehindFactor = Utils.GetLerpValue(30f, 70f, attackTimer, true);
                        if (lagBehindFactor == 0f)
                            continue;

                        drawPosition = baseDrawPosition + npc.velocity * -3f * (i - baseDuplicateCount - 1f) * lagBehindFactor;
                        duplicateColor *= 1f - duplicateFade;
                    }

                    // Draw wings.
                    spriteBatch.Draw(wingOutlineTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);
                    spriteBatch.Draw(wingTexture, drawPosition, wingFrame, duplicateColor, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

                    // Draw tentacles in phase 2.
                    if (InPhase2(npc))
                        spriteBatch.Draw(tentacleTexture, drawPosition, tentacleFrame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw the base texture.
                    spriteBatch.Draw(texture, drawPosition, npc.frame, duplicateColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                    // Draw hands.
                    for (int j = 0; j < 2; j++)
                    {
                        if (j == leftArmDrawOrder)
                            spriteBatch.Draw(leftArmTexture, drawPosition, leftArmFrame, duplicateColor, npc.rotation, origin, npc.scale, direction, 0f);

                        if (j == rightArmDrawOrder)
                            spriteBatch.Draw(rightArmTexture, drawPosition, rightArmFrame, duplicateColor, npc.rotation, origin2, npc.scale, direction, 0f);
                    }
                }
            }

            baseColor *= baseColorOpacity;
            void DrawInstance(Vector2 drawPosition, Color color, Color? tentacleDressColorOverride = null)
            {
                // Draw wings. This involves usage of a shader to give the wing texture.
                spriteBatch.Draw(wingOutlineTexture, drawPosition, wingFrame, color, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0f);

                spriteBatch.EnterShaderRegion();

                DrawData wingData = new(wingTexture, drawPosition, wingFrame, color, npc.rotation, wingFrame.Size() / 2f, npc.scale * 2f, direction, 0);
                PrepareShader();
                GameShaders.Misc["HallowBoss"].Apply(wingData);
                wingData.Draw(spriteBatch);
                spriteBatch.ExitShaderRegion();

                float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Pi) * 0.5f + 0.5f;
                Color tentacleDressColor = Main.hslToRgb((pulse * 0.08f + 0.6f) % 1f, 1f, 0.5f);
                tentacleDressColor.A = 0;
                tentacleDressColor *= 0.6f;
                if (ShouldBeEnraged)
                {
                    tentacleDressColor = Main.OurFavoriteColor;
                    tentacleDressColor.A = 0;
                    tentacleDressColor *= 0.3f;
                }
                tentacleDressColor = tentacleDressColorOverride ?? tentacleDressColor;
                tentacleDressColor *= baseColorOpacity * npc.Opacity;

                // Draw tentacles.
                if (InPhase2(npc))
                {
                    spriteBatch.Draw(tentacleTexture, drawPosition, tentacleFrame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                        spriteBatch.Draw(tentacleTexture, drawPosition + drawOffset, tentacleFrame, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    }
                }

                // Draw the base texture.
                spriteBatch.Draw(texture, drawPosition, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

                // Draw the dress.
                if (InPhase2(npc))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = npc.rotation.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 4f + MathHelper.PiOver4) * MathHelper.Lerp(2f, 8f, pulse);
                        spriteBatch.Draw(dressGlowmaskTexture, drawPosition + drawOffset, null, tentacleDressColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
                    }
                }

                // Draw arms.
                for (int k = 0; k < 2; k++)
                {
                    if (k == leftArmDrawOrder)
                        spriteBatch.Draw(leftArmTexture, drawPosition, leftArmFrame, color, npc.rotation, origin, npc.scale, direction, 0f);

                    if (k == rightArmDrawOrder)
                        spriteBatch.Draw(rightArmTexture, drawPosition, rightArmFrame, color, npc.rotation, origin2, npc.scale, direction, 0f);
                }
            }

            if (attackType == EmpressOfLightAttackType.RainbowWispForm && npc.Infernum().ExtraAI[0] > 0f)
            {
                float wispInterpolant = npc.Infernum().ExtraAI[0];
                for (int i = 0; i < 10; i++)
                {
                    Color wispColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.6f + 0.2f + i / 10f) % 1f, 1f, 0.5f) * baseColorOpacity * 0.2f;
                    wispColor = Color.Lerp(baseColor, wispColor, wispInterpolant);
                    wispColor.A /= 6;

                    Vector2 drawOffset = (MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2() * wispInterpolant * 20f;
                    DrawInstance(baseDrawPosition + drawOffset, wispColor, wispColor);
                }

                Color baseInstanceColor = baseColor;
                baseInstanceColor.A /= 5;
                DrawInstance(baseDrawPosition, baseInstanceColor);
            }
            else
            {
                if (attackType == EmpressOfLightAttackType.InfiniteBrilliance)
                {
                    float telegraphInterpolant = npc.Infernum().ExtraAI[0];
                    if (telegraphInterpolant <= 0f && npc.ai[1] >= 1f)
                        telegraphInterpolant = 1f;

                    for (int i = 0; i < 8; i++)
                    {
                        Color wispColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.6f + 0.2f + i / 8f) % 1f, 1f, 0.8f) * baseColorOpacity * 0.2f;
                        wispColor = Color.Lerp(baseColor, wispColor, telegraphInterpolant);
                        wispColor.A = 0;

                        Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2() * telegraphInterpolant * 20f;
                        DrawInstance(baseDrawPosition + drawOffset, wispColor, wispColor);
                    }
                }

                DrawInstance(baseDrawPosition, baseColor);
            }

            // Draw telegraphs.
            if (attackType == EmpressOfLightAttackType.MesmerizingMagic)
            {
                float telegraphRotation = npc.Infernum().ExtraAI[0];
                float telegraphInterpolant = npc.Infernum().ExtraAI[1];
                float boltCount = npc.Infernum().ExtraAI[2];
                float totalHandsToShootFrom = npc.Infernum().ExtraAI[3];

                // Stop early if the telegraphs are not able to be drawn.
                if (telegraphInterpolant <= 0f)
                    return false;

                for (int i = 0; i < 2; i++)
                {
                    if (i >= totalHandsToShootFrom)
                        break;

                    int handDirection = (i == 0).ToDirectionInt();
                    float telegraphWidth = MathHelper.Lerp(0.5f, 4f, telegraphInterpolant);
                    Vector2 handOffset = new(55f, -30f);
                    Vector2 handPosition = npc.Center + handOffset * new Vector2(handDirection, 1f);

                    for (int j = 0; j < boltCount; j++)
                    {
                        Color telegraphColor = Main.hslToRgb(j / (float)boltCount, 1f, 0.5f) * (float)Math.Sqrt(telegraphInterpolant);
                        if (ShouldBeEnraged)
                            telegraphColor = Main.OurFavoriteColor;
                        telegraphColor *= 0.6f;

                        Vector2 telegraphDirection = (MathHelper.TwoPi * j / boltCount + telegraphRotation).ToRotationVector2();
                        Vector2 start = handPosition;
                        Vector2 end = start + telegraphDirection * 4500f;
                        spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    }
                }
            }

            if (attackType == EmpressOfLightAttackType.InfiniteBrilliance)
            {
                float telegraphRotation = npc.Infernum().ExtraAI[1];
                float telegraphInterpolant = npc.Infernum().ExtraAI[0];
                float telegraphSlope = (float)Math.Tan(telegraphRotation);
                float telegraphWidth = MathHelper.Lerp(2f, 6f, telegraphInterpolant);
                float brightness = (float)Math.Pow(telegraphInterpolant, 0.48);

                if (brightness <= 0f && npc.ai[1] >= 1f)
                    brightness = 1f;

                if (brightness > 0f)
                {
                    float twinkleScale = brightness;
                    Texture2D twinkleTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/LargeStar").Value;
                    Vector2 drawPosition = npc.Center - Main.screenPosition;
                    float secondaryTwinkleRotation = Main.GlobalTimeWrappedHourly * 5.13f;

                    spriteBatch.SetBlendState(BlendState.Additive);

                    for (int i = 0; i < 2; i++)
                    {
                        spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, 0f, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1f, 1.85f), SpriteEffects.None, 0f);
                        spriteBatch.Draw(twinkleTexture, drawPosition, null, Color.White, secondaryTwinkleRotation, twinkleTexture.Size() * 0.5f, twinkleScale * new Vector2(1.3f, 1f), SpriteEffects.None, 0f);
                    }
                    spriteBatch.ResetBlendState();
                }

                // Stop early if the telegraphs are not able to be drawn.
                if (telegraphInterpolant <= 0f)
                    return false;

                for (int i = 0; i < 4; i++)
                {
                    float hue = (i / 4f + Main.GlobalTimeWrappedHourly * 0.67f) % 1f;
                    Color telegraphColor = Main.hslToRgb(hue, 1f, 0.7f);
                    if (ShouldBeEnraged)
                        telegraphColor = Main.OurFavoriteColor;

                    Vector2 telegraphDirection = (MathHelper.TwoPi * i / 4f + MathHelper.PiOver4).ToRotationVector2() * new Vector2(1f, telegraphSlope);
                    Vector2 start = npc.Center + Vector2.UnitY * 8f;
                    Vector2 end = start + telegraphDirection.SafeNormalize(Vector2.UnitY) * SpinningPrismLaserbeam2.MaxLaserLength;
                    spriteBatch.DrawLineBetter(start, end, Main.hslToRgb(hue, 1f, 0.7f), telegraphWidth);
                    spriteBatch.DrawLineBetter(start, end, Color.White, telegraphWidth * 0.5f);

                    if (i % 2 == 0)
                    {
                        Vector2 aheadDirection = (MathHelper.TwoPi * (i + 1f) / 4f + MathHelper.PiOver4).ToRotationVector2() * new Vector2(1f, telegraphSlope);
                        for (int j = 0; j < 18; j++)
                        {
                            Vector2 currentTelegraphDirection = Vector2.Lerp(telegraphDirection, aheadDirection, j / 17f);
                            end = start + currentTelegraphDirection.SafeNormalize(Vector2.UnitY) * SpinningPrismLaserbeam2.MaxLaserLength;
                            spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                            spriteBatch.DrawLineBetter(start, end, Color.White, telegraphWidth * 0.5f);
                        }
                    }
                }
            }

            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = InPhase2(npc).ToInt() * frameHeight;
        }
        #endregion Drawing and Frames

        #region Misc Utilities
        public static bool InPhase2(NPC npc)
        {
            float attackTimer = npc.ai[1];
            float currentPhase = npc.ai[2];
            EmpressOfLightAttackType attackType = (EmpressOfLightAttackType)npc.ai[0];
            return currentPhase >= 1f && (attackType != EmpressOfLightAttackType.EnterSecondPhase || attackTimer >= SecondPhaseFadeoutTime);
        }

        public static bool InPhase3(NPC npc) => npc.ai[2] >= 2f;

        public static bool InPhase4(NPC npc) => npc.ai[2] >= 3f;
        #endregion
    }
}
