using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class DukeFishronBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DukeFishron;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum DukeAttackType
        {
            Charge,
            ChargeWait,
            BubbleSpit,
            BubbleSpin,
            StationaryBubbleCharge,
            SharkTornadoSummon,
            TidalWave,
            TeleportCharge,
            RazorbladeRazorstorm
        }

        public enum DukeFrameDrawingType
        {
            FinFlapping,
            IdleFins,
            OpenMouth,
            OpenMouthFinFlapping
        }
        #endregion

        #region Pattern Lists
        public static readonly DukeAttackType[] Subphase1Pattern = new DukeAttackType[]
        {
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.StationaryBubbleCharge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.StationaryBubbleCharge,
        };

        public static readonly DukeAttackType[] Subphase2Pattern = new DukeAttackType[]
        {
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.StationaryBubbleCharge,
            DukeAttackType.TidalWave,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.TidalWave,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.StationaryBubbleCharge,
        };

        public static readonly DukeAttackType[] Subphase3Pattern = new DukeAttackType[]
        {
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.RazorbladeRazorstorm,
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.Charge,
            DukeAttackType.Charge,
            DukeAttackType.TidalWave,
        };

        public static readonly DukeAttackType[] Subphase4Pattern = new DukeAttackType[]
        {
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.TeleportCharge,
            DukeAttackType.RazorbladeRazorstorm,
        };

        public static readonly Dictionary<DukeAttackType[], Func<NPC, bool>> SubphaseTable = new Dictionary<DukeAttackType[], Func<NPC, bool>>()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.65f,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax < 0.65f && npc.life / (float)npc.lifeMax >= 0.35f,
            [Subphase3Pattern] = (npc) => npc.life / (float)npc.lifeMax < 0.35f && npc.life / (float)npc.lifeMax >= 0.15f,
            [Subphase4Pattern] = (npc) => npc.life / (float)npc.lifeMax < 0.15f,
        };
        #endregion

        #region AI
        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            aquireNewTarget();

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                aquireNewTarget();
                // If no possible target was found, fly away.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y -= 0.4f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                    if (npc.timeLeft > 160)
                        npc.timeLeft = 160;
                    return false;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;

            npc.damage = npc.defDamage;
            npc.Calamity().DR = MathHelper.Lerp(0.1f, 0.37f, Utils.InverseLerp(5f, 1.6f, npc.velocity.Length(), true));
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = lifeRatio < 0.65f;
            bool inPhase3 = lifeRatio < 0.35f;
            bool inPhase4 = lifeRatio < 0.15f;
            bool inWater = npc.wet;
            ref float aiState = ref npc.Infernum().ExtraAI[5];
            ref float aiStateIndex = ref npc.ai[1];
            ref float attackTimer = ref npc.Infernum().ExtraAI[6];
            ref float frameDrawType = ref npc.ai[3];
            ref float phaseTransitionPhase = ref npc.Infernum().ExtraAI[7];
            ref float phaseTransitionTime = ref npc.Infernum().ExtraAI[8];
            ref float hasEyes01Flag = ref npc.Infernum().ExtraAI[9];
            ref float attackDelay = ref npc.Infernum().ExtraAI[10];
            ref float teleportChargeCount = ref npc.Infernum().ExtraAI[11];
            ref float eyeGlowmaskOpacity = ref npc.Infernum().ExtraAI[12];

            bool enraged = target.position.Y < 300f || target.position.Y > Main.worldSurface * 16.0 ||
                           target.position.X > 6000f && target.position.X < (Main.maxTilesX * 16 - 6000);

            if (BossRushEvent.BossRushActive)
                enraged = false;

            npc.Calamity().CurrentlyEnraged = enraged;

            Vector2 mouthPosition = (npc.rotation + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi).ToRotationVector2() * (npc.Size + Vector2.UnitY * 55f) * 0.6f + npc.Center;
            mouthPosition.Y += 12f;

            if (attackDelay < 60f)
            {
                npc.damage = 0;
                if (attackDelay == 1f)
                    npc.velocity = Vector2.UnitY * -4.4f;
                else
                    npc.velocity.Y *= 0.97f;

                attackDelay++;
                return false;
            }

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[1]++;

                DukeAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
                DukeAttackType nextAttackType = patternToUse[(int)(npc.ai[1] % patternToUse.Length)];

                // Going to the next AI state.
                npc.Infernum().ExtraAI[5] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.Infernum().ExtraAI[6] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            void aquireNewTarget(bool changeDirection = true)
            {
                npc.TargetClosest(changeDirection);
                target = Main.player[npc.target];
            }

            float getAdjustedAngle(float baseAngle, bool adjustDirection = false)
            {
                float idealAngle = baseAngle;
                if (adjustDirection)
                    npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                if (npc.spriteDirection == 1)
                    idealAngle += MathHelper.Pi;
                return idealAngle;
            }

            frameDrawType = (int)DukeFrameDrawingType.FinFlapping;

            // Phase transitions.
            if ((phaseTransitionPhase == 0f && inPhase2) || (phaseTransitionPhase == 1f && inPhase3))
            {
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                npc.velocity *= 0.98f;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.02f);

                // Sound.
                if (phaseTransitionTime == 60)
                {
                    hasEyes01Flag = 1f;
                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);
                }

                phaseTransitionTime++;
                if (phaseTransitionTime >= 95f)
                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                if (phaseTransitionPhase == 0f)
                    eyeGlowmaskOpacity = phaseTransitionTime / 120f;

                if (phaseTransitionTime >= 120f)
                {
                    phaseTransitionPhase++;
                    aiStateIndex = 0f;
                    phaseTransitionTime = 0f;
                }
                return false;
            }

            // Start the water background.
            if (inPhase3)
            {
                npc.ai[2]++;
                npc.ai[0] = 10f;
                npc.alpha = Utils.Clamp(npc.alpha, 120, 255);
            }

            switch ((DukeAttackType)(int)aiState)
            {
                case DukeAttackType.Charge:
                    int angularAimTime = 4;
                    int chargeTime = 40;
                    float chargeSpeed = 28f;
                    int decelerationTime = 14;
                    float chargeDeceleration = 0.8f;
                    if (enraged || inPhase3)
                    {
                        angularAimTime = 2;
                        chargeTime -= 10;
                        chargeSpeed *= 1.3f;
                    }
                    if (inPhase4)
                    {
                        chargeTime -= 5;
                        chargeSpeed *= 1.15f;
                    }

                    if (inWater)
                        chargeSpeed += 4f;
                    if (BossRushEvent.BossRushActive)
                    {
                        chargeTime -= 8;
                        chargeSpeed *= 1.5f;
                    }

                    if (attackTimer < angularAimTime)
                    {
                        if (attackTimer == 1f)
                            aquireNewTarget();

                        float rotationalSpeed = MathHelper.Lerp(0.06f, 0.5f, Utils.InverseLerp(0.05f, MathHelper.Pi, Math.Abs(MathHelper.WrapAngle(npc.rotation - npc.AngleTo(target.Center))), true));
                        npc.rotation = npc.rotation.AngleLerp(getAdjustedAngle(npc.AngleTo(target.Center)), 0.15f);
                    }

                    // Lunge at the player.
                    if (attackTimer == angularAimTime)
                    {
                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                        if (enraged)
                        {
                            float aimAheadFactor = 20f * Utils.InverseLerp(80f, 360f, npc.Distance(target.Center), true);
                            npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * aimAheadFactor) * chargeSpeed;
                        }
                        else
                            npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                        npc.rotation = getAdjustedAngle(npc.velocity.ToRotation());
                        npc.netUpdate = true;
                    }

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;
                    if (attackTimer >= angularAimTime && attackTimer < angularAimTime + chargeTime)
                        GenerateParticles(npc);

                    // And decelerate over time.
                    if (attackTimer >= angularAimTime + chargeTime &&
                        attackTimer <= angularAimTime + chargeTime + decelerationTime)
                    {
                        npc.rotation = npc.rotation.AngleLerp(getAdjustedAngle(npc.AngleTo(target.Center), true), 0.32f);
                        npc.velocity *= chargeDeceleration;
                    }

                    if (attackTimer >= angularAimTime + chargeTime + decelerationTime)
                        goToNextAIState();
                    break;

                case DukeAttackType.ChargeWait:
                    npc.damage = 0;
                    int waitDelay = 30;
                    ref float horizontalHoverOffset = ref npc.Infernum().ExtraAI[0];

                    // Hover near the target.
                    if (horizontalHoverOffset == 0f)
                        horizontalHoverOffset = Math.Sign(target.Center.X - npc.Center.X) * 500f;
                    Vector2 hoverDestination = target.Center + new Vector2(horizontalHoverOffset, -350f) - npc.velocity;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 22f, 1.05f);

                    // Look at the target.
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.rotation = npc.AngleTo(target.Center);

                    if (npc.spriteDirection == 1)
                        npc.rotation += MathHelper.Pi;

                    // Handle frames.
                    frameDrawType = (int)DukeFrameDrawingType.FinFlapping;
                    npc.frameCounter++;

                    if (attackTimer >= waitDelay)
                        goToNextAIState();
                    break;

                case DukeAttackType.BubbleSpit:
                    int bubbleCount = 24;
                    int bubbleShootRate = 3;
                    float minBubbleSpeed = 8f;
                    float maxBubbleSpeed = 11f;

                    if (enraged)
                    {
                        bubbleShootRate = 2;
                        minBubbleSpeed = 11f;
                        maxBubbleSpeed = 15f;
                    }

                    if (BossRushEvent.BossRushActive)
                    {
                        minBubbleSpeed *= 1.5f;
                        maxBubbleSpeed *= 1.5f;
                    }

                    ref float hoverDirection = ref npc.Infernum().ExtraAI[0];

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

                    // Play sound and assign hover direction.
                    if (hoverDirection == 0f)
                    {
                        hoverDirection = Math.Sign((npc.Center - target.Center).X);
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);
                    }

                    Vector2 hoverVelocity = npc.SafeDirectionTo(target.Center + new Vector2(hoverDirection * 400f, -320f) - npc.velocity) * 8f;
                    npc.SimpleFlyMovement(hoverVelocity, 0.42f);
                    npc.rotation = npc.rotation.AngleLerp(getAdjustedAngle(npc.AngleTo(target.Center), true), 0.32f);

                    // Belch bubbles.
                    if (attackTimer % bubbleShootRate == 0)
                    {
                        Main.PlaySound(SoundID.NPCKilled, (int)npc.Center.X, (int)npc.Center.Y, 19, 1f, 0f);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int bubble = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, NPCID.DetonatingBubble);
                            Main.npc[bubble].velocity = Main.npc[bubble].SafeDirectionTo(target.Center).RotatedByRandom(0.1f) * Main.rand.NextFloat(minBubbleSpeed, maxBubbleSpeed);
                        }
                    }

                    if (attackTimer >= bubbleShootRate * bubbleCount)
                        goToNextAIState();
                    break;
                case DukeAttackType.BubbleSpin:
                    int spinTime = 120;
                    float spinSpeed = 16f;
                    float moveToTargetSpeed = 6f;
                    float totalSpins = 3f;
                    bubbleShootRate = 10;

                    if (enraged)
                    {
                        spinSpeed = 23f;
                        moveToTargetSpeed = 9f;
                        bubbleShootRate = 5;
                    }

                    if (BossRushEvent.BossRushActive)
                        spinSpeed *= 1.45f;

                    spinSpeed *= totalSpins * 0.5f;

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

                    if (attackTimer == 1f)
                    {
                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                        npc.velocity = npc.SafeDirectionTo(target.Center) * spinSpeed;
                        npc.rotation = getAdjustedAngle(npc.velocity.ToRotation());
                        npc.netUpdate = true;

                        // Roar.
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);
                    }
                    else if (attackTimer > 1f)
                    {
                        float rotationalSpeed = MathHelper.TwoPi * totalSpins / spinTime * npc.spriteDirection;
                        npc.rotation += rotationalSpeed;
                        npc.velocity = npc.velocity.RotatedBy(rotationalSpeed);

                        if (!npc.WithinRange(target.Center, 200f))
                            npc.Center += npc.SafeDirectionTo(target.Center) * moveToTargetSpeed;

                        if (attackTimer % bubbleShootRate == bubbleShootRate - 1)
                        {
                            Main.PlaySound(SoundID.NPCKilled, (int)npc.Center.X, (int)npc.Center.Y, 19, 1f, 0f);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int bubble = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, NPCID.DetonatingBubble);
                                Main.npc[bubble].velocity = npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection).RotatedByRandom(0.18f) * Main.rand.NextFloat(11f, 15f);
                                Main.npc[bubble].Center -= Main.npc[bubble].velocity * 3f;
                            }
                        }
                    }

                    if (attackTimer >= spinTime)
                        goToNextAIState();
                    break;
                case DukeAttackType.StationaryBubbleCharge:
                    bubbleCount = 10;
                    bubbleShootRate = 6;
                    hoverDirection = ref npc.Infernum().ExtraAI[0];
                    ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

                    if (enraged)
                    {
                        bubbleCount = 15;
                        bubbleShootRate = 4;
                    }

                    if (BossRushEvent.BossRushActive)
                        bubbleShootRate = 15;

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

                    // Fly a bit above the target.
                    if (attackSubstate == 0f)
                    {
                        attackTimer = 0f;
                        hoverDirection = ((npc.Center - target.Center).X > 0).ToDirectionInt();

                        Vector2 destination = target.Center + new Vector2(hoverDirection * 555f, -150f);
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * (16f + target.velocity.Length() * 1.2f), 0.5f + target.velocity.Length() * 0.06f);
                        npc.rotation = npc.rotation.AngleLerp(getAdjustedAngle(npc.AngleTo(target.Center), true), 0.32f);

                        if (npc.WithinRange(destination, 21f))
                        {
                            attackSubstate = 1f;
                            npc.Center = destination;
                            npc.velocity = Vector2.UnitX * (17f + target.velocity.Length()) * -hoverDirection;
                            npc.spriteDirection = (int)hoverDirection;
                            npc.rotation = 0f;
                            npc.netUpdate = true;

                            // Roar.
                            Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);
                        }
                    }

                    // And dash while releasing bubbles.
                    else if (attackSubstate == 1f)
                    {
                        Main.PlaySound(SoundID.NPCKilled, (int)npc.Center.X, (int)npc.Center.Y, 19, 1f, 0f);

                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % bubbleShootRate == bubbleShootRate - 1)
                        {
                            int bubble = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<RedirectingBubble>());
                            Main.npc[bubble].Center += npc.velocity * 1.5f;
                            Main.npc[bubble].velocity = Vector2.UnitY * ((int)(attackTimer / bubbleShootRate) % 2 == 0).ToDirectionInt() * RedirectingBubble.InitialSpeed;
                            Main.npc[bubble].target = npc.target;
                        }
                    }

                    if (attackTimer >= bubbleCount * bubbleShootRate - 20f)
                        npc.velocity *= 0.965f;

                    if (attackTimer >= bubbleCount * bubbleShootRate)
                        goToNextAIState();
                    break;

                case DukeAttackType.SharkTornadoSummon:
                    int slowdownTime = 60;
                    int sharkWaves = 6;
                    int sharkSummonRate = 10;
                    ref float summonOutwardness = ref npc.Infernum().ExtraAI[0];

                    if (BossRushEvent.BossRushActive)
                        sharkSummonRate = 8;

                    if (attackTimer < slowdownTime)
                        npc.velocity *= 0.95f;

                    npc.rotation = npc.rotation.AngleLerp(getAdjustedAngle(npc.AngleTo(target.Center), true), 0.4f);

                    if (attackTimer >= slowdownTime - 8f && attackTimer <= slowdownTime + 10f)
                        frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                    if (attackTimer == slowdownTime)
                    {
                        // Roar.
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);
                        List<int> xSpawnPositions = new List<int>()
                        {
                            (int)(target.Center.X - (enraged ? 500f : 600f)) / 16,
                            (int)(target.Center.X + (enraged ? 500f : 600f)) / 16
                        };

                        // Summon tornadoes on the ground/water.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int y = Utils.Clamp((int)target.Center.Y / 16 - 50, 20, Main.maxTilesY - 20);
                            foreach (int x in xSpawnPositions)
                            {
                                WorldUtils.Find(new Point(x, y), Searches.Chain(new Searches.Down(Main.maxTilesY - 10), new CustomTileConditions.IsWaterOrSolid()), out Point result);
                                Vector2 spawnPosition = result.ToWorldCoordinates();
                                Vector2 tornadoVelocity = Vector2.UnitX * (target.Center.X > spawnPosition.X).ToDirectionInt() * 4f;
                                int tornado = Utilities.NewProjectileBetter(spawnPosition, tornadoVelocity, ModContent.ProjectileType<Tornado>(), 150, 0f);
                                Main.projectile[tornado].Bottom = spawnPosition;
                            }
                        }
                    }

                    // Summon sharks in the ocean.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > slowdownTime && attackTimer % sharkSummonRate == 0)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            int x = (int)(npc.Center.X + (summonOutwardness + 150f) * i) / 16;
                            int y = Utils.Clamp((int)target.Center.Y / 16 - 10, 10, Main.maxTilesY - 10);
                            WorldUtils.Find(new Point(x, y), Searches.Chain(new Searches.Down(150), new CustomTileConditions.IsWater()), out Point result);
                            Vector2 spawnPosition = result.ToWorldCoordinates();
                            int summoner = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<SharkSummoner>(), 0, 0f);
                            float flySpeed = Math.Abs(npc.Center.Y - spawnPosition.Y) * 0.0125f + 5f;
                            flySpeed = MathHelper.Min(flySpeed, 27f);
                            if (Main.projectile.IndexInRange(summoner))
                            {
                                Main.projectile[summoner].direction = i;
                                Main.projectile[summoner].ai[1] = flySpeed;
                            }
                        }

                        summonOutwardness += 200f;
                    }

                    if (attackTimer >= slowdownTime + sharkWaves * sharkSummonRate)
                        goToNextAIState();
                    break;

                case DukeAttackType.TidalWave:
                    int redirectTime = inPhase3 ? 32 : 45;
                    float lungeSpeed = enraged ? 30f : 22f;
                    float waveSpeed = enraged ? 20f : 13.5f;
                    if (inPhase3)
                    {
                        lungeSpeed *= 1.4f;
                        waveSpeed *= 1.6f;
                    }

                    if (BossRushEvent.BossRushActive)
                    {
                        lungeSpeed *= 2f;
                        waveSpeed *= 1.35f;
                    }

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                    int lungeMaxTime = 180;
                    if (attackTimer < redirectTime)
                    {
                        Vector2 destination = target.Center - Vector2.UnitY.RotatedBy(target.velocity.X / 20f * MathHelper.ToRadians(26f)) * 430f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 15f, 0.5f);
                        npc.rotation = getAdjustedAngle(npc.AngleTo(target.Center), true);
                    }
                    if (attackTimer == redirectTime)
                    {
                        // Roar.
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);

                        npc.velocity = npc.SafeDirectionTo(target.Center) * lungeSpeed;
                        npc.netUpdate = true;
                    }

                    if (attackTimer > redirectTime)
                    {
                        GenerateParticles(npc);
                        if (Collision.SolidCollision(npc.position, npc.width, npc.width) ||
                            Collision.WetCollision(npc.position, npc.width, npc.width) ||
                            attackTimer >= redirectTime + lungeMaxTime)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = -1; i <= 1; i += 2)
                                {
                                    int wave = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * waveSpeed * i, ModContent.ProjectileType<TidalWave>(), 150, 0f);
                                    Main.projectile[wave].Bottom = npc.Center + Vector2.UnitY * 700f;
                                }
                            }

                            // Very heavily disturb water.
                            if (Main.netMode != NetmodeID.Server)
                            {
                                WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                                float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTime * 20f);
                                Vector2 ripplePos = npc.Center + npc.velocity * 7f;
                                Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
                                ripple.QueueRipple(ripplePos, waveData, Vector2.One * 860f, RippleShape.Circle, npc.rotation);
                            }
                            npc.velocity *= -0.5f;
                            goToNextAIState();
                        }
                    }
                    break;
                case DukeAttackType.RazorbladeRazorstorm:
                    int hoverTime = 60;
                    float initialChargeSpeed = enraged ? 34f : 30f;
                    int chargeCount = 5;
                    int typhoonBurstRate = enraged ? 20 : 30;
                    int typhoonCount = enraged ? 14 : 8;
                    float typhoonBurstSpeed = enraged ? 14f : 9f;

                    if (inPhase4)
                    {
                        initialChargeSpeed += 2.5f;
                        typhoonBurstRate -= 5;
                        typhoonCount += 5;
                    }

                    if (BossRushEvent.BossRushActive)
                        initialChargeSpeed *= 1.2f;

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                    if (attackTimer < hoverTime)
                    {
                        Vector2 destination = target.Center + new Vector2(500f, -1000f);
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 23f, 0.7f);
                        npc.rotation = getAdjustedAngle(npc.AngleTo(target.Center), true);
                    }

                    // Roar, make the initial charge, and summon tornado borders.
                    if (attackTimer == hoverTime)
                    {
                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                        npc.velocity = -Vector2.UnitX.RotatedBy(MathHelper.Pi * -0.11f) * initialChargeSpeed;

                        // Roar.
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);

                        // Summon tornadoes on the ground/water.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            List<int> xSpawnPositions = new List<int>()
                            {
                                (int)(target.Center.X - (enraged ? 600f : 750f)) / 16,
                                (int)(target.Center.X + (enraged ? 600f : 750f)) / 16
                            };

                            int y = Utils.Clamp((int)target.Center.Y / 16 - 50, 20, Main.maxTilesY - 20);
                            foreach (int x in xSpawnPositions)
                            {
                                WorldUtils.Find(new Point(x, y), Searches.Chain(new Searches.Down(Main.maxTilesY - 10), new CustomTileConditions.IsWaterOrSolid()), out Point result);
                                Vector2 spawnPosition = result.ToWorldCoordinates();
                                int tornado = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<Tornado>(), 200, 0f);
                                Main.projectile[tornado].Bottom = spawnPosition;
                            }
                        }

                        npc.rotation = getAdjustedAngle(npc.velocity.ToRotation());
                        npc.netUpdate = true;
                    }
                    if (attackTimer > hoverTime)
                    {
                        // Reflected charge.
                        if (attackTimer % 30f == 29f)
                        {
                            npc.spriteDirection *= -1;
                            npc.velocity = Vector2.Reflect(npc.velocity, Vector2.UnitX);
                            npc.rotation = getAdjustedAngle(npc.velocity.ToRotation());
                            npc.netUpdate = true;
                        }

                        if (attackTimer % typhoonBurstRate == typhoonBurstRate - 1f)
                        {
                            Main.PlaySound(SoundID.Item84, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < typhoonCount; i++)
                                {
                                    float offsetAngle = MathHelper.TwoPi * i / typhoonCount;
                                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * typhoonBurstSpeed;
                                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<TyphoonBlade>(), 105, 0f);
                                }
                            }
                        }
                    }

                    if (attackTimer >= hoverTime + chargeCount * 30f + 45f)
                        goToNextAIState();
                    break;
                case DukeAttackType.TeleportCharge:
                    chargeSpeed = enraged ? 35f : 31f;
                    if (inPhase4)
                        chargeSpeed += 3f;

                    if (BossRushEvent.BossRushActive)
                        chargeSpeed *= 1.15f;

                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                    // Fadeout effects, flying, and damage disabling.
                    if (attackTimer < 45f)
                    {
                        npc.damage = 0;

                        hoverDirection = ref npc.Infernum().ExtraAI[0];
                        if (hoverDirection == 0f)
                            hoverDirection = Math.Sign((npc.Center - target.Center).X);
                        hoverDestination = target.Center + new Vector2(hoverDirection * 400f, -200f);
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 1.2f);
                        npc.alpha += 25;
                    }

                    // Charge.
                    if (attackTimer == 45f)
                    {
                        hoverDirection = ref npc.Infernum().ExtraAI[0];

                        // Roar.
                        Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 20, 1f, 0f);

                        npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                        if (enraged)
                        {
                            float aimAheadFactor = 20f * Utils.InverseLerp(80f, 360f, npc.Distance(target.Center), true);
                            npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * aimAheadFactor) * chargeSpeed;
                        }
                        else
                            npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                        npc.rotation = getAdjustedAngle(npc.velocity.ToRotation());
                        npc.netUpdate = true;
                    }

                    if (attackTimer > 45f)
                        npc.alpha = Utils.Clamp(npc.alpha - 45, 0, 255);

                    // Deceleration.
                    if (npc.alpha > 0 && attackTimer > 85f)
                    {
                        npc.rotation = npc.rotation.AngleTowards(getAdjustedAngle(npc.AngleTo(target.Center), true), 0.15f);
                        npc.velocity *= 0.95f;
                    }

                    npc.alpha = Utils.Clamp(npc.alpha, 0, 255);

                    if (attackTimer >= 100f)
                    {
                        teleportChargeCount++;

                        if (teleportChargeCount > 3f)
                            goToNextAIState();
                        else
                            attackTimer = 44f;
                    }
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void GenerateParticles(NPC npc)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Vector2 spawnPosition = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(i * MathHelper.Pi / 7f) + npc.Center;
                Vector2 dustVelocity = (Main.rand.NextFloat(MathHelper.Pi) - MathHelper.PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                int water = Dust.NewDust(spawnPosition + dustVelocity, 0, 0, 172, dustVelocity.X * 2f, dustVelocity.Y * 2f, 100, default, 1.4f);
                Main.dust[water].noGravity = true;
                Main.dust[water].noLight = true;
                Main.dust[water].velocity *= 0.25f;
                Main.dust[water].velocity -= npc.velocity * 0.6f;
            }
        }
        #endregion

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 202;
            DukeFrameDrawingType frameDrawType = (DukeFrameDrawingType)(int)npc.ai[3];
            switch (frameDrawType)
            {
                case DukeFrameDrawingType.FinFlapping:
                    int frame = (int)(npc.frameCounter / 7) % 6;
                    npc.frame.X = 0;
                    npc.frame.Y = frame * frameHeight;
                    break;
                case DukeFrameDrawingType.IdleFins:
                    npc.frame.X = 0;
                    npc.frame.Y = 0;
                    break;
                case DukeFrameDrawingType.OpenMouth:
                    npc.frame.X = 0;
                    npc.frame.Y = 7 * frameHeight;
                    break;
                case DukeFrameDrawingType.OpenMouthFinFlapping:
                    frame = (int)(npc.frameCounter / 7) % 4 + 7;
                    npc.frame.X = 202;
                    npc.frame.Y = (frame - 7) * frameHeight;
                    break;
            }
            npc.frameCounter++;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            bool hasEyes = npc.Infernum().ExtraAI[9] == 1f || npc.Infernum().ExtraAI[12] > 0f;
            Texture2D eyeTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DukeFishron/DukeFishronGlowmask");
            Texture2D dukeTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DukeFishron/DukeFishronResprite");
            Vector2 origin = npc.frame.Size() * 0.5f;
            void drawOldDukeInstance(Color color, Vector2 drawPosition, int direction)
            {
                SpriteEffects spriteEffects = direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                if (npc.life / (float)npc.lifeMax < 0.65f)
                    color = Color.Lerp(color, Color.Blue, 0.1f);

                color = npc.GetAlpha(color);
                spriteBatch.Draw(dukeTexture, drawPosition - Main.screenPosition, npc.frame, color, npc.rotation, origin, npc.scale, spriteEffects, 0f);

                if (hasEyes)
                {
                    Color eyeColor = Color.Lerp(Color.White, Color.Yellow, 0.5f) * npc.Infernum().ExtraAI[12];
                    spriteBatch.Draw(eyeTexture, drawPosition - Main.screenPosition, npc.frame, eyeColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            drawOldDukeInstance(lightColor, npc.Center, npc.spriteDirection);
            return false;
        }
        #endregion
    }
}
