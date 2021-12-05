using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;
using CalamitasCloneNPC = CalamityMod.NPCs.Calamitas.CalamitasRun3;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CalamitasCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasCloneNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const bool ReadyToUseBuffedAI = true;

        #region Enumerations
        public enum CloneAttackType
        {
            HorizontalDartRelease,
            BrimstoneMeteors,
            BrimstoneVolcano,
            BrimstoneLightning,
            BrimstoneFireBurst,
            DiagonalCharge,
            RisingBrimstoneFireBursts,
            HorizontalBurstCharge
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.7f;
        public const float Phase3LifeRatio = 0.3f;
        public const float Phase4LifeRatio = 0.15f;
        public const int FinalPhaseTransitionTime = 180;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.calamitas = npc.whoAmI;

            npc.defense = npc.defDefense = 0;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -28f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && ReadyToUseBuffedAI;
            int brotherCount = NPC.CountNPCS(ModContent.NPCType<CalamitasRun>()) + NPC.CountNPCS(ModContent.NPCType<CalamitasRun2>());
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.Infernum().ExtraAI[7];
            ref float transitionState = ref npc.ai[2];
            ref float brotherFadeoutTime = ref npc.ai[3];
            ref float finalPhaseTransitionCountdown = ref npc.Infernum().ExtraAI[8];
            ref float finalPhaseFireTimer = ref npc.Infernum().ExtraAI[9];

            bool brotherIsPresent = brotherCount > 0 || (brotherFadeoutTime > 0f && brotherFadeoutTime < 50f && transitionState < 2f);

            bool inFinalPhase = transitionState == 4f;

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = NPC.AnyNPCs(ModContent.NPCType<SoulSeeker>());

            // Prepare to fade out and summon brothers.
            if (Main.netMode != NetmodeID.MultiplayerClient && transitionState == 0f && lifeRatio < Phase2LifeRatio)
            {
                transitionState = 1f;
                brotherFadeoutTime = 1f;
                attackTimer = 0f;

                Main.NewText($"Destroy {(target.Male ? "him" : "her")}, my brothers.", Color.Orange);

                // Set the ring radius and create a soul seeker ring.
                npc.Infernum().ExtraAI[6] = shouldBeBuffed ? 950f : 750f;
                for (int i = 0; i < 50; i++)
                {
                    float seekerAngle = MathHelper.TwoPi * i / 50f;
                    int seeker = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SoulSeeker2>());
                    if (Main.npc.IndexInRange(seeker))
                        Main.npc[seeker].ai[0] = seekerAngle;
                }

                npc.netUpdate = true;
                return false;
            }

            if (transitionState == 1f && brotherCount <= 0 && brotherFadeoutTime > 40f)
            {
                transitionState = 2f;
                npc.netUpdate = true;
            }

            // Create a seeker ring once at a low enough life.
            if (transitionState == 2f && lifeRatio < Phase3LifeRatio)
            {
                Main.NewText("You will suffer.", Color.Orange);

                int seekerCount = 7;
                for (int i = 0; i < seekerCount; i++)
                {
                    int spawn = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SoulSeeker>(), npc.whoAmI, 0, 0, 0, -1);
                    Main.npc[spawn].ai[0] = 360f / seekerCount * i;
                }
                SelectNewAttack(npc);

                transitionState = 3f;
                npc.netUpdate = true;
            }

            // Begin transitioning to the final phase once the seekers have been spawned and then killed.
            if (transitionState == 3f && !NPC.AnyNPCs(ModContent.NPCType<SoulSeeker>()))
            {
                transitionState = 4f;
                attackTimer = 0f;
                finalPhaseTransitionCountdown = FinalPhaseTransitionTime;
                npc.netUpdate = true;
                return false;
            }

            // Do phase transitions.
            if (finalPhaseTransitionCountdown > 0f)
            {
                npc.dontTakeDamage = true;

                finalPhaseTransitionCountdown--;
                npc.velocity *= 0.97f;
                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                if (finalPhaseTransitionCountdown == 60f)
                    Main.NewText("I will not be defeated so easily.", Color.Orange);

                if (finalPhaseTransitionCountdown == 0f)
                {
                    attackTimer = 0f;
                    SelectNewAttack(npc);

                    int explosion = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);
                    Main.projectile[explosion].ai[0] = NPCID.Retinazer;
                }

                return false;
            }

            // Periodically release fire in the final phase.
            if (inFinalPhase)
                finalPhaseFireTimer++;

            if (finalPhaseFireTimer % 170f == 169f)
            {
                Main.PlaySound(SoundID.Item74, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 spawnPosition = target.Center + (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 1300f;
                        Vector2 burstVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 11f;
                        if (shouldBeBuffed)
                            burstVelocity *= 2.7f;
                        Utilities.NewProjectileBetter(spawnPosition, burstVelocity, ModContent.ProjectileType<BrimstoneBurstTelegraph>(), 0, 0f);
                    }
                }
            }

            // Fade away and don't do damage if brothers are present.
            if (brotherFadeoutTime > 0f)
            {
                // Reset the attack state for when the attack concludes.
                attackType = (int)CloneAttackType.HorizontalDartRelease;

                npc.damage = 0;
                npc.dontTakeDamage = true;
                brotherFadeoutTime = MathHelper.Clamp(brotherFadeoutTime + brotherIsPresent.ToDirectionInt(), 0f, 90f);
                npc.Opacity = 1f - brotherFadeoutTime / 90f;

                if (Main.netMode != NetmodeID.MultiplayerClient && brotherFadeoutTime == 30f && transitionState == 1f)
                {
                    // Summon Catatrophe and Cataclysm.
                    int cataclysm = NPC.NewNPC((int)target.Center.X - 1000, (int)target.Center.Y - 1000, ModContent.NPCType<CalamitasRun>());
                    CalamityUtils.BossAwakenMessage(cataclysm);

                    int catastrophe = NPC.NewNPC((int)target.Center.X + 1000, (int)target.Center.Y - 1000, ModContent.NPCType<CalamitasRun2>());
                    CalamityUtils.BossAwakenMessage(catastrophe);
                }

                Vector2 hoverDestination = target.Center;
                if (!brotherIsPresent)
                    hoverDestination.Y -= 350f;

                // Move the ring towards the target.
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 4.5f, 0.2f);

                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                return false;
            }

            switch ((CloneAttackType)(int)attackType)
            {
                case CloneAttackType.HorizontalDartRelease:
                    npc.damage = 0;
                    DoBehavior_HorizontalDartRelease(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneMeteors:
                    npc.damage = 0;
                    DoBehavior_BrimstoneMeteors(npc, target, lifeRatio, inFinalPhase, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneVolcano:
                    npc.damage = 0;
                    DoBehavior_BrimstoneVolcano(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneLightning:
                    npc.damage = 0;
                    DoBehavior_BrimstoneLightning(npc, target, inFinalPhase, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneFireBurst:
                    npc.damage = 0;
                    DoBehavior_BrimstoneFireBurst(npc, target, lifeRatio, inFinalPhase, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.DiagonalCharge:
                    DoBehavior_DiagonalCharge(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.RisingBrimstoneFireBursts:
                    npc.damage = 0;
                    DoBehavior_RisingBrimstoneFireBursts(npc, target, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.HorizontalBurstCharge:
                    npc.damage = 0;
                    DoBehavior_HorizontalBurstCharge(npc, target, shouldBeBuffed, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HorizontalDartRelease(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackCycleCount = 3;
            int hoverTime = 210;
            float hoverHorizontalOffset = 530f;
            float hoverSpeed = 20f;
            float initialFlameSpeed = 10.75f;
            float flameAngularVariance = 0.84f;
            int flameReleaseRate = 8;
            int flameReleaseTime = 180;
            if (lifeRatio < Phase2LifeRatio)
            {
                attackCycleCount--;
                hoverHorizontalOffset -= 70f;
                initialFlameSpeed += 6f;
                flameAngularVariance *= 1.35f;
                flameReleaseRate -= 2;
            }

            if (shouldBeBuffed)
            {
                attackCycleCount--;
                hoverSpeed += 4f;
                initialFlameSpeed *= 1.35f;
            }

            if (BossRushEvent.BossRushActive)
            {
                attackCycleCount--;
                hoverSpeed += 8f;
                initialFlameSpeed *= 1.72f;
            }

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 110f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release fireballs.
            if (attackSubstate == 1f)
            {
                if (attackTimer % flameReleaseRate == flameReleaseRate - 1f && attackTimer % 90f > 35f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dartDamage = shouldBeBuffed ? 310 : 145;
                        float idealDirection = npc.AngleTo(target.Center);
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 32f, -Vector2.UnitY).RotatedByRandom(flameAngularVariance) * initialFlameSpeed;

                        int cinder = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AdjustingCinder>(), dartDamage, 0f);
                        if (Main.projectile.IndexInRange(cinder))
                            Main.projectile[cinder].ai[0] = idealDirection;
                    }
                }

                if (attackTimer > flameReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_BrimstoneMeteors(NPC npc, Player target, float lifeRatio, bool inFinalPhase, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackDelay = 90;
            int attackTime = 480;
            int meteorShootRate = 8;
            float meteorShootSpeed = 18.5f;
            float hoverSpeed = 20f;
            if (shouldBeBuffed)
            {
                attackTime += 60;
                meteorShootRate -= 3;
                meteorShootSpeed += 5f;
                hoverSpeed += 5f;
            }
            if (BossRushEvent.BossRushActive)
            {
                attackTime -= 45;
                hoverSpeed += 8f;
                meteorShootSpeed *= 1.4f;
            }
            if (inFinalPhase)
            {
                attackTime -= 50;
                meteorShootRate--;
            }

            meteorShootSpeed *= MathHelper.Lerp(1f, 1.35f, 1f - lifeRatio);

            ref float meteorAngle = ref npc.Infernum().ExtraAI[0];

            // Attempt to hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 380f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Create an explosion sound and decide the meteor angle right before the meteors fall.
            if (attackTimer == attackDelay - 25f)
            {
                Main.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);
                meteorAngle = Main.rand.NextFloatDirection() * MathHelper.Pi / 9f;
                npc.netUpdate = true;
            }

            bool canFire = attackTimer > attackDelay && attackTimer < attackTime + attackDelay;

            // Rain meteors from the sky. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire && attackTimer % meteorShootRate == meteorShootRate - 1f)
            {
                int meteorDamage = shouldBeBuffed ? 325 : 150;
                float horizontalOffsetMax = MathHelper.Lerp(450f, 1050f, Utils.InverseLerp(0f, 8f, target.velocity.Length(), true));
                Vector2 meteorSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-horizontalOffsetMax, horizontalOffsetMax), -780f);
                Vector2 shootDirection = Vector2.UnitY.RotatedBy(meteorAngle);
                Vector2 shootVelocity = shootDirection * meteorShootSpeed;

                int meteorType = ModContent.ProjectileType<BrimstoneMeteor>();
                Utilities.NewProjectileBetter(meteorSpawnPosition, shootVelocity, meteorType, meteorDamage, 0f);
            }

            if (attackTimer > attackTime + attackDelay * 2f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BrimstoneVolcano(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackDelay = 45;
            int attackTime = 300;
            int lavaShootRate = 24;
            float hoverSpeed = 20f;

            if (lifeRatio < Phase2LifeRatio)
            {
                attackTime += 25;
                lavaShootRate -= 9;
            }

            if (shouldBeBuffed)
            {
                attackTime += 45;
                lavaShootRate -= 7;
                hoverSpeed += 5f;
            }

            if (BossRushEvent.BossRushActive)
            {
                attackTime -= 45;
                hoverSpeed += 8f;
                lavaShootRate -= 7;
            }

            // Attempt to hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 350f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Create an flame burst sound and before the lava comes up.
            if (attackTimer == attackDelay - 25f)
            {
                Main.PlaySound(SoundID.DD2_FlameburstTowerShot, target.Center);
                npc.netUpdate = true;
            }

            bool canFire = attackTimer > attackDelay && attackTimer < attackTime + attackDelay;

            // Create lava from the ground. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire && attackTimer % lavaShootRate == lavaShootRate - 1f)
            {
                int lavaDamage = shouldBeBuffed ? 325 : 150;
                Vector2 lavaSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 50f + target.velocity.X * Main.rand.NextFloat(35f, 60f), 420f);
                if (WorldUtils.Find(lavaSpawnPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(1500), new Conditions.IsSolid()), out Point result))
                {
                    lavaSpawnPosition = result.ToWorldCoordinates();
                    int lavaType = ModContent.ProjectileType<BrimstoneGeyser>();
                    Utilities.NewProjectileBetter(lavaSpawnPosition, Vector2.Zero, lavaType, lavaDamage, 0f);
                }

                // Use a different attack if a bottom could not be located.
                else
                    SelectNewAttack(npc);
            }

            if (attackTimer > attackTime + attackDelay * 2f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BrimstoneLightning(NPC npc, Player target, bool inFinalPhase, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackDelay = 45;
            int attackTime = 330;
            int lightningShootRate = 23;
            float hoverSpeed = 18f;

            if (shouldBeBuffed)
            {
                attackTime += 45;
                lightningShootRate -= 8;
                hoverSpeed += 6f;
            }

            if (BossRushEvent.BossRushActive)
            {
                attackTime -= 45;
                hoverSpeed += 8f;
                lightningShootRate -= 9;
            }

            if (inFinalPhase)
            {
                attackTime -= 45;
                lightningShootRate -= 4;
            }

            // Attempt to hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 350f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Create a thunder sound and before the lightning comes down.
            if (attackTimer == attackDelay - 25f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);
                npc.netUpdate = true;
            }

            bool canFire = attackTimer > attackDelay && attackTimer < attackTime + attackDelay;

            // Create lightning from the sky. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire && attackTimer % lightningShootRate == lightningShootRate - 1f)
            {
                Vector2 lightningSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 350f + target.velocity.X * 42f, 40f);
                Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.Zero, ModContent.ProjectileType<BrimstoneLightningTelegraph>(), 0, 0f);
            }

            if (attackTimer > attackTime + attackDelay * 2f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BrimstoneFireBurst(NPC npc, Player target, float lifeRatio, bool inFinalPhase, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackCycleCount = 2;
            int hoverTime = 210;
            float hoverHorizontalOffset = 600f;
            float hoverSpeed = 19f;
            float fireballSpeed = MathHelper.Lerp(12f, 15.6f, 1f - lifeRatio);

            int fireballCount = 5;
            int fireballReleaseRate = 36;
            int fireballReleaseTime = 150;
            float fireballSpread = 0.7f;

            if (inFinalPhase)
            {
                fireballCount = 3;
                fireballReleaseRate -= 10;
                fireballSpeed *= 1.15f;
            }

            if (shouldBeBuffed)
            {
                attackCycleCount--;
                fireballReleaseRate = (int)(fireballReleaseRate * 0.65f);
                hoverSpeed += 6f;
                fireballSpeed += 8.5f;
            }

            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed += 8f;
                fireballReleaseRate = (int)(fireballReleaseRate * 0.6f);
                fireballSpeed *= 1.5f;
            }

            if (Math.Abs(Vector2.Dot(target.velocity, Vector2.UnitX)) > 0.91f)
            {
                hoverSpeed += 8f;
                fireballReleaseRate = (int)(fireballReleaseRate * 0.65f);
                fireballSpeed *= 1.3f;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<SoulSeeker>()))
            {
                fireballSpeed *= 0.915f;
                fireballCount = 4;
                fireballSpread *= 1.225f;
            }

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 60f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release fireballs.
            if (attackSubstate == 1f)
            {
                if (attackTimer % fireballReleaseRate == fireballReleaseRate - 1f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int fireballDamage = shouldBeBuffed ? 345 : 150;

                        for (int i = 0; i < fireballCount; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * fireballSpeed;
                            shootVelocity = shootVelocity.RotatedBy(MathHelper.Lerp(-fireballSpread, fireballSpread, i / (float)(fireballCount - 1f)) + Main.rand.NextFloatDirection() * 0.13f);
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ExplodingBrimstoneFireball>(), fireballDamage, 0f);
                        }
                    }
                }

                if (attackTimer > fireballReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_DiagonalCharge(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            float chargeOffset = 395f;
            float redirectSpeed = 33f;
            float chargeSpeed = MathHelper.Lerp(26.75f, 30.25f, 1f - lifeRatio);
            int chargeTime = 50;
            int chargeSlowdownTime = 15;
            int chargeCount = 5;

            if (shouldBeBuffed)
            {
                chargeSpeed *= 1.5f;
                redirectSpeed += 6f;
            }

            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed *= 1.72f;
                redirectSpeed += 6f;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<SoulSeeker>()))
                chargeSpeed *= 0.825f;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Hover into position.
                case 0:
                    Vector2 hoverDestination = target.Center;
                    hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * chargeOffset;
                    hoverDestination.Y += (target.Center.Y < npc.Center.Y).ToDirectionInt() * chargeOffset;

                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * redirectSpeed, redirectSpeed / 20f);
                    npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                    if (attackTimer > 240f || (npc.WithinRange(hoverDestination, 120f) && attackTimer > 50f))
                    {
                        Main.PlaySound(SoundID.Roar, npc.Center, 0);
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f, -Vector2.UnitY) * chargeSpeed;
                        attackTimer = 0f;
                        attackState = 1f;
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;

                    // Slow down after the charge has ended and look at the target.
                    if (attackTimer > chargeTime)
                    {
                        npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.1f) * 0.96f;
                        float idealRotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;
                        npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.08f).AngleTowards(idealRotation, 0.15f);
                    }

                    // Go to the next attack once done slowing down.
                    if (attackTimer > chargeTime + chargeSlowdownTime)
                    {
                        chargeCounter++;
                        attackTimer = 0f;
                        attackState = 0f;
                        if (chargeCounter >= chargeCount)
                            SelectNewAttack(npc);
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_RisingBrimstoneFireBursts(NPC npc, Player target, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackCycleCount = 2;
            int hoverTime = 210;
            float hoverHorizontalOffset = 485f;
            float hoverSpeed = 19f;
            float fireballSpeed = 13.5f;
            int fireballReleaseRate = 22;
            int fireballReleaseTime = 360;

            if (shouldBeBuffed)
            {
                fireballReleaseRate -= 6;
                hoverSpeed += 9f;
                fireballSpeed += 7f;
            }

            if (BossRushEvent.BossRushActive)
            {
                fireballReleaseRate -= 10;
                hoverSpeed += 10f;
                fireballSpeed *= 1.65f;
            }

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 110f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release fireballs.
            if (attackSubstate == 1f)
            {
                if (attackTimer % fireballReleaseRate == fireballReleaseRate - 1f && attackTimer % 180f < 60f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int fireballDamage = shouldBeBuffed ? 320 : 140;
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * fireballSpeed;

                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<RisingBrimstoneFireball>(), fireballDamage, 0f);
                    }
                }

                if (attackTimer > fireballReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_HorizontalBurstCharge(NPC npc, Player target, bool shouldBeBuffed, ref float attackTimer)
        {
            int redirectTime = 60;
            int chargeTime = 130;
            float chargeSpeed = 21.5f;
            float fireballSpeed = 10.5f;

            if (shouldBeBuffed)
            {
                chargeSpeed += 6f;
                fireballSpeed += 6f;
            }

            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed *= 1.56f;
                fireballSpeed *= 1.7f;
            }

            if (attackTimer < redirectTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 1500f, -350f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 13f;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.04f);
                if (npc.WithinRange(hoverDestination, 20f))
                    attackTimer = redirectTime - 1f;
            }
            else if (attackTimer == redirectTime)
            {
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center);
                chargeVelocity.Y *= 0.25f;
                chargeVelocity = chargeVelocity.SafeNormalize(Vector2.UnitX);
                npc.rotation = chargeVelocity.ToRotation() - MathHelper.PiOver2;
                npc.velocity = chargeVelocity * chargeSpeed;
            }
            else
            {
                npc.position.X += npc.SafeDirectionTo(target.Center).X * 9f;
                npc.position.Y += npc.SafeDirectionTo(target.Center - Vector2.UnitY * 400f).Y * 7f;
                if (attackTimer % 30f == 29f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int fireballDamage = shouldBeBuffed ? 320 : 140;
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);
                        shootVelocity = Vector2.Lerp(shootVelocity, npc.velocity.SafeNormalize(Vector2.Zero), 0.6f).SafeNormalize(Vector2.UnitY) * fireballSpeed;

                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<RisingBrimstoneFireball>(), fireballDamage, 0f);
                    }
                }
            }

            if (attackTimer >= redirectTime + chargeTime)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            List<CloneAttackType> possibleAttacks = new List<CloneAttackType>
            {
                CloneAttackType.HorizontalDartRelease,
                CloneAttackType.BrimstoneMeteors,
                CloneAttackType.BrimstoneVolcano
            };

            for (int i = 0; i < 3; i++)
            {
                if (lifeRatio < Phase2LifeRatio)
                    possibleAttacks.Remove(CloneAttackType.HorizontalDartRelease);
                possibleAttacks.AddWithCondition(CloneAttackType.BrimstoneLightning, lifeRatio < Phase2LifeRatio);
                possibleAttacks.AddWithCondition(CloneAttackType.BrimstoneFireBurst, lifeRatio < Phase2LifeRatio);
                possibleAttacks.AddWithCondition(CloneAttackType.DiagonalCharge, lifeRatio < Phase2LifeRatio);
            }

            // If seekers are present.
            if (NPC.AnyNPCs(ModContent.NPCType<SoulSeeker>()))
            {
                possibleAttacks.Clear();
                possibleAttacks.Add(CloneAttackType.BrimstoneFireBurst);
                possibleAttacks.Add(CloneAttackType.DiagonalCharge);
            }

            // Final phase.
            if (npc.ai[2] == 4f)
            {
                possibleAttacks.Clear();
                possibleAttacks.Add(CloneAttackType.BrimstoneFireBurst);
                possibleAttacks.Add(CloneAttackType.RisingBrimstoneFireBursts);
                possibleAttacks.Add(CloneAttackType.HorizontalBurstCharge);

                if (lifeRatio < Phase4LifeRatio)
                {
                    possibleAttacks.Add(CloneAttackType.BrimstoneMeteors);
                    possibleAttacks.Add(CloneAttackType.BrimstoneLightning);
                }
            }

            if (possibleAttacks.Count > 1)
                possibleAttacks.RemoveAll(a => a == (CloneAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.Infernum().ExtraAI[7] = 0f;
            npc.netUpdate = true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            int afterimageCount = 7;
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 origin = npc.frame.Size() * 0.5f;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = Color.Lerp(lightColor, Color.White, 0.5f) * ((afterimageCount - i) / 15f) * npc.Opacity;
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + origin - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 drawPosition = npc.position + origin - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, npc.frame, lightColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/Calamitas/CalamitasRun3Glow");
            Color afterimageBaseColor = Color.Lerp(Color.White, Color.Red, 0.5f);

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i++)
                {
                    Color afterimageColor = Color.Lerp(afterimageBaseColor, Color.White, 0.5f) * ((afterimageCount - i) / 15f) * npc.Opacity;
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + origin - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion AI
    }
}
