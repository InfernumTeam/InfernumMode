using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BoC
{
    public class BoCBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.BrainofCthulhu;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        #region Enumerations
        internal enum BoCAttackState
        {
            IdlyFloat,
            DiagonalCharge,
            BloodDashSwoop,
            CreeperBloodDripping,
            DashingIllusions,
            PsionicBombardment,
            SpinPull
        }
        #endregion

        #region AI

        public static int ElectricBoltDamage => 90;

        public static int BloodSpitDamage => 95;

        public static int IchorSpitDamage => 95;

        public static int PsionicOrbDamage => 100;

        public static int PsionicLightningBoltDamage => 140;

        public override bool PreAI(NPC npc)
        {
            NPC.crimsonBoss = npc.whoAmI;

            // Disable knockback since it fucks up the fight.
            npc.knockBackResist = 0f;

            // Emit a crimson light idly.
            Lighting.AddLight(npc.Center, Color.Crimson.ToVector3());

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Reset things.
            npc.damage = npc.alpha > 4 ? 0 : npc.defDamage;
            npc.defense = npc.defDefense - 2;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Lol. Lmao.
            if (target.HasBuff(BuffID.Electrified))
                target.ClearBuff(BuffID.Electrified);

            int creeperCount = 8;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[6];
            ref float hasCreatedCreepersFlag = ref npc.localAI[0];

            bool outOfBiome = !target.ZoneCrimson && !target.ZoneCorrupt && !BossRushEvent.BossRushActive;
            bool enraged = enrageTimer > 300f;
            bool phase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            bool phase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            enrageTimer = MathHelper.Clamp(enrageTimer + outOfBiome.ToDirectionInt(), 0f, 480f);

            npc.dontTakeDamage = enraged;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            // Summon creepers.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedCreepersFlag == 0f)
            {
                for (int i = 0; i < creeperCount; i++)
                {
                    Point spawnPosition = (npc.position + npc.Size * Main.rand.NextVector2Square(0f, 1f)).ToPoint();
                    int creeperAwMan = NPC.NewNPC(npc.GetSource_FromAI(), spawnPosition.X, spawnPosition.Y, NPCID.Creeper, ai0: i / (float)creeperCount);
                    if (Main.npc.IndexInRange(creeperAwMan))
                        Main.npc[creeperAwMan].velocity = Main.rand.NextVector2Circular(3f, 3f);
                }
                hasCreatedCreepersFlag = 1f;
            }

            switch ((BoCAttackState)(int)attackType)
            {
                case BoCAttackState.IdlyFloat:
                    DoAttack_IdlyFloat(npc, target, phase2, phase3, enraged, ref attackTimer);
                    break;
                case BoCAttackState.DiagonalCharge:
                    DoAttack_DiagonalCharge(npc, target, phase2, phase3, enraged, ref attackTimer);
                    break;
                case BoCAttackState.BloodDashSwoop:
                    DoAttack_BloodDashSwoop(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case BoCAttackState.CreeperBloodDripping:
                    DoAttack_CreeperBloodDripping(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case BoCAttackState.DashingIllusions:
                    DoAttack_DashingIllusions(npc, target, enraged, ref attackTimer);
                    break;
                case BoCAttackState.PsionicBombardment:
                    DoAttack_PsionicBombardment(npc, target, enraged, ref attackTimer);
                    break;
                case BoCAttackState.SpinPull:
                    DoAttack_SpinPull(npc, target, enraged, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoDespawnEffects(NPC npc)
        {
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 15f, 0.15f);
            npc.alpha = Utils.Clamp(npc.alpha + 20, 0, 255);
            npc.damage = 0;
            if (npc.timeLeft > 60)
                npc.timeLeft = 60;
        }

        public static void DoAttack_IdlyFloat(NPC npc, Player target, bool phase2, bool phase3, bool enraged, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            int teleportFadeTime = 50;
            int floatTime = 320;

            if (phase2)
            {
                teleportFadeTime -= 10;
                floatTime -= 40;
            }
            if (phase3)
            {
                teleportFadeTime -= 10;
                floatTime -= 40;
            }

            float teleportOffset = MathHelper.Lerp(600f, 475f, 1f - lifeRatio);
            if (!DoTeleportFadeEffect(npc, attackTimer, target.Center + Main.rand.NextVector2CircularEdge(teleportOffset, teleportOffset), teleportFadeTime))
                return;

            float floatSpeed = MathHelper.Lerp(5.8f, 8f, 1f - lifeRatio) + npc.Distance(target.Center) * 0.009f;
            if (enraged)
                floatSpeed *= 1.5f;
            if (BossRushEvent.BossRushActive)
                floatSpeed *= 1.85f;

            npc.velocity = npc.SafeDirectionTo(target.Center) * floatSpeed;

            // Stick to the target if close to them.
            if (npc.WithinRange(target.Center, 100f))
            {
                npc.velocity = Vector2.Zero;

                // Make the attack go much faster though to prevent annoying telefragging.
                attackTimer += 18f;
            }

            if (attackTimer >= floatTime + teleportFadeTime * 1.5f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_DiagonalCharge(NPC npc, Player target, bool phase2, bool phase3, bool enraged, ref float attackTimer)
        {
            int teleportFadeTime = 30;

            if (phase2)
                teleportFadeTime -= 4;
            if (phase3)
                teleportFadeTime -= 4;

            float horizontalTeleportDirection = -Math.Sign(target.velocity.X);
            if (horizontalTeleportDirection == 0f)
                horizontalTeleportDirection = Main.rand.NextBool(2).ToDirectionInt();
            ref float canFloatFlag = ref npc.Infernum().ExtraAI[0];
            Vector2 teleportDestination = target.Center + new Vector2(horizontalTeleportDirection * 400f, -210f);
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (canFloatFlag == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * Utils.GetLerpValue(120f, 108f, attackTimer, true) * 5f, 0.125f);
                if (attackTimer >= 120f)
                    GotoNextAttackState(npc);
                return;
            }
            if (attackTimer == teleportFadeTime + 25f)
            {
                SoundEngine.PlaySound(SoundID.Roar, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 16.75f;
                    if (enraged)
                        npc.velocity *= 1.56f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 2.15f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer > teleportFadeTime + 25f)
            {
                if (attackTimer <= teleportFadeTime + 80f)
                {
                    npc.velocity *= 1.0065f;

                    // Release ichor everywhere.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 5f == 4f)
                    {
                        Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8f);
                        Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(40f, 40f);
                        Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), IchorSpitDamage, 0f, -1, 0f, 1f);
                    }
                }
                else
                {
                    npc.velocity *= 0.95f;
                    if (npc.velocity.Length() < 1.25f && canFloatFlag == 0f)
                    {
                        canFloatFlag = 1f;
                        attackTimer = 60f;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        public static void DoAttack_BloodDashSwoop(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int teleportFadeTime = 46;

            if (phase2)
                teleportFadeTime -= 6;
            if (phase3)
                teleportFadeTime -= 6;

            Vector2 teleportDestination = target.Center + new Vector2(target.direction * -350f, -420f);
            if (Math.Abs(target.velocity.X) > 0f)
                teleportDestination = target.Center + new Vector2(Math.Sign(target.velocity.X) * -310f, -360f);

            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (attackTimer == teleportFadeTime + 10f)
            {
                SoundEngine.PlaySound(SoundID.Roar, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = Vector2.UnitY * 16f;
                    npc.direction = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;

                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 spawnPosition = npc.Center - Vector2.UnitY.RotatedByRandom(0.42f) * 12f;
                        Vector2 bloodVelocity = Utilities.GetProjectilePhysicsFiringVelocity(spawnPosition, target.Center, BloodGeyser2.Gravity, Main.rand.NextFloat(12f, 14f), out _);
                        bloodVelocity = bloodVelocity.RotatedByRandom(0.78f);
                        if (BossRushEvent.BossRushActive)
                            bloodVelocity *= 1.35f;

                        Utilities.NewProjectileBetter(spawnPosition, bloodVelocity, ModContent.ProjectileType<BloodGeyser2>(), BloodSpitDamage, 0f);
                    }
                }
            }

            // Swoop downward.
            if (attackTimer > teleportFadeTime + 10f)
            {
                if (Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)) < 0.96f)
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(npc.direction * 2f));
                else
                {
                    npc.velocity.X *= 0.988f;
                    npc.velocity.Y *= 0.96f;
                }

                if (attackTimer > teleportFadeTime + 100f)
                    GotoNextAttackState(npc);
            }
        }

        public static void DoAttack_CreeperBloodDripping(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int teleportFadeTime = 54;
            int shootTime = 380;

            if (phase2)
            {
                teleportFadeTime -= 10;
                shootTime -= 35;
            }
            if (phase3)
            {
                teleportFadeTime -= 8;
                shootTime -= 40;
            }

            Vector2 teleportDestination;
            int tries = 0;
            do
            {
                teleportDestination = target.Center + Main.rand.NextVector2CircularEdge(450f, 450f);
                tries++;

                if (tries > 500f)
                    break;
            }
            while (Collision.SolidCollision(teleportDestination - npc.Size * 0.5f, npc.width, npc.height));

            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            // Creepers do most of the interesting stuff with this attack.
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * (float)Math.Sin((attackTimer - 54f) / 24f) * 6f, 0.007f);

            if (attackTimer >= shootTime)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_DashingIllusions(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int teleportFadeTime = 35;
            int chargeDelay = 56;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            Vector2 teleportDestination = target.Center + Vector2.UnitY * 435f;
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
            {
                if (attackTimer >= teleportFadeTime && attackTimer <= 95f && npc.Opacity > 0.7f)
                    npc.Opacity = 0.7f;
                return;
            }

            npc.damage = npc.defDamage;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == teleportFadeTime + 1f)
            {
                for (int i = 1; i < 8; i++)
                {
                    int illusion = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BrainIllusion>());
                    Main.npc[illusion].ai[1] = MathHelper.TwoPi * i / 8f;
                }
            }

            if (attackTimer == teleportFadeTime + chargeDelay)
            {
                SoundEngine.PlaySound(SoundID.Roar, target.Center);

                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * 17f;
                if (enraged)
                    npc.velocity *= 1.55f;
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 1.8f;

                npc.netUpdate = true;
            }

            if (attackTimer > teleportFadeTime + chargeDelay + 50f)
            {
                npc.velocity *= 0.97f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.05f, 0f, 1f);

                if (npc.Opacity <= 0f)
                {
                    chargeCounter++;

                    if (chargeCounter >= 2f)
                        GotoNextAttackState(npc);
                    else
                        attackTimer = teleportFadeTime + 28f;
                    npc.Opacity = 1f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_PsionicBombardment(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int teleportFadeTime = 50;
            Vector2 teleportDestination = target.Center - Vector2.UnitY * 350f;
            ref float cyanAuraStrength = ref npc.localAI[1];

            while (Collision.SolidCollision(teleportDestination - npc.Size * 0.5f, npc.width, npc.height))
                teleportDestination.Y -= 8f;

            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.1f);
            if (attackTimer >= 70f)
                npc.velocity *= 0.94f;

            cyanAuraStrength = Utils.GetLerpValue(105f, 125f, attackTimer, true) * Utils.GetLerpValue(445f, 425f, attackTimer, true);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (attackTimer == 130f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Top + Vector2.UnitY * 16f;
                    bool shouldUseUndergroundAI = target.Center.Y / 16f < Main.worldSurface || Collision.SolidCollision(npc.Center - Vector2.One * 24f, 48, 48);
                    if (lifeRatio < 0.2f)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(orb =>
                        {
                            orb.ModProjectile<PsionicOrb>().UseUndergroundAI = shouldUseUndergroundAI;
                        });
                        Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY.RotatedBy(-0.17f) * -5f, ModContent.ProjectileType<PsionicOrb>(), PsionicOrbDamage, 0f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(orb =>
                        {
                            orb.ModProjectile<PsionicOrb>().UseUndergroundAI = shouldUseUndergroundAI;
                        });
                        Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY.RotatedBy(0.17f) * -5f, ModContent.ProjectileType<PsionicOrb>(), PsionicOrbDamage, 0f);
                    }
                    else
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(orb =>
                        {
                            orb.ModProjectile<PsionicOrb>().UseUndergroundAI = shouldUseUndergroundAI;
                        });
                        Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * -6f, ModContent.ProjectileType<PsionicOrb>(), PsionicOrbDamage, 0f);
                    }
                }
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item92, target.Center);
            }

            if (attackTimer >= (enraged ? 335f : 450f))
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<PsionicOrb>(), ModContent.ProjectileType<PsionicLightningBolt>(), ProjectileID.MartianTurretBolt);
                GotoNextAttackState(npc);
            }
        }

        public static void DoAttack_SpinPull(NPC npc, Player target, bool enraged, ref float attackTimer)
        {
            int teleportFadeTime = 50;
            float spinRadius = 395f;
            float spinTime = 120f;
            ref float spinAngle = ref npc.Infernum().ExtraAI[0];
            if (attackTimer == 1f)
                spinAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 teleportDestination = target.Center - Vector2.UnitY.RotatedBy(spinAngle) * spinRadius;
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (attackTimer > teleportFadeTime * 1.5f)
            {
                spinAngle += MathHelper.TwoPi * 2f / spinTime * Utils.GetLerpValue(teleportFadeTime * 1.5f + spinTime, teleportFadeTime * 1.5f + spinTime - 30f, attackTimer, true);
                if (Main.netMode != NetmodeID.MultiplayerClient && (int)attackTimer % 16f == 15f)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BrainIllusion2>(), npc.whoAmI);
            }

            npc.localAI[1] = (float)Math.Sin(Utils.GetLerpValue((int)(teleportFadeTime * 1.5f) + spinTime - 20f, (int)(teleportFadeTime * 1.5f) + spinTime + 45f, attackTimer, true) * MathHelper.Pi);
            if (attackTimer == (int)(teleportFadeTime * 1.5f) + spinTime + 15f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 26f;
                if (enraged)
                    npc.velocity *= 1.425f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != ModContent.NPCType<BrainIllusion2>())
                        continue;

                    Main.npc[i].velocity = Main.npc[i].SafeDirectionTo(target.Center) * npc.velocity.Length();
                    Main.npc[i].netUpdate = true;
                }

                SoundEngine.PlaySound(SoundID.ForceRoarPitched, target.Center);
            }

            if (attackTimer < (int)(teleportFadeTime * 1.5f) + spinTime)
                npc.Center = teleportDestination;
            else
                npc.velocity *= 0.98f;

            if (attackTimer >= teleportFadeTime * 1.5f + spinTime + 70f)
            {
                npc.velocity *= 0.9f;
                npc.Opacity -= 0.05f;
                if (npc.Opacity <= 0.05f)
                    GotoNextAttackState(npc);
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal const float Phase2LifeRatio = 0.75f;
        internal const float Phase3LifeRatio = 0.45f;
        public static void GotoNextAttackState(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            npc.Opacity = 0f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            BoCAttackState oldAttackType = (BoCAttackState)(int)npc.ai[0];
            BoCAttackState newAttackType = BoCAttackState.IdlyFloat;
            switch (oldAttackType)
            {
                case BoCAttackState.IdlyFloat:
                    newAttackType = lifeRatio < Phase2LifeRatio ? BoCAttackState.DiagonalCharge : BoCAttackState.BloodDashSwoop;
                    break;
                case BoCAttackState.DiagonalCharge:
                    newAttackType = BoCAttackState.BloodDashSwoop;
                    break;
                case BoCAttackState.BloodDashSwoop:
                    newAttackType = BoCAttackState.CreeperBloodDripping;
                    break;
                case BoCAttackState.CreeperBloodDripping:
                    newAttackType = lifeRatio < Phase3LifeRatio ? Main.rand.NextBool() ? BoCAttackState.PsionicBombardment : BoCAttackState.DashingIllusions : BoCAttackState.IdlyFloat;
                    break;
                case BoCAttackState.DashingIllusions:
                    newAttackType = BoCAttackState.PsionicBombardment;
                    break;
                case BoCAttackState.PsionicBombardment:
                    newAttackType = BoCAttackState.SpinPull;
                    break;
                case BoCAttackState.SpinPull:
                    newAttackType = Main.rand.NextBool(2) ? BoCAttackState.DiagonalCharge : BoCAttackState.BloodDashSwoop;
                    break;
            }

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static bool DoTeleportFadeEffect(NPC npc, float time, Vector2 teleportDestination, int teleportFadeTime)
        {
            if (npc.Opacity < 1f)
                npc.damage = 0;

            if (npc.life / (float)npc.lifeMax < Phase3LifeRatio)
                teleportFadeTime = (int)(teleportFadeTime * 0.6f);

            // Fade out and teleport after a bit.
            if (time <= teleportFadeTime)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, time / teleportFadeTime);

                // Teleport when completely transparent.
                if (Main.netMode != NetmodeID.MultiplayerClient && time == teleportFadeTime)
                {
                    npc.Center = teleportDestination;

                    // And bring creepers along with because their re-adjustment motion in the base game is unpredictable and unpleasant.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type != NPCID.Creeper || !Main.npc[i].active)
                            continue;

                        Main.npc[i].Center = npc.Center + Main.rand.NextVector2CircularEdge(3f, 3f);
                        Main.npc[i].netUpdate = true;
                    }
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.94f;
                return false;
            }

            // Fade back in after teleporting.
            if (time > teleportFadeTime && time <= teleportFadeTime * 1.5f)
                npc.Opacity = MathHelper.Lerp(0f, 1f, Utils.GetLerpValue(teleportFadeTime, teleportFadeTime * 1.5f, time, true));
            return true;
        }
        #endregion AI Utility Methods

        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            frame.Y += texture.Height / Main.npcFrameCount[npc.type] * 4;

            void drawInstance(Vector2 drawPosition, Color color, float scale)
            {
                drawPosition -= Main.screenPosition;
                Main.spriteBatch.Draw(texture, drawPosition, frame, color, npc.rotation, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            float cyanAuraStrength = npc.localAI[1];
            if (cyanAuraStrength > 0f)
            {
                float scale = npc.scale * MathHelper.Lerp(0.9f, 1.06f, cyanAuraStrength);
                Color auraColor = Color.Lerp(Color.Transparent, Color.Cyan, cyanAuraStrength) * npc.Opacity * 0.3f;
                auraColor.A = 0;

                for (int i = 0; i < 7; i++)
                {
                    Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 7f + Main.GlobalTimeWrappedHourly * 4.3f).ToRotationVector2() * cyanAuraStrength * 4f;
                    drawInstance(drawPosition, auraColor, scale);
                }
            }
            drawInstance(npc.Center, npc.GetAlpha(lightColor), npc.scale);
            return false;
        }

        #endregion Drawing

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips(bool hatGirl)
        {
            yield return n =>
            {
                if (hatGirl)
                    return "The Brain of Cthulhu uses a lot of prediction and deception in its attacks, so play extra smart!";
                return string.Empty;
            };
            yield return n =>
            {
                if (hatGirl)
                    return "The Brain is going to try to decieve you with various mind games, keep your eyes on the real one!";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
