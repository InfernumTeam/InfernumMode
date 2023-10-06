using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Common.Worldgen;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DukeFishron
{
    public class DukeFishronBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DukeFishron;

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
            ChargeTeleport,
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
        public static DukeAttackType[] Subphase1Pattern => new DukeAttackType[]
        {
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.StationaryBubbleCharge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.StationaryBubbleCharge,
        };

        public static DukeAttackType[] Subphase2Pattern => new DukeAttackType[]
        {
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpin,
            DukeAttackType.BubbleSpit,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.StationaryBubbleCharge,
            DukeAttackType.TidalWave,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.BubbleSpin,
            DukeAttackType.TidalWave,
            DukeAttackType.SharkTornadoSummon,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.ChargeWait,
            DukeAttackType.Charge,
            DukeAttackType.StationaryBubbleCharge,
            DukeAttackType.BubbleSpin,
        };

        public static DukeAttackType[] Subphase3Pattern =>
            DoCharge(3, true).
            Fuse(DukeAttackType.RazorbladeRazorstorm, DukeAttackType.BubbleSpin).
            Fuse(DoCharge(5, true)).
            Fuse(DukeAttackType.SharkTornadoSummon).
            Fuse(DoCharge(3, true)).
            Fuse(DukeAttackType.SharkTornadoSummon, DukeAttackType.BubbleSpit).
            ToArray();

        public static DukeAttackType[] Subphase4Pattern =>
            DoCharge(9, true).
            Fuse(DukeAttackType.TidalWave, DukeAttackType.BubbleSpin).
            Fuse(DoCharge(9, true)).
            Fuse(DukeAttackType.RazorbladeRazorstorm, DukeAttackType.BubbleSpin).
            ToArray();

        public static DukeAttackType[] DoCharge(int chargeCount, bool teleportAtStart = false)
        {
            List<DukeAttackType> result = new();

            result.AddWithCondition(DukeAttackType.ChargeTeleport, teleportAtStart);
            for (int i = 0; i < chargeCount; i++)
            {
                result.Add(DukeAttackType.ChargeWait);
                result.Add(DukeAttackType.Charge);
            }
            return result.ToArray();
        }

        public static Dictionary<DukeAttackType[], Func<NPC, bool>> SubphaseTable => new()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > Phase2LifeRatio,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax is < Phase2LifeRatio and >= Phase3LifeRatio,
            [Subphase3Pattern] = (npc) => npc.life / (float)npc.lifeMax is < Phase3LifeRatio and >= Phase4LifeRatio,
            [Subphase4Pattern] = (npc) => npc.life / (float)npc.lifeMax < Phase4LifeRatio,
        };

        public const float Phase2LifeRatio = 0.7f;

        public const float Phase3LifeRatio = 0.4f;

        public const float Phase4LifeRatio = 0.2f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };
        #endregion

        #region AI

        public static int ChargeTyphoonDamage => 190;

        public static int SmallWaveDamage => 190;

        public static int TornadoDamage => 250;

        public static int TidalWaveDamage => 275;

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                target = AcquireNewTarget(npc);

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

            npc.dontTakeDamage = false;
            npc.damage = npc.defDamage;
            npc.Calamity().DR = Lerp(0.1f, 0.37f, Utils.GetLerpValue(5f, 1.6f, npc.velocity.Length(), true));
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = lifeRatio < Phase2LifeRatio;
            bool inPhase3 = lifeRatio < Phase3LifeRatio;
            bool inPhase4 = lifeRatio < Phase4LifeRatio;
            ref float attackState = ref npc.Infernum().ExtraAI[5];
            ref float aiStateIndex = ref npc.ai[1];
            ref float attackTimer = ref npc.Infernum().ExtraAI[6];
            ref float frameDrawType = ref npc.ai[3];
            ref float phaseTransitionPhase = ref npc.Infernum().ExtraAI[7];
            ref float phaseTransitionTime = ref npc.Infernum().ExtraAI[8];
            ref float hasEyes01Flag = ref npc.Infernum().ExtraAI[9];
            ref float attackDelay = ref npc.Infernum().ExtraAI[10];
            ref float eyeGlowmaskOpacity = ref npc.Infernum().ExtraAI[11];
            ref float hasEnteredPhase4 = ref npc.Infernum().ExtraAI[12];

            bool enraged = target.position.Y < 300f || target.position.Y > Main.worldSurface * 16.0 ||
                           target.position.X > 6000f && target.position.X < Main.maxTilesX * 16 - 6000;

            if (BossRushEvent.BossRushActive)
                enraged = false;

            npc.Calamity().CurrentlyEnraged = enraged;

            Vector2 mouthPosition = (npc.rotation + (npc.spriteDirection == 1).ToInt() * Pi).ToRotationVector2() * (npc.Size + Vector2.UnitY * 55f) * 0.6f + npc.Center;
            mouthPosition.Y += 24f;

            if (attackDelay < 60f)
            {
                npc.damage = 0;
                if (attackDelay == 1f)
                    npc.velocity = Vector2.UnitY * -4.4f;
                else
                    npc.velocity.Y *= 0.95f;

                // Roar in the middle of animation.
                if (attackDelay == 30f)
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                if (attackDelay >= 30f)
                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                attackDelay++;
                attackState = (int)DukeAttackType.ChargeWait;
                return false;
            }

            frameDrawType = (int)DukeFrameDrawingType.FinFlapping;

            // Phase transitions.
            if (phaseTransitionPhase == 0f && inPhase2 || phaseTransitionPhase == 1f && inPhase3)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                npc.velocity *= 0.96f;
                npc.velocity.Y = Lerp(npc.velocity.Y, 0f, 0.04f);

                // Roar in the middle of animation.
                if (phaseTransitionTime == 75f)
                {
                    hasEyes01Flag = 1f;
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
                }

                phaseTransitionTime++;
                if (phaseTransitionTime >= 75f)
                    frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

                if (phaseTransitionPhase == 0f)
                    eyeGlowmaskOpacity = phaseTransitionTime / 120f;

                if (phaseTransitionTime >= 120f)
                {
                    phaseTransitionPhase++;
                    aiStateIndex = -1f;
                    phaseTransitionTime = 0f;
                }
                return false;
            }

            if (hasEnteredPhase4 == 0f && inPhase4)
            {
                aiStateIndex = -1f;
                SelectNextAttack(npc);
                hasEnteredPhase4 = 1f;
                npc.netUpdate = true;

                // Clear leftover projectiles.
                for (int i = 0; i < 3; i++)
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ChargeTyphoon>(), ModContent.ProjectileType<SmallWave>(), ModContent.ProjectileType<TidalWave>(), ModContent.ProjectileType<Tornado>(), ModContent.ProjectileType<TyphoonBlade>());

                return false;
            }

            // Reset the eye glowmask opacity to be fullbright after it has initially appeared.
            if (phaseTransitionPhase >= 1f)
                eyeGlowmaskOpacity = 1f;

            // Start the water background in phase 3.
            if (inPhase3)
            {
                npc.ai[2]++;
                npc.ai[0] = 10f;
                npc.alpha = Utils.Clamp(npc.alpha, 120, 255);
            }

            // Stay near the target.
            if (!npc.WithinRange(target.Center, 4000f))
                npc.Center = npc.Center.MoveTowards(target.Center, 12.5f);

            switch ((DukeAttackType)(int)attackState)
            {
                case DukeAttackType.Charge:
                    DoBehavior_Charge(npc, target, ref attackTimer, ref frameDrawType, inPhase2, inPhase3, inPhase4, enraged);
                    break;
                case DukeAttackType.ChargeWait:
                    DoBehavior_ChargeWait(npc, target, ref attackTimer, ref frameDrawType);
                    break;
                case DukeAttackType.BubbleSpit:
                    DoBehavior_BubbleSpit(npc, target, mouthPosition, ref attackTimer, ref frameDrawType, inPhase3, inPhase4, enraged);
                    break;
                case DukeAttackType.BubbleSpin:
                    DoBehavior_BubbleSpin(npc, target, mouthPosition, ref attackTimer, ref frameDrawType, inPhase3, inPhase4, enraged);
                    break;
                case DukeAttackType.StationaryBubbleCharge:
                    DoBehavior_StationaryBubbleCharge(npc, target, mouthPosition, ref attackTimer, ref frameDrawType, enraged);
                    break;
                case DukeAttackType.SharkTornadoSummon:
                    DoBehavior_SharkTornadoSummon(npc, target, ref attackTimer, ref frameDrawType, enraged);
                    break;
                case DukeAttackType.TidalWave:
                    DoBehavior_TidalWave(npc, target, ref attackTimer, ref frameDrawType, inPhase3, enraged);
                    break;
                case DukeAttackType.RazorbladeRazorstorm:
                    DoBehavior_RazorbladeRazorstorm(npc, target, ref attackTimer, ref frameDrawType, inPhase4, enraged);
                    break;
                case DukeAttackType.ChargeTeleport:
                    DoBehavior_ChargeTeleport(npc, target, ref attackTimer, ref frameDrawType, ref eyeGlowmaskOpacity, inPhase4, enraged);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[1]++;

            DukeAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            DukeAttackType nextAttackType = patternToUse[(int)(npc.ai[1] % patternToUse.Length)];

            // Go to the next attack state.
            npc.Infernum().ExtraAI[5] = (int)nextAttackType;

            // Reset the attack timer.
            npc.Infernum().ExtraAI[6] = 0f;

            // Reset misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }

        public static Player AcquireNewTarget(NPC npc, bool changeDirection = true)
        {
            npc.TargetClosest(changeDirection);
            return Main.player[npc.target];
        }

        public static float GetAdjustedRotation(NPC npc, Player target, float baseAngle, bool adjustDirection = false)
        {
            float idealAngle = baseAngle;
            if (adjustDirection)
                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

            if (npc.spriteDirection == 1)
                idealAngle += Pi;
            return idealAngle;
        }

        public static void DoBehavior_Charge(NPC npc, Player target, ref float attackTimer, ref float frameDrawType, bool inPhase2, bool inPhase3, bool inPhase4, bool enraged)
        {
            int angularAimTime = 4;
            int chargeTime = 43;
            int decelerationTime = 6;
            float chargeSpeed = 33f;
            float chargeDeceleration = 0.6f;
            if (inPhase2)
            {
                chargeTime -= 5;
                decelerationTime -= 3;
            }
            if (enraged || inPhase3)
            {
                angularAimTime = 2;
                chargeTime -= 8;
                chargeSpeed += 7.5f;
            }
            if (inPhase4)
            {
                chargeTime -= 4;
                chargeSpeed += 7.5f;
            }

            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 6;
                chargeSpeed *= 1.3f;
            }

            if (attackTimer < angularAimTime)
            {
                if (attackTimer == 1f)
                    AcquireNewTarget(npc);

                npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center)), 0.15f);
            }

            // Charge at the target.
            if (attackTimer == angularAimTime)
            {
                float predictivenessFactor = 0f;

                if (enraged)
                    predictivenessFactor = Utils.GetLerpValue(80f, 360f, npc.Distance(target.Center), true) * 20f;

                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor) * chargeSpeed;
                npc.rotation = GetAdjustedRotation(npc, target, npc.velocity.ToRotation());
                npc.netUpdate = true;

                // Release typhoons in phase 3.
                if (inPhase3 && !npc.WithinRange(target.Center, 250f))
                {
                    SoundEngine.PlaySound(SoundID.Item45, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 typhoonSpeed = -npc.velocity * 0.2f;
                        Utilities.NewProjectileBetter(npc.Center, typhoonSpeed, ModContent.ProjectileType<ChargeTyphoon>(), ChargeTyphoonDamage, 0f);
                    }
                }
            }

            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            // Emit particles while charging.
            if (attackTimer >= angularAimTime && attackTimer < angularAimTime + chargeTime)
                GenerateParticles(npc);

            // Decelerate over time.
            if (attackTimer >= angularAimTime + chargeTime &&
                attackTimer <= angularAimTime + chargeTime + decelerationTime)
            {
                npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true), 0.32f);
                npc.velocity *= chargeDeceleration;
            }

            if (attackTimer >= angularAimTime + chargeTime + decelerationTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ChargeWait(NPC npc, Player target, ref float attackTimer, ref float frameDrawType)
        {
            // Disable contact damage while redirecting.
            npc.damage = 0;

            int waitDelay = 22;
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(target.Center.X - npc.Center.X) * -500f, -350f) - npc.velocity;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 2f);

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);

            if (npc.spriteDirection == 1)
                npc.rotation += Pi;

            // Handle frames.
            frameDrawType = (int)DukeFrameDrawingType.FinFlapping;
            if (attackTimer >= waitDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BubbleSpit(NPC npc, Player target, Vector2 mouthPosition, ref float attackTimer, ref float frameDrawType, bool inPhase3, bool inPhase4, bool enraged)
        {
            int bubbleCount = 24;
            int bubbleShootRate = 3;
            float minBubbleSpeed = 8f;
            float maxBubbleSpeed = 11f;
            ref float hoverDirection = ref npc.Infernum().ExtraAI[0];

            if (inPhase3)
            {
                minBubbleSpeed += 2f;
                maxBubbleSpeed += 2f;
                bubbleCount += 4;
            }
            if (inPhase4)
            {
                minBubbleSpeed += 2f;
                maxBubbleSpeed += 2f;
                bubbleCount += 6;
            }

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

            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            // Play sound and define an initial hover direction.
            if (hoverDirection == 0f)
            {
                hoverDirection = Math.Sign((npc.Center - target.Center).X);
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
            }

            Vector2 hoverVelocity = npc.SafeDirectionTo(target.Center + new Vector2(hoverDirection * 400f, -320f) - npc.velocity) * 8f;
            npc.SimpleFlyMovement(hoverVelocity, 0.42f);
            npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true), 0.32f);

            // Belch bubbles.
            if (attackTimer % bubbleShootRate == 0f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bubble = NPC.NewNPC(npc.GetSource_FromAI(), (int)mouthPosition.X, (int)mouthPosition.Y, NPCID.DetonatingBubble);
                    Main.npc[bubble].velocity = Main.npc[bubble].SafeDirectionTo(target.Center).RotatedByRandom(0.1f) * Main.rand.NextFloat(minBubbleSpeed, maxBubbleSpeed);
                }
            }

            if (attackTimer >= bubbleShootRate * bubbleCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BubbleSpin(NPC npc, Player target, Vector2 mouthPosition, ref float attackTimer, ref float frameDrawType, bool inPhase3, bool inPhase4, bool enraged)
        {
            int bubbleReleaseRate = 5;
            int hoverRedirectTime = 120;
            int spinTime = 250;
            float chargeSpeed = 21f;
            ref float hoverDirection = ref npc.Infernum().ExtraAI[0];

            if (inPhase3)
                chargeSpeed += 2.5f;

            if (inPhase4)
            {
                bubbleReleaseRate--;
                chargeSpeed += 4f;
            }

            if (enraged)
            {
                bubbleReleaseRate -= 2;
                chargeSpeed *= 1.35f;
            }

            if (BossRushEvent.BossRushActive)
            {
                bubbleReleaseRate -= 1;
                spinTime -= 25;
                chargeSpeed *= 1.45f;
            }

            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            // Play sound and define an initial hover direction.
            if (hoverDirection == 0f)
            {
                hoverDirection = Math.Sign((npc.Center - target.Center).X);
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
            }

            // Hover into position.
            if (attackTimer < hoverRedirectTime)
            {
                // Disable contact damage while redirecting, to prevent cheap hits.
                npc.damage = 0;

                // Define rotation.
                float slowdownFactor = Utils.GetLerpValue(hoverRedirectTime - 45f, hoverRedirectTime, attackTimer, true);
                float idealRotation = npc.AngleTo(target.Center).AngleLerp(GetAdjustedRotation(npc, target, 0f), slowdownFactor);
                npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, idealRotation, true), 0.25f);

                Vector2 hoverDestination = target.Center + new Vector2(hoverDirection * 720f, -600f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 4f);
                npc.velocity = (npc.velocity * 24f + npc.SafeDirectionTo(hoverDestination) * (1f - slowdownFactor) * 30f) / 25f;
            }

            // Charge.
            if (attackTimer == hoverRedirectTime)
            {
                npc.velocity = Vector2.UnitX * -npc.spriteDirection * chargeSpeed;
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
            }

            // Spin and release bubbles.
            if (attackTimer > hoverRedirectTime)
            {
                // Slow down before the attack ends.
                if (attackTimer < hoverRedirectTime + spinTime - 30f)
                {
                    bool wouldUnfairlyHitPlayer = npc.WithinRange(target.Center, 300f) && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.31f;
                    if (attackTimer % 60f > 30f && !wouldUnfairlyHitPlayer)
                        npc.velocity = npc.velocity.RotatedBy(-TwoPi / 30f);
                }
                else
                {
                    npc.velocity *= 0.95f;
                    npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true), 0.25f);
                }
                npc.rotation = GetAdjustedRotation(npc, target, npc.velocity.ToRotation());

                if (attackTimer % bubbleReleaseRate == bubbleReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bubble = NPC.NewNPC(npc.GetSource_FromAI(), (int)mouthPosition.X, (int)mouthPosition.Y, NPCID.DetonatingBubble);
                        Main.npc[bubble].velocity = Main.npc[bubble].SafeDirectionTo(target.Center).RotatedByRandom(0.1f) * Main.rand.NextFloat(10f, 16f);
                    }
                }
            }

            if (attackTimer >= spinTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_StationaryBubbleCharge(NPC npc, Player target, Vector2 mouthPosition, ref float attackTimer, ref float frameDrawType, bool enraged)
        {
            int bubbleCount = 10;
            int bubbleShootRate = 6;
            ref float hoverDirection = ref npc.Infernum().ExtraAI[0];
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
                npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true), 0.32f);

                if (npc.WithinRange(destination, 21f))
                {
                    attackSubstate = 1f;
                    npc.Center = destination;
                    npc.velocity = Vector2.UnitX * (17f + target.velocity.Length() * 0.65f) * -hoverDirection;
                    if (npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.3f)
                        npc.velocity *= 0.6f;

                    npc.spriteDirection = (int)hoverDirection;
                    npc.rotation = 0f;
                    npc.netUpdate = true;

                    // Roar.
                    SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
                }
            }

            // And dash while releasing bubbles.
            else if (attackSubstate == 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath19, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % bubbleShootRate == bubbleShootRate - 1)
                {
                    int bubble = NPC.NewNPC(npc.GetSource_FromAI(), (int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<RedirectingBubble>());
                    Main.npc[bubble].Center += npc.velocity * 1.5f;
                    Main.npc[bubble].velocity = Vector2.UnitY * ((int)(attackTimer / bubbleShootRate) % 2 == 0).ToDirectionInt() * RedirectingBubble.InitialSpeed;
                    Main.npc[bubble].velocity += npc.velocity * 0.4f;
                    Main.npc[bubble].target = npc.target;
                }
            }

            if (attackTimer >= bubbleCount * bubbleShootRate - 20f)
                npc.velocity *= 0.965f;

            if (attackTimer >= bubbleCount * bubbleShootRate)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SharkTornadoSummon(NPC npc, Player target, ref float attackTimer, ref float frameDrawType, bool enraged)
        {
            int slowdownTime = 60;
            int sharkWaves = 6;
            int sharkSummonRate = 10;
            ref float summonOutwardness = ref npc.Infernum().ExtraAI[0];

            if (BossRushEvent.BossRushActive)
                sharkSummonRate = 8;

            if (attackTimer < slowdownTime)
                npc.velocity *= 0.95f;

            npc.rotation = npc.rotation.AngleLerp(GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true), 0.4f);

            if (attackTimer >= slowdownTime - 8f && attackTimer <= slowdownTime + 10f)
                frameDrawType = (int)DukeFrameDrawingType.OpenMouth;

            if (attackTimer == slowdownTime)
            {
                // Roar.
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
                List<int> xSpawnPositions = new()
                {
                    (int)(target.Center.X - (enraged ? 600f : 800f)) / 16,
                    (int)(target.Center.X + (enraged ? 600f : 800f)) / 16
                };

                // Summon tornadoes on the ground/water.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int y = Utils.Clamp((int)target.Center.Y / 16 - 5, 20, Main.maxTilesY - 20);
                    foreach (int x in xSpawnPositions)
                    {
                        WorldUtils.Find(new Point(x, y), Searches.Chain(new Searches.Down(Main.maxTilesY - 10), new CustomTileConditions.IsWaterOrSolid()), out Point result);
                        Vector2 spawnPosition = result.ToWorldCoordinates();
                        Vector2 tornadoVelocity = Vector2.UnitX * (target.Center.X > spawnPosition.X).ToDirectionInt() * 4f;
                        int tornado = Utilities.NewProjectileBetter(spawnPosition, tornadoVelocity, ModContent.ProjectileType<Tornado>(), TornadoDamage, 0f);
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
                    flySpeed = MathF.Min(flySpeed, 27f);
                    if (Main.projectile.IndexInRange(summoner))
                    {
                        Main.projectile[summoner].direction = i;
                        Main.projectile[summoner].ai[1] = flySpeed;
                    }
                }

                summonOutwardness += 200f;
            }

            if (attackTimer >= slowdownTime + sharkWaves * sharkSummonRate)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_TidalWave(NPC npc, Player target, ref float attackTimer, ref float frameDrawType, bool inPhase3, bool enraged)
        {
            int lungeMaxTime = 105;
            int redirectTime = inPhase3 ? 32 : 45;
            float lungeSpeed = enraged ? 30f : 24.5f;
            float waveSpeed = enraged ? 20f : 13.5f;
            if (inPhase3)
            {
                lungeSpeed *= 1.64f;
                waveSpeed *= 1.8f;
            }

            if (BossRushEvent.BossRushActive)
            {
                lungeSpeed *= 2f;
                waveSpeed *= 1.35f;
            }

            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            if (attackTimer == 1f)
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.DukeFishronTidalWaveTip");

            if (attackTimer < redirectTime)
            {
                Vector2 destination = target.Center - Vector2.UnitY.RotatedBy(target.velocity.X / 20f * ToRadians(26f)) * 430f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 15f, 0.5f);
                npc.rotation = GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true);
            }
            if (attackTimer == redirectTime)
            {
                // Roar.
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.velocity = npc.SafeDirectionTo(target.Center) * lungeSpeed;
                npc.velocity.Y = Math.Abs(npc.velocity.Y);
                npc.rotation = GetAdjustedRotation(npc, target, npc.velocity.ToRotation());

                npc.netUpdate = true;
            }

            if (attackTimer > redirectTime)
            {
                GenerateParticles(npc);
                if ((Collision.SolidCollision(npc.position, npc.width, npc.width) ||
                    Collision.WetCollision(npc.position, npc.width, npc.width) ||
                    attackTimer >= redirectTime + lungeMaxTime) && attackTimer >= redirectTime + 65f && !npc.WithinRange(target.Center, 375f))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            int wave = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * waveSpeed * i, ModContent.ProjectileType<TidalWave>(), TidalWaveDamage, 0f);
                            Main.projectile[wave].Bottom = npc.Center + Vector2.UnitY * 700f;
                        }
                    }

                    // Very heavily disturb water.
                    if (Main.netMode != NetmodeID.Server)
                    {
                        WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                        float waveSine = 0.1f * Sin(Main.GlobalTimeWrappedHourly * 20f);
                        Vector2 ripplePos = npc.Center + npc.velocity * 7f;
                        Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
                        ripple.QueueRipple(ripplePos, waveData, Vector2.One * 860f, RippleShape.Circle, npc.rotation);
                    }
                    npc.velocity *= -0.5f;
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_RazorbladeRazorstorm(NPC npc, Player target, ref float attackTimer, ref float frameDrawType, bool inPhase4, bool enraged)
        {
            int hoverTime = 105;
            int chargeRedirectTime = 23;
            int chargeCount = 7;
            int upwardChargeCount = 4;
            int upwardChargeTime = 40;
            int upwardChargeFadeinTime = 18;
            int typhoonBurstRate = enraged ? 16 : 28;
            int typhoonCount = enraged ? 11 : 5;
            float upwardChargeSpeed = 37f;
            float initialChargeSpeed = enraged ? 23f : 17f;
            float typhoonBurstSpeed = enraged ? 11f : 6.4f;
            ref float offsetDirection = ref npc.Infernum().ExtraAI[0];

            if (inPhase4)
            {
                initialChargeSpeed += 2.5f;
                typhoonBurstRate -= 3;
                typhoonCount += 2;
            }

            if (BossRushEvent.BossRushActive)
                initialChargeSpeed *= 1.2f;

            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            if (attackTimer < hoverTime)
            {
                // Disable contact damage while hovering.
                npc.damage = 0;

                if (offsetDirection == 0f)
                {
                    offsetDirection = Main.rand.NextBool().ToDirectionInt();
                    npc.netUpdate = true;
                }

                Vector2 destination = target.Center - Vector2.UnitY * 1050f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 33f, 1.3f);
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.014f).MoveTowards(destination, 15f);
                npc.rotation = GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true);

                // Teleport to the destination at the end of the attack.
                if (attackTimer == hoverTime - 1f)
                {
                    npc.Center = destination;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Summon tornadoes.
            if (attackTimer == hoverTime - 45f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.DukeFishronRazorbladeTip");
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    List<int> horizontalSpawnPositions = new()
                    {
                        (int)(target.Center.X - (enraged ? 600f : 750f)) / 16,
                        (int)(target.Center.X + (enraged ? 600f : 750f)) / 16
                    };

                    int y = Utils.Clamp((int)target.Center.Y / 16 + 95, 20, Main.maxTilesY - 20);
                    foreach (int x in horizontalSpawnPositions)
                    {
                        Vector2 spawnPosition = new Point(x, y).ToWorldCoordinates();
                        int tornado = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<Tornado>(), TornadoDamage, 0f);
                        Main.projectile[tornado].ai[1] = 1f;
                        Main.projectile[tornado].Bottom = spawnPosition;
                    }
                }
            }

            // Roar, make the initial charge, and summon tornado borders.
            if (attackTimer == hoverTime)
            {
                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                npc.velocity = Vector2.UnitY * initialChargeSpeed;

                // Roar.
                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);

                npc.rotation = GetAdjustedRotation(npc, target, npc.velocity.ToRotation());
                npc.netUpdate = true;
            }

            // Do reflected charge.
            if (attackTimer > hoverTime && attackTimer < hoverTime + chargeCount * chargeRedirectTime)
            {
                if (attackTimer % typhoonBurstRate == typhoonBurstRate - 1f && !npc.WithinRange(target.Center, 200f))
                {
                    SoundEngine.PlaySound(SoundID.Item84, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < typhoonCount; i++)
                        {
                            float offsetAngle = TwoPi * i / typhoonCount;
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * typhoonBurstSpeed;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<TyphoonBlade>(), ChargeTyphoonDamage, 0f);
                        }
                    }
                }
            }

            // Perform upward charges.
            if (attackTimer >= hoverTime + chargeCount * chargeRedirectTime)
            {
                float chargeTimer = (attackTimer - (hoverTime + chargeCount * chargeRedirectTime)) % (upwardChargeFadeinTime + upwardChargeTime);

                // Fade in and look up at the target.
                if (chargeTimer <= upwardChargeFadeinTime)
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitY * 360f;
                    npc.Opacity = chargeTimer / upwardChargeFadeinTime;

                    if (npc.position.Y < target.Center.Y + 20f)
                        npc.position.Y = target.Center.Y + 20f;

                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.125f).MoveTowards(hoverDestination, 5f);
                    npc.rotation = GetAdjustedRotation(npc, target, npc.AngleTo(target.Center));
                    npc.velocity = Vector2.Zero;
                }

                // Charge.
                else
                {
                    npc.velocity.X = 0f;
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * upwardChargeSpeed, 0.18f);
                    npc.rotation = GetAdjustedRotation(npc, target, npc.velocity.ToRotation());
                    GenerateParticles(npc);
                }
            }

            if (attackTimer >= hoverTime + chargeCount * chargeRedirectTime + (upwardChargeFadeinTime + upwardChargeTime) * upwardChargeCount - 1f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ChargeTeleport(NPC npc, Player target, ref float attackTimer, ref float frameDrawType, ref float eyeGlowmaskOpacity, bool inPhase4, bool enraged)
        {
            int teleportDelay = 12;
            int chargeDelay = 42;
            if (inPhase4 || enraged)
                chargeDelay -= 8;

            // Disable contact damage while teleporting.
            npc.damage = 0;

            // Teleport on the first frame and roar.
            if (attackTimer == teleportDelay)
            {
                npc.Center = target.Center + new Vector2(Main.rand.NextBool().ToDirectionInt() * 560f, -270f);
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Zombie20, npc.Center);
            }

            // Otherwise hover in place.
            else
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -300f) - npc.velocity;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 26f, 0.9f);
            }

            npc.rotation = GetAdjustedRotation(npc, target, npc.AngleTo(target.Center), true);
            npc.Opacity = Utils.GetLerpValue(teleportDelay, 0f, attackTimer, true) + Utils.GetLerpValue(0f, chargeDelay, attackTimer - teleportDelay, true);
            eyeGlowmaskOpacity = Utils.GetLerpValue(teleportDelay, 0f, attackTimer, true) + Utils.GetLerpValue(0f, 12f, attackTimer - teleportDelay - chargeDelay, true);
            frameDrawType = (int)DukeFrameDrawingType.OpenMouthFinFlapping;

            if (attackTimer >= chargeDelay)
                SelectNextAttack(npc);
        }

        public static void GenerateParticles(NPC npc)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Vector2 spawnPosition = (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(i * Pi / 7f) + npc.Center;
                Vector2 dustVelocity = (Main.rand.NextFloat(Pi) - PiOver2).ToRotationVector2() * Main.rand.Next(3, 8);
                int water = Dust.NewDust(spawnPosition + dustVelocity, 0, 0, DustID.DungeonWater, dustVelocity.X * 2f, dustVelocity.Y * 2f, 100, default, 1.4f);
                Main.dust[water].noGravity = true;
                Main.dust[water].noLight = true;
                Main.dust[water].velocity *= 0.25f;
                Main.dust[water].velocity -= npc.velocity * 0.6f;
            }
        }
        #endregion

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            AchievementPlayer.DukeFishronDefeated = true;
            return true;
        }
        #endregion Death Effects

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 202;
            DukeFrameDrawingType frameDrawType = (DukeFrameDrawingType)(int)npc.ai[3];
            switch (frameDrawType)
            {
                case DukeFrameDrawingType.FinFlapping:
                    int frame = (int)(npc.frameCounter / 6) % 6;
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
                    frame = (int)(npc.frameCounter / 6) % 4 + 7;
                    npc.frame.X = 202;
                    npc.frame.Y = (frame - 7) * frameHeight;
                    break;
            }
            npc.frameCounter++;
        }

        public static Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, Math.Abs(Sin(completionRatio * Pi + Main.GlobalTimeWrappedHourly))) * (1f - completionRatio) * 1.6f;
        }

        public static float WidthFunction(float completionRatio) => SmoothStep(50f, 35f, completionRatio);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 22;
            ref float afterimageCount = ref npc.Infernum().ExtraAI[13];

            // Declare the trail drawer.
            npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(InfernumTextureRegistry.VoronoiShapes);

            bool hasEyes = npc.Infernum().ExtraAI[9] == 1f || npc.Infernum().ExtraAI[11] > 0f;
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DukeFishron/DukeFishronGlowmask").Value;
            Texture2D dukeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DukeFishron/DukeFishronResprite").Value;
            Vector2 origin = npc.frame.Size() * 0.5f;
            void DrawOldDukeInstance(Color color, Vector2 drawPosition, int direction)
            {
                SpriteEffects spriteEffects = direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                if (npc.life / (float)npc.lifeMax < Phase2LifeRatio)
                    color = Color.Lerp(color, Color.Blue, 0.1f);

                color = npc.GetAlpha(color);

                float transitionCompletionRatio = npc.Infernum().ExtraAI[8] / 120f;
                if (transitionCompletionRatio > 0f)
                {
                    float drawOffsetFactor = Utils.GetLerpValue(0f, 0.75f, transitionCompletionRatio, true) * Utils.GetLerpValue(1f, 0.75f, transitionCompletionRatio, true) * 30f;
                    Color backimageColor = lightColor.MultiplyRGB(Color.Turquoise) * transitionCompletionRatio * 0.4f;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = (TwoPi * i / 4f).ToRotationVector2() * drawOffsetFactor;
                        Main.spriteBatch.Draw(dukeTexture, drawPosition + drawOffset - Main.screenPosition, npc.frame, backimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                    }
                }

                Main.spriteBatch.Draw(dukeTexture, drawPosition - Main.screenPosition, npc.frame, color, npc.rotation, origin, npc.scale, spriteEffects, 0f);

                if (hasEyes)
                {
                    Color eyeColor = Color.Lerp(Color.White, Color.Yellow, 0.5f) * npc.Infernum().ExtraAI[11];
                    eyeColor *= Pow(color.ToVector3().Length() / 1.414f, 0.6f);
                    Main.spriteBatch.Draw(eyeTexture, drawPosition - Main.screenPosition, npc.frame, eyeColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            int idealAfterimageCount = 0;
            if (npc.Infernum().ExtraAI[5] == (int)DukeAttackType.ChargeTeleport)
                idealAfterimageCount = 3;
            if (npc.Infernum().ExtraAI[5] == (int)DukeAttackType.Charge)
                idealAfterimageCount = 6;
            if (npc.Infernum().ExtraAI[5] == (int)DukeAttackType.BubbleSpin)
                idealAfterimageCount = 10;

            if (afterimageCount != idealAfterimageCount)
                afterimageCount += Math.Sign(idealAfterimageCount - afterimageCount);

            // Draw afterimages.
            if (afterimageCount > 0)
            {
                if (npc.oldPos.Length < afterimageCount + 1)
                {
                    npc.oldPos = new Vector2[(int)afterimageCount + 1];
                    npc.oldRot = new float[(int)afterimageCount + 1];
                }

                for (int i = (int)afterimageCount; i >= 1; i--)
                {
                    Color afterimageColor = lightColor.MultiplyRGB(Color.White) * Pow(1f - i / (float)afterimageCount, 3f);
                    DrawOldDukeInstance(afterimageColor, npc.oldPos[i] + npc.Size * 0.5f, npc.spriteDirection);
                }
            }
            DrawOldDukeInstance(lightColor, npc.Center, npc.spriteDirection);
            if (npc.Infernum().ExtraAI[5] == (int)DukeAttackType.BubbleSpin)
            {
                for (int i = 0; i < 2; i++)
                    npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos.Take((int)afterimageCount * 2), npc.Size * 0.5f - Main.screenPosition, 43);
            }

            return false;
        }
        #endregion
    }
}
