using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Polterghast;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using PolterghastBoss = CalamityMod.NPCs.Polterghast.Polterghast;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PolterghastBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.35f;

        #region Enumerations
        public enum PolterghastAttackType
        {
            IdleMove,
            ReleaseBurstsOfSouls,
            Impale,
            RockCharge,
            SpiritPetal,
            EtherealRoar,
            BeastialExplosion,
            CloneSplit
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.ghostBoss = npc.whoAmI;

            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                for (int i = 0; i < 4; i++)
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EerieLimb>(), 0, i);
                npc.localAI[3] = 1f;
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            PolterghastAttackType attackState = (PolterghastAttackType)(int)npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float totalReleasedSouls = ref npc.ai[2];
            ref float dyingTimer = ref npc.Infernum().ExtraAI[6];
            ref float initialDeathPositionX = ref npc.Infernum().ExtraAI[7];
            ref float initialDeathPositionY = ref npc.Infernum().ExtraAI[8];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool enraged = npc.Bottom.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            npc.Calamity().CurrentlyEnraged = enraged;

            // Store the enraged field so that the limbs can check it more easily.
            npc.ai[3] = enraged.ToInt();

            if (phase3)
                npc.HitSound = SoundID.NPCHit36;

            if (totalReleasedSouls < 0f)
                totalReleasedSouls = 0f;

            npc.scale = MathHelper.Lerp(1.225f, 0.68f, MathHelper.Clamp(totalReleasedSouls / 60f, 0f, 1f));

            if (dyingTimer > 0f)
            {
                npc.dontTakeDamage = true;
                npc.DeathSound = InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PolterghastDeath");

                // Clear away any clones and legs.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int cloneType = NPC.CountNPCS(ModContent.NPCType<PolterPhantom>());
                    int legType = ModContent.NPCType<EerieLimb>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if ((Main.npc[i].type == cloneType || Main.npc[i].type == legType) && Main.npc[i].active)
                        {
                            Main.npc[i].life = 0;
                            Main.npc[i].active = false;
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }
                }

                // Quickly slow down.
                npc.velocity *= 0.955f;

                dyingTimer++;

                float turnSpeed = Utils.InverseLerp(240f, 45f, dyingTimer, true);
                if (turnSpeed > 0f)
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) + MathHelper.PiOver2, turnSpeed);

                // Begin releasing souls.
                if (dyingTimer > 210f && dyingTimer % 2f == 0f && totalReleasedSouls < 60f)
                {
                    if (dyingTimer % 8f == 0f)
                        Main.PlaySound(SoundID.NPCHit36, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(7f, 13f);
                        int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<DeathAnimationSoul>(), 0, 0f);
                        if (Main.projectile.IndexInRange(soul))
                            Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();

                        totalReleasedSouls++;

                        npc.netSpam = 0;
                        npc.netUpdate = true;
                    }
                }

                if (totalReleasedSouls >= 60f)
                {
                    // Focus on the boss as it jitters and explode.
                    if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 2700f))
                    {
                        Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                        Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.InverseLerp(270f, 290f, dyingTimer, true);
                        Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.InverseLerp(370f, 362f, dyingTimer, true);
                    }

                    Vector2 jitter = Main.rand.NextVector2Unit() * MathHelper.SmoothStep(1f, 3.25f, Utils.InverseLerp(270f, 350f, dyingTimer, true));
                    Main.LocalPlayer.Infernum().CurrentScreenShakePower = jitter.Length() * Utils.InverseLerp(1950f, 1100f, Main.LocalPlayer.Distance(npc.Center), true) * 4f;

                    if (initialDeathPositionX != 0f && initialDeathPositionY != 0f)
                        npc.Center = new Vector2(initialDeathPositionX, initialDeathPositionY) + jitter;

                    // Make a flame-like sound effect right before dying.
                    if (dyingTimer == 368f)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), target.Center);
                    else
                    {
                        initialDeathPositionX = npc.Center.X;
                        initialDeathPositionY = npc.Center.Y;
                        npc.netUpdate = true;
                    }

                    // Release a bunch of other souls right before death.
                    if (Main.netMode != NetmodeID.MultiplayerClient && dyingTimer > 360f)
                    {
                        Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 9f);
                        int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<DeathAnimationSoul>(), 0, 0f);
                        if (Main.projectile.IndexInRange(soul))
                        {
                            Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();
                            Main.projectile[soul].ai[1] = 1f;
                        }
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && dyingTimer == 370f)
                    {
                        for (int i = 0; i < 125; i++)
                        {
                            Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 9f);
                            int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<DeathAnimationSoul>(), 0, 0f);
                            if (Main.projectile.IndexInRange(soul))
                            {
                                Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();
                                Main.projectile[soul].ai[1] = 1f;
                            }
                        }

                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.checkDead();
                    }
                }
                else if (dyingTimer > 270f)
                {
                    // Declare the death position for the sake of jittering later.
                    if (initialDeathPositionX == 0f || initialDeathPositionY == 0f)
                    {
                        initialDeathPositionX = npc.Center.X;
                        initialDeathPositionY = npc.Center.Y;
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                    dyingTimer = 260f;
                }

                return false;
            }

            int totalClones = NPC.CountNPCS(ModContent.NPCType<PolterPhantom>());
            if (totalClones > 0)
                npc.scale = MathHelper.Lerp(0.7f, 1.225f, 1f - totalClones / 2f);

            npc.dontTakeDamage = false;
            npc.hide = false;

            switch (attackState)
            {
                case PolterghastAttackType.IdleMove:
                    DoAttack_IdleMove(npc, target, ref attackTimer, enraged);
                    break;
                case PolterghastAttackType.ReleaseBurstsOfSouls:
                    DoAttack_ReleaseBurstsOfSouls(npc, target, ref attackTimer, ref totalReleasedSouls, enraged);
                    break;
                case PolterghastAttackType.Impale:
                    DoAttack_Impale(npc, target, ref attackTimer);
                    break;
                case PolterghastAttackType.SpiritPetal:
                    DoAttack_SpiritPetal(npc, target, ref attackTimer, ref totalReleasedSouls, enraged);
                    break;
                case PolterghastAttackType.RockCharge:
                    DoAttack_DoRockCharge(npc, target, ref attackTimer, enraged);
                    break;
                case PolterghastAttackType.EtherealRoar:
                    DoAttack_EtherealRoar(npc, target, ref attackTimer, ref totalReleasedSouls, enraged);
                    break;
                case PolterghastAttackType.BeastialExplosion:
                    DoAttack_BeastialExplosion(npc, target, ref attackTimer, ref totalReleasedSouls);
                    break;
                case PolterghastAttackType.CloneSplit:
                    DoAttack_CloneSplit(npc, target, ref attackTimer, enraged);
                    break;
            }

            npc.damage = npc.hide ? 0 : npc.defDamage;
            attackTimer++;
            return false;
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.velocity *= 1.035f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.01f, 0f, 1f);
            npc.dontTakeDamage = true;

            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            PolterghastAttackType oldAttackState = (PolterghastAttackType)(int)npc.ai[0];
            PolterghastAttackType newAttackState = PolterghastAttackType.IdleMove;

            switch (oldAttackState)
            {
                case PolterghastAttackType.IdleMove:
                    newAttackState = PolterghastAttackType.ReleaseBurstsOfSouls;
                    break;
                case PolterghastAttackType.ReleaseBurstsOfSouls:
                    newAttackState = PolterghastAttackType.Impale;
                    break;
                case PolterghastAttackType.Impale:
                    newAttackState = PolterghastAttackType.RockCharge;
                    break;
                case PolterghastAttackType.RockCharge:
                    newAttackState = phase2 ? PolterghastAttackType.SpiritPetal : PolterghastAttackType.IdleMove;
                    break;
                case PolterghastAttackType.SpiritPetal:
                    newAttackState = PolterghastAttackType.EtherealRoar;
                    break;
                case PolterghastAttackType.EtherealRoar:
                    newAttackState = phase3 ? PolterghastAttackType.BeastialExplosion : PolterghastAttackType.IdleMove;
                    break;
                case PolterghastAttackType.BeastialExplosion:
                    newAttackState = PolterghastAttackType.CloneSplit;
                    break;
                case PolterghastAttackType.CloneSplit:
                    newAttackState = PolterghastAttackType.IdleMove;
                    break;
            }

            npc.TargetClosest();
            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void DoAttack_IdleMove(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float speed = MathHelper.Lerp(14f, 18f, 1f - lifeRatio);
            float inertia = MathHelper.Lerp(35f, 21f, 1f - lifeRatio);
            if (enraged)
            {
                speed *= 1.45f;
                inertia *= 0.66f;
            }
            if (BossRushEvent.BossRushActive)
            {
                speed *= 1.7f;
                inertia *= 0.6f;
            }

            if (!npc.WithinRange(target.Center, 150f) || npc.velocity == Vector2.Zero)
                npc.velocity = (npc.velocity * (inertia - 1f) + npc.SafeDirectionTo(target.Center) * speed) / inertia;
            else
                npc.velocity *= 1.01f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer >= 135f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ReleaseBurstsOfSouls(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls, bool enraged)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int shootRate = 13;
            if (lifeRatio < Phase2LifeRatio)
                shootRate = 9;
            if (lifeRatio < Phase3LifeRatio)
                shootRate = 6;
            float shootSpeed = MathHelper.Lerp(19f, 22.5f, 1f - lifeRatio);
            if (enraged)
            {
                shootRate = 5;
                shootSpeed = 40f;
            }
            if (BossRushEvent.BossRushActive)
            {
                shootRate = (int)(shootRate * 0.6f);
                shootSpeed *= 1.55f;
            }

            Vector2 destination = target.Center - Vector2.UnitY * 300f;
            destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;

            if (attackTimer % 160f > 60f)
            {
                // Release intertwined souls.
                if (attackTimer % 160f > 80f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                    {
                        Projectile[] soulPair = new Projectile[2];
                        for (int i = 0; i < soulPair.Length; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(1.05f) * shootSpeed * Main.rand.NextFloat(0.8f, 1.2f);
                            int soul = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 1.25f, shootVelocity, ModContent.ProjectileType<PairedSoul>(), 290, 0f, npc.target);
                            soulPair[i] = Main.projectile[soul];
                        }

                        soulPair[0].ai[1] = soulPair[1].whoAmI;
                        soulPair[1].ai[1] = soulPair[0].whoAmI;
                        totalReleasedSouls += 2;
                        npc.netUpdate = true;
                    }

                    if (attackTimer % 16f == 15f)
                        Main.PlaySound(SoundID.NPCHit36, target.Center);
                }

                // Slow down significantly.
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.05f);
                npc.velocity *= 0.98f;
            }
            else
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 18f, 0.4f);

            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            if (attackTimer >= 160f * 3f + 70f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_Impale(NPC npc, Player target, ref float attackTimer)
        {
            SelectNextAttack(npc);

            int impaleCount = 3;
            float impaleTime = 150f;

            if (attackTimer >= impaleTime * impaleCount + 15f)
                SelectNextAttack(npc);

            // Hover to the top left/right of the target before attacking.
            if (attackTimer % impaleTime < 45f)
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                Vector2 destination = target.Center - Vector2.UnitY * 250f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 170f;
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * 21.5f) / 11f;
                return;
            }

            // Slow down.
            npc.velocity *= 0.95f;
            if (attackTimer % impaleTime < 50f)
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Roar right before impaling.
            if (attackTimer % impaleTime == 90f)
            {
                var roar = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AbilitySounds/OmegaBlueAbility"), target.Center);
                if (roar != null)
                {
                    roar.Pitch = -0.425f;
                    roar.Volume = MathHelper.Clamp(roar.Volume * 1.5f, -1f, 1f);
                }
            }
        }

        public static void DoAttack_SpiritPetal(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls, bool enraged)
        {
            // Slow down and look at the target.
            npc.velocity *= 0.97f;
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Hover above the player prior to attacking.
            if (attackTimer < 50f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 250f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 170f;
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * 21.5f) / 11f;
            }

            // Create a light effect at the bottom of the screen.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 45f)
                Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<Light>(), 0, 0f);

            // Create a petal of released souls.
            int shootRate = enraged ? 5 : 7;
            if (BossRushEvent.BossRushActive)
                shootRate = 4;

            // Release a petal-like dance of souls. They spawn randomized, to make the pattern semi-inconsistent.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > 60f && attackTimer < 300f && attackTimer % shootRate == shootRate - 1f)
            {
                float offsetAngle = (float)Math.Sin(MathHelper.TwoPi * (attackTimer - 60f) / 128f) * MathHelper.Pi / 3f + Main.rand.NextFloatDirection() * 0.16f;
                Vector2 baseSpawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 44f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 leftVelocity = (MathHelper.TwoPi * i / 3f - offsetAngle).ToRotationVector2() * 22.5f;
                    Vector2 rightVelocity = (MathHelper.TwoPi * i / 3f + offsetAngle).ToRotationVector2() * 22.5f;

                    int soul = Utilities.NewProjectileBetter(baseSpawnPosition + leftVelocity * 2f, leftVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 290, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = 1f;

                    soul = Utilities.NewProjectileBetter(baseSpawnPosition + rightVelocity * 2f, rightVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 290, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = 1f;
                    totalReleasedSouls += 2f;
                }
            }

            if (totalReleasedSouls > 90f)
                totalReleasedSouls = 90f;

            // Do fade effect.
            if (attackTimer < 360f)
                npc.Opacity = Utils.InverseLerp(110f, 60f, attackTimer, true);
            else
                npc.Opacity = Utils.InverseLerp(360f, 400f, attackTimer, true);
            npc.hide = npc.Opacity < 0.25f;
            npc.dontTakeDamage = npc.hide;

            for (int i = 0; i < 15; i++)
            {
                Vector2 spawnOffsetDirection = Main.rand.NextVector2Unit();

                Dust ectoplasm = Dust.NewDustPerfect(npc.Center + spawnOffsetDirection * Main.rand.NextFloat(120f) * npc.scale, 264);
                ectoplasm.velocity = -Vector2.UnitY * MathHelper.Lerp(1f, 2.4f, Utils.InverseLerp(0f, 100f, npc.Distance(ectoplasm.position), true));
                ectoplasm.color = Color.Lerp(Color.Cyan, Color.Red, Main.rand.NextFloat(0.6f));
                ectoplasm.scale = 1.45f;
                ectoplasm.noLight = true;
                ectoplasm.noGravity = true;
            }

            if (attackTimer % 14f == 13f && attackTimer > 60f && attackTimer < 300f)
                Main.PlaySound(SoundID.NPCHit36, target.Center);

            if (attackTimer >= 440f && totalReleasedSouls <= 15f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_DoRockCharge(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float chargeSpeed = MathHelper.Lerp(24.5f, 31f, 1f - lifeRatio);
            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.45f;

            // Aim.
            if (attackTimer < 80f)
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * chargeSpeed) / 11f;
            }

            // Slow down.
            if (attackTimer > 80f && attackTimer < 105f)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
            }

            // Charge.
            if (attackTimer == 105f)
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                var roar = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AbilitySounds/OmegaBlueAbility"), target.Center);
                if (roar != null)
                {
                    roar.Pitch = -0.525f;
                    roar.Volume = MathHelper.Clamp(roar.Volume * 1.5f, -1f, 1f);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;
                }
            }

            // And release accelerating stones.
            if (attackTimer >= 105f && attackTimer < 160f)
            {
                npc.velocity *= 1.005f;

                // Release a burst of rocks.
                int shootRate = enraged ? 5 : 7;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 rockVelocity = npc.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY) * 2f;
                    Utilities.NewProjectileBetter(npc.Center + rockVelocity * 20f, rockVelocity, ModContent.ProjectileType<GhostlyRock>(), 280, 0f);
                    rockVelocity *= -1f;
                    Utilities.NewProjectileBetter(npc.Center + rockVelocity * 20f, rockVelocity, ModContent.ProjectileType<GhostlyRock>(), 280, 0f);
                }
            }

            // Slow down.
            if (attackTimer >= 160f)
            {
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.275f);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.0425f) * 0.96f;
            }

            if (attackTimer >= 205f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_EtherealRoar(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls, bool enraged)
        {
            int shootCount = enraged ? 5 : 3;
            int shootRate = enraged ? 45 : 75;
            float roarSpeed = 12.75f;

            if (BossRushEvent.BossRushActive)
            {
                shootCount = 7;
                shootRate = 35;
                roarSpeed *= 1.75f;
            }

            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float totalShotsDoneSoFar = ref npc.Infernum().ExtraAI[1];

            // Slow down and look at the target at the beginning.
            if (attackTimer < 30f)
                npc.velocity *= 0.95f;

            // Otherwise crawl into a corner and shoot things.
            else
            {
                Vector2 destination = target.Center - Vector2.UnitY * 345f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 280f;
                npc.velocity = (npc.velocity * 9f + npc.SafeDirectionTo(destination) * 20f) / 10f;
            }

            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Roar.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 55f)
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PolterghastWave>(), 0, 0f);

            if (attackTimer >= 50f)
                shootCounter++;

            // Roar, shoot spirits, and release a cluster of souls in the form of a roar thing idk lol.
            if (shootCounter >= shootRate)
            {
                var roar = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AbilitySounds/OmegaBlueAbility"), target.Center);
                if (roar != null)
                {
                    roar.Pitch = -0.525f;
                    roar.Volume = MathHelper.Clamp(roar.Volume * 1.5f, -1f, 1f);
                }

                // Release souls and a burst.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * roarSpeed, ModContent.ProjectileType<NecroplasmicRoar>(), 375, 0f);

                    for (int i = 0; i <= 10; i++)
                    {
                        Vector2 soulVelocity = npc.SafeDirectionTo(target.Center) * 15f;
                        soulVelocity = soulVelocity.RotatedBy(MathHelper.Lerp(-0.7f, 0.7f, (i + 0.5f) / 10f));
                        soulVelocity *= MathHelper.Lerp(1f, 1.4f, (float)Math.Sin(MathHelper.Pi * i / 10f));
                        Utilities.NewProjectileBetter(npc.Center, soulVelocity, ModContent.ProjectileType<WavySoul>(), 290, 0f);
                    }
                    totalReleasedSouls += 7;

                    shootCounter = 0f;
                    totalShotsDoneSoFar++;
                    npc.netUpdate = true;
                }
            }

            if (totalShotsDoneSoFar >= shootCount)
                SelectNextAttack(npc);
        }

        public static void DoAttack_BeastialExplosion(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls)
        {
            ref float middleAngle = ref npc.Infernum().ExtraAI[0];

            if (attackTimer < 60f && !npc.WithinRange(target.Center, 300f))
                npc.velocity = (npc.velocity * 19f + npc.SafeDirectionTo(target.Center) * 15f) / 20f;

            if (attackTimer > 60f)
                npc.velocity *= 0.965f;

            if (attackTimer <= 145f || attackTimer > 285f)
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.15f);
            else
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.0085f);

            // Roar.
            if (attackTimer == 145f)
            {
                var roar = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AbilitySounds/OmegaBlueAbility"), target.Center);
                if (roar != null)
                {
                    roar.Pitch = -0.525f;
                    roar.Volume = MathHelper.Clamp(roar.Volume * 1.5f, -1f, 1f);
                }

                middleAngle = npc.AngleTo(target.Center);
                npc.netUpdate = true;
            }

            // And release bursts of souls.
            if (attackTimer >= 155f && attackTimer < 285f && attackTimer % 3f == 2f)
            {
                if (attackTimer % 12f == 8f)
                    Main.PlaySound(SoundID.NPCHit36, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 35f + Main.rand.NextVector2Circular(20f, 20f);
                        Vector2 shootVelocity = (npc.rotation - MathHelper.PiOver2 + Main.rand.NextFloatDirection() * 0.81f).ToRotationVector2() * 23f;
                        Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 290, 0f);
                        totalReleasedSouls++;
                    }

                    npc.netUpdate = true;
                }
            }

            // Move around the player while reforming.
            if (attackTimer >= 285f && totalReleasedSouls > 12f)
            {
                Vector2 destination = target.Center + (attackTimer / 24f).ToRotationVector2() * 350f;
                Vector2 idealVelocity = (npc.velocity * 8f + npc.SafeDirectionTo(destination) * 18f) / 9f;
                npc.SimpleFlyMovement(idealVelocity, 0.4f);
            }
            else if (totalReleasedSouls <= 12f)
                npc.velocity *= 0.96f;

            if (attackTimer <= 310f)
            {
                npc.scale = MathHelper.Lerp(npc.scale, 0.25f, Utils.InverseLerp(155f, 300f, attackTimer, true));
                npc.Opacity = Utils.InverseLerp(0.78f, 0.95f, npc.scale, true);
            }
            else
            {
                npc.Opacity = Utils.InverseLerp(0.78f, 0.95f, npc.scale, true);
                if (totalReleasedSouls <= 0f || attackTimer > 540f)
                    SelectNextAttack(npc);
            }

            npc.hide = npc.Opacity < 0.45f;
            if (npc.Opacity < 0.7f)
            {
                npc.dontTakeDamage = true;
                for (int i = 0; i < 15; i++)
                {
                    Vector2 spawnOffsetDirection = Main.rand.NextVector2Unit();

                    Dust ectoplasm = Dust.NewDustPerfect(npc.Center + spawnOffsetDirection * Main.rand.NextFloat(110f) * npc.scale, 264);
                    ectoplasm.velocity = spawnOffsetDirection.RotatedBy(MathHelper.PiOver2 * Main.rand.NextBool(2).ToDirectionInt());
                    ectoplasm.velocity *= MathHelper.Lerp(1f, 2.4f, Utils.InverseLerp(0f, 100f, npc.Distance(ectoplasm.position), true));
                    ectoplasm.color = Color.Lerp(Color.Cyan, Color.Red, Main.rand.NextFloat(0.6f));
                    ectoplasm.scale = 1.45f;
                    ectoplasm.noLight = true;
                    ectoplasm.noGravity = true;
                }
            }
        }

        public static void DoAttack_CloneSplit(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            int totalCharges = 3;
            float chargeSpeed = enraged || BossRushEvent.BossRushActive ? 28f : 23f;
            int cloneType = ModContent.NPCType<PolterPhantom>();
            float adjustedTimer = attackTimer % 180f;

            IEnumerable<int> polterghasts = Main.npc.Take(Main.maxNPCs).
                Where(n => (n.type == npc.type || n.type == cloneType) && n.active).
                Select(n => n.whoAmI);

            if (adjustedTimer < 40f && !npc.WithinRange(target.Center, 300f))
            {
                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;

                npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(destination) * 18f) / 16f;
            }

            if (adjustedTimer == 20f)
            {
                // Summon three new clones.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int clone = NPC.NewNPC((int)npc.Center.X - 1, (int)npc.Center.Y, cloneType);

                        // An NPC must update once for it to recieve a whoAmI variable.
                        // Without this, the below IEnumerable collection would not incorporate this NPC.
                        // Yes, this is dumb.
                        Main.npc[clone].UpdateNPC(clone);
                    }
                }

                polterghasts = Main.npc.Take(Main.maxNPCs).
                    Where(n => n.type == cloneType && n.active).
                    Select(n => n.whoAmI);

                // Teleport around the player.
                Vector2 originalPosition = npc.Center;
                for (int i = 0; i < polterghasts.Count(); i++)
                {
                    Vector2 newPosition = originalPosition - Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / polterghasts.Count()) * 260f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.npc[polterghasts.ElementAt(i)].Center = newPosition;
                        Main.npc[polterghasts.ElementAt(i)].netUpdate = true;

                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PolterghastWave>(), 0, 0f);
                    }
                }
                var roar = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AbilitySounds/OmegaBlueAbility"), target.Center);
                if (roar != null)
                {
                    roar.Pitch = -0.525f;
                    roar.Volume = MathHelper.Clamp(roar.Volume * 1.5f, -1f, 1f);
                }
            }

            if (adjustedTimer > 50f && adjustedTimer < 105f)
            {
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.velocity *= 1.01f;
            }
            else
            {
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.325f);

                if (attackTimer > 50f)
                    npc.velocity *= 0.97f;
            }

            // Charge.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedTimer == 50f)
            {
                for (int i = 0; i < polterghasts.Count(); i++)
                {
                    Main.npc[polterghasts.ElementAt(i)].velocity = Main.npc[polterghasts.ElementAt(i)].SafeDirectionTo(target.Center + target.velocity * 10f) * chargeSpeed;
                    Main.npc[polterghasts.ElementAt(i)].netUpdate = true;
                }
            }

            if (attackTimer >= totalCharges * 180f)
                SelectNextAttack(npc);
        }

        #endregion AI

        #region Frames and Drawcode

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            bool inPhase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            bool enraged = npc.ai[3] == 1f;
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            Texture2D polterTexture = Main.npcTexture[npc.type];
            Texture2D polterGlowmaskEctoplasm = ModContent.GetTexture("CalamityMod/NPCs/Polterghast/PolterghastGlow");
            Texture2D polterGlowmaskHeart = ModContent.GetTexture("CalamityMod/NPCs/Polterghast/PolterghastGlow2");

            void drawInstance(Vector2 position, Color color)
            {
                spriteBatch.Draw(polterTexture, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(polterGlowmaskHeart, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(polterGlowmaskEctoplasm, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            if (inPhase3 || enraged)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                Color baseColor = Color.White;
                float drawOffsetFactor = MathHelper.Lerp(6.5f, 8.5f, (float)Math.Cos(Main.GlobalTime * 2.7f) * 0.5f + 0.5f) * npc.scale * npc.Opacity;
                float fadeFactor = 0.225f;
                if (enraged)
                {
                    drawOffsetFactor = MathHelper.Lerp(7f, 9.75f, (float)Math.Cos(Main.GlobalTime * 4.3f) * 0.5f + 0.5f) * npc.scale * npc.Opacity;
                    baseColor = Color.Red;
                    fadeFactor = 0.3f;
                }

                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTime * 1.9f).ToRotationVector2() * drawOffsetFactor;
                    drawInstance(baseDrawPosition + drawOffset, npc.GetAlpha(baseColor) * fadeFactor);
                }
            }
            spriteBatch.ResetBlendState();

            drawInstance(baseDrawPosition, npc.GetAlpha(Color.White));
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter % 7f == 6f)
                npc.frame.Y += frameHeight;

            int minFrame = 0;
            int maxFrame = 3;

            if (npc.life / (float)npc.lifeMax < Phase2LifeRatio)
            {
                minFrame = 4;
                maxFrame = 7;
            }
            if (npc.life / (float)npc.lifeMax < Phase3LifeRatio)
            {
                minFrame = 8;
                maxFrame = 11;
            }

            if (npc.frame.Y < frameHeight * minFrame)
                npc.frame.Y = frameHeight * minFrame;
            if (npc.frame.Y > frameHeight * maxFrame)
                npc.frame.Y = frameHeight * minFrame;
        }
        #endregion Frames and Drawcode
    }
}
