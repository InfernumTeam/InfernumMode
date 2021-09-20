using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using InfernumMode.Dusts;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using BrimmyNPC = CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneElementalBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<BrimmyNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public const float BaseDR = 0.25f;
        public const float InvincibleDR = 0.99999f;

        #region Enumerations
        public enum BrimmyAttackType
        {
            FlameTeleportBombardment,
            BrimstoneRoseBurst,
            FlameChargeSkullBlasts,
            GrimmBulletHellCopyLmao,
            EyeLaserbeams
        }

        public enum BrimmyFrameType
        {
            TypicalFly,
            OpenEye,
            ClosedShell
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.brimstoneElemental = npc.whoAmI;

            // Reset DR and its breakability every frame.
            npc.Calamity().DR = BaseDR;
            npc.Calamity().unbreakableDR = false;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -8f, 0.25f);
                if (!npc.WithinRange(target.Center, 880f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;
            bool pissedOff = target.Bottom.Y < (Main.maxTilesY - 200f) * 16f;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float spawnAnimationTimer = ref npc.ai[2];
            ref float frameType = ref npc.localAI[0];

            npc.dontTakeDamage = pissedOff;

            if (spawnAnimationTimer < 240f)
            {
                DoBehavior_SpawnAnimation(npc, target, spawnAnimationTimer, ref frameType);
                spawnAnimationTimer++;
                return false;
            }

            switch ((BrimmyAttackType)(int)attackType)
            {
                case BrimmyAttackType.FlameTeleportBombardment:
                    DoBehavior_FlameTeleportBombardment(npc, target, lifeRatio, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
                    break;
                case BrimmyAttackType.BrimstoneRoseBurst:
                    DoBehavior_BrimstoneRoseBurst(npc, target, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
                    break;
                case BrimmyAttackType.FlameChargeSkullBlasts:
                    DoBehavior_FlameChargeSkullBlasts(npc, target, lifeRatio, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
                    break;
                case BrimmyAttackType.GrimmBulletHellCopyLmao:
                    DoBehavior_CocoonBulletHell(npc, target, lifeRatio, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
                    break;
                case BrimmyAttackType.EyeLaserbeams:
                    DoBehavior_EyeLaserbeams(npc, target, lifeRatio, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, float spawnAnimationTimer, ref float frameType)
        {
            frameType = (int)BrimmyFrameType.ClosedShell;
            npc.velocity = Vector2.UnitY * Utils.InverseLerp(135f, 45f, spawnAnimationTimer, true) * -4f;
            npc.Opacity = Utils.InverseLerp(0f, 40f, spawnAnimationTimer, true);

            // Adjust sprite direction to look at the player.
            if (MathHelper.Distance(target.Center.X, npc.Center.X) > 45f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            int brimstoneDustCount = (int)MathHelper.Lerp(2f, 8f, npc.Opacity);
            for (int i = 0; i < brimstoneDustCount; i++)
            {
                Dust brimstoneFire = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.5f, 267);
                brimstoneFire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.4f, 0.9f));
                brimstoneFire.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 5.4f);
                brimstoneFire.scale = MathHelper.SmoothStep(0.9f, 1.56f, Utils.InverseLerp(2f, 5.4f, brimstoneFire.velocity.Y, true));
                brimstoneFire.noGravity = true;
            }

            npc.Calamity().DR = InvincibleDR;
            npc.Calamity().unbreakableDR = true;
        }

        public static void DoBehavior_FlameTeleportBombardment(NPC npc, Player target, float lifeRatio, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
            int bombardCount = lifeRatio < 0.5f ? 7 : 6;
            int bombardTime = 75;
            int fireballShootRate = lifeRatio < 0.5f ? 9 : 12;
            int fadeOutTime = (int)MathHelper.Lerp(48f, 27f, 1f - lifeRatio);
            float horizontalTeleportOffset = MathHelper.Lerp(985f, 850f, 1f - lifeRatio);
            float verticalDestinationOffset = MathHelper.Lerp(600f, 475f, 1f - lifeRatio);
            Vector2 verticalDestination = target.Center - Vector2.UnitY * verticalDestinationOffset;
            ref float bombardCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackState = ref npc.Infernum().ExtraAI[1];

            if (shouldBeBuffed)
            {
                bombardTime -= 35;
                fadeOutTime = (int)(fadeOutTime * 0.6);
                horizontalTeleportOffset *= 0.8f;
                fireballShootRate -= 4;
            }
            if (pissedOff)
            {
                fadeOutTime = (int)(fadeOutTime * 0.45);
                horizontalTeleportOffset *= 0.7f;
                fireballShootRate = 4;
            }

            switch ((int)attackState)
            {
                // Fade out and disappear into flames.
                case 0:
                    npc.velocity *= 0.92f;
                    npc.rotation = npc.velocity.X * 0.04f;
                    npc.Opacity = MathHelper.Clamp(npc.Opacity - 1f / fadeOutTime, 0f, 1f);

                    int brimstoneDustCount = (int)MathHelper.Lerp(2f, 8f, npc.Opacity);
                    for (int i = 0; i < brimstoneDustCount; i++)
                    {
                        Dust brimstoneFire = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.5f, 267);
                        brimstoneFire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.4f, 0.9f));
                        brimstoneFire.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 5.4f);
                        brimstoneFire.scale = MathHelper.SmoothStep(0.9f, 1.56f, Utils.InverseLerp(2f, 5.4f, brimstoneFire.velocity.Y, true));
                        brimstoneFire.noGravity = true;
                    }

                    // Go to the next attack state and teleport once completely invisible.
                    if (npc.Opacity <= 0f)
                    {
                        Vector2 teleportOffset = Vector2.UnitX * horizontalTeleportOffset * (bombardCounter % 2f == 0f).ToDirectionInt();
                        attackTimer = 0f;
                        attackState++;
                        npc.Center = target.Center + teleportOffset;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = npc.SafeDirectionTo(verticalDestination) * npc.Distance(verticalDestination) / bombardTime;
                        npc.netUpdate = true;
                    }

                    // Use the closed shell animation.
                    frameType = (int)BrimmyFrameType.ClosedShell;
                    break;

                // Rapidly fade back in and move.
                case 1:
                    npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.12f, 0f, 1f);
                    npc.rotation = npc.velocity.X * 0.04f;

                    if (attackTimer % fireballShootRate == fireballShootRate - 1f)
                    {
                        Main.PlaySound(SoundID.Item20, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int skullDamage = shouldBeBuffed ? 310 : 125;
                            skullDamage += (int)((1f - lifeRatio) * 35);

                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center);
                            int skull = Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<HomingBrimstoneSkull>(), skullDamage, 0f);
                            if (Main.projectile.IndexInRange(skull))
                                Main.projectile[skull].ai[0] = pissedOff ? -8f : (attackTimer - bombardTime) / 2;
                        }
                    }

                    if (attackTimer >= bombardTime)
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        bombardCounter++;

                        if (bombardCounter >= bombardCount)
                        {
                            bombardCounter = 0f;
                            SelectNewAttack(npc);
                        }

                        npc.netUpdate = true;
                    }

                    // Use the flying animation.
                    frameType = (int)BrimmyFrameType.TypicalFly;
                    break;
            }
        }

        public static void DoBehavior_BrimstoneRoseBurst(NPC npc, Player target, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
            // Use the flying animation.
            frameType = (int)BrimmyFrameType.TypicalFly;

            int totalRosesToSpawn = shouldBeBuffed ? 6 : 5;
            int castingAnimationTime = shouldBeBuffed ? 70 : 110;
            if (pissedOff)
                totalRosesToSpawn += 5;
            Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * 20f, -70f);
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float roseSpawnBaseAngle = ref npc.Infernum().ExtraAI[1];
            ref float roseSpawnCenterX = ref npc.Infernum().ExtraAI[2];
            ref float roseSpawnCenterY = ref npc.Infernum().ExtraAI[3];
            ref float roseCreationCounter = ref npc.Infernum().ExtraAI[4];

            // Adjust sprite direction to look at the player.
            if (MathHelper.Distance(target.Center.X, npc.Center.X) > 45f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.rotation = npc.velocity.X * 0.04f;

            switch ((int)attackState)
            {
                case 0:
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * new Vector2(11f, 6.67f), 0.18f);
                    if (attackTimer >= 125f || npc.WithinRange(target.Center, 105f))
                    {
                        attackTimer = 0f;
                        attackState = 1f;
                        roseCreationCounter++;

                        if (roseCreationCounter >= 4f)
                        {
                            roseCreationCounter = 0f;
                            SelectNewAttack(npc);
                        }
                        npc.netUpdate = true;
                    }
                    break;
                case 1:
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.4f) * 0.96f;

                    // Initialize the rose spawn offset angle and spawn position before performing the attack.
                    if (attackTimer == castingAnimationTime - 15f)
                    {
                        roseSpawnCenterX = target.Center.X + target.velocity.X * 16f;
                        roseSpawnCenterY = target.Center.Y + target.velocity.Y * 15f;
                        roseSpawnBaseAngle = Main.rand.NextFloat(-0.61f, 0.61f);
                        npc.netUpdate = true;
                    }

                    // Create the charge dust.
                    int dustCount = attackTimer >= 35f ? 2 : 1;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float dustScale = i % 2 == 1 ? 1.65f : 0.8f;

                        Vector2 dustSpawnPosition = eyePosition + Main.rand.NextVector2CircularEdge(16f, 16f);
                        Dust chargeDust = Dust.NewDustPerfect(dustSpawnPosition, 267);
                        chargeDust.velocity = (eyePosition - dustSpawnPosition).SafeNormalize(Vector2.UnitY) * (dustCount == 2 ? 3.5f : 2.8f);
                        chargeDust.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.222f, 0.777f));
                        chargeDust.scale = dustScale;
                        chargeDust.noGravity = true;
                    }

                    if (attackTimer >= castingAnimationTime)
                    {
                        Main.PlaySound(SoundID.Item72, npc.Center);

                        for (int i = 0; i < totalRosesToSpawn; i++)
                        {
                            // Generate sets of points where the roses will be spawned.
                            // This relies on a hefty amount of trig to avoid the excessive use of ExtraAI slots.
                            float spawnRotationAngle = MathHelper.Lerp(-1.35f, 1.35f, i / (float)totalRosesToSpawn);
                            float baseVerticalSpawnOffset = (float)Math.Sin((roseSpawnBaseAngle + spawnRotationAngle) * 56f) * 125f + 415f;
                            Vector2 roseSpawnPosition = new Vector2(roseSpawnCenterX, roseSpawnCenterY) - Vector2.UnitY.RotatedBy(roseSpawnBaseAngle + spawnRotationAngle) * baseVerticalSpawnOffset;

                            Dust.QuickDustLine(eyePosition, roseSpawnPosition, 135f, Color.Red);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int rose = Utilities.NewProjectileBetter(roseSpawnPosition, Vector2.Zero, ModContent.ProjectileType<BrimstoneRose>(), 0, 0f);
                                if (Main.projectile.IndexInRange(rose))
                                    Main.projectile[rose].ai[1] = pissedOff.ToInt();
                            }
                        }

                        attackTimer = 0f;
                        attackState = 0f;
                        roseSpawnBaseAngle = 0f;
                        roseSpawnCenterX = 0f;
                        roseSpawnCenterY = 0f;

                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_FlameChargeSkullBlasts(NPC npc, Player target, float lifeRatio, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
            // Use the open eye fly animation.
            frameType = (int)BrimmyFrameType.OpenEye;

            int chargeTime = (int)MathHelper.Lerp(240f, 150f, 1f - lifeRatio);
            int totalBursts = 3;
            int burstRate = 90;
            Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * 20f, -70f);
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float burstCounter = ref npc.Infernum().ExtraAI[1];

            if (shouldBeBuffed)
            {
                chargeTime -= 45;
                totalBursts = 4;
                burstRate -= 20;
            }

            switch ((int)attackState)
            {
                // Teleport near the target and immediately go to the next attack state.
                case 0:
                    attackState++;
                    attackTimer = 0f;
                    Vector2 teleportDestination = target.Center - Vector2.UnitY * 350f;
                    CreateTeleportTelegraph(npc.Center, teleportDestination, 250);
                    npc.Center = target.Center - Vector2.UnitY * 325f;
                    npc.netUpdate = true;
                    break;

                // Charge prior to firing.
                case 1:
                    // Create the charge dust.
                    npc.velocity *= 0.5f;
                    npc.rotation = npc.velocity.X * 0.04f;

                    int dustCount = attackTimer >= 100f ? 2 : 1;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float dustScale = i % 2 == 1 ? 1.5f : 0.8f;

                        Vector2 dustSpawnPosition = eyePosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(45f, 60f);
                        Dust chargeDust = Dust.NewDustPerfect(dustSpawnPosition, ModContent.DustType<BrimstoneCinderDust>());
                        chargeDust.velocity = (eyePosition - dustSpawnPosition).SafeNormalize(Vector2.UnitY) * (dustCount == 2 ? 7f : 5.6f);
                        chargeDust.scale = dustScale;
                        chargeDust.noGravity = true;
                    }

                    // Adjust sprite direction to look at the player.
                    if (MathHelper.Distance(target.Center.X, npc.Center.X) > 45f)
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                    // Explode and go to the next attack state once done charging.
                    if (attackTimer >= chargeTime)
                    {
                        // Look at the player.
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                        Utilities.CreateGenericDustExplosion(eyePosition, ModContent.DustType<BrimstoneCinderDust>(), 25, 7f, 1.3f);
                        Utilities.CreateGenericDustExplosion(eyePosition, (int)CalamityDusts.Brimstone, 35, 5.5f, 1.4f);

                        attackState++;
                        attackTimer = 0f;
                    }
                    break;

                // Release bursts of skulls and hellblasts in bursts.
                case 2:
                    if (attackTimer >= burstRate)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Release waving skulls.
                            int skullCount = (int)MathHelper.Lerp(4f, 9f, 1f - lifeRatio);
                            int skullDamage = shouldBeBuffed ? 310 : 125;
                            skullDamage += (int)((1f - lifeRatio) * 35);

                            if (pissedOff)
                                skullCount += 8;

                            for (int i = 0; i < skullCount; i++)
                            {
                                float offsetAngle = MathHelper.Lerp(-0.74f, 0.74f, i / (float)(skullCount - 1f));
                                Vector2 shootVelocity = (target.Center - eyePosition).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * 10f;
                                Utilities.NewProjectileBetter(eyePosition, shootVelocity, ModContent.ProjectileType<BrimstoneSkull>(), skullDamage, 0f);
                            }

                            // And hellblasts.
                            for (int i = 0; i < skullCount / 2 + 2; i++)
                            {
                                float offsetAngle = Main.rand.NextFloat(-0.89f, 0.89f);
                                Vector2 shootVelocity = (target.Center - eyePosition).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * Main.rand.NextFloat(2f, 3f);
                                Utilities.NewProjectileBetter(eyePosition, shootVelocity, ModContent.ProjectileType<BrimstoneHellblast>(), skullDamage, 0f);
                            }
                        }

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        attackTimer = 0f;
                        burstCounter++;

                        if (burstCounter >= totalBursts)
                        {
                            attackState = 0f;
                            burstCounter = 0f;
                            SelectNewAttack(npc);
                        }
                    }
                    break;
            }
        }

        public static void DoBehavior_CocoonBulletHell(NPC npc, Player target, float lifeRatio, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
            // Use the cocoon animation.
            frameType = (int)BrimmyFrameType.ClosedShell;
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            int fireReleaseRate = lifeRatio < 0.5f ? 5 : 7;
            int bulletHellTime = 480;
            if (shouldBeBuffed)
            {
                fireReleaseRate -= 1;
                bulletHellTime += 60;
            }
            if (pissedOff)
                fireReleaseRate = 3;

            // Rapidly slow down.
            npc.velocity *= 0.8f;
            npc.rotation = npc.velocity.X * 0.04f;

            npc.Calamity().DR = InvincibleDR;
            npc.Calamity().unbreakableDR = true;

            // Have a small delay prior to the bullet hell to allow the target to prepare.
            if (attackTimer < 105f)
                return;

            // Release the bullet hell cinders.
            shootTimer++;
            if (shootTimer > fireReleaseRate && attackTimer < bulletHellTime + 60f)
            {
                Main.PlaySound(SoundID.Item100, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireDamage = shouldBeBuffed ? 320 : 130;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 shootDirection = Main.rand.NextVector2Unit();
                        Vector2 fireSpawnPosition = npc.Center + npc.Size * shootDirection * 0.45f;
                        Vector2 fireShootVelocity = shootDirection * 15f;
                        Utilities.NewProjectileBetter(fireSpawnPosition, fireShootVelocity, ModContent.ProjectileType<BrimstoneFireball>(), fireDamage, 0f);
                    }

                    // Sometimes release predictive darts.
                    if (Main.rand.NextBool(8))
                    {
                        int dartCount = 3;
                        for (int i = 0; i < dartCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.64f, 0.64f, i / (float)(dartCount - 1f));
                            Vector2 dartVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 25f).RotatedBy(offsetAngle) * 14f;
                            Utilities.NewProjectileBetter(npc.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), fireDamage, 0f);
                        }
                    }
                }
                shootTimer = 0f;
            }

            if (attackTimer > bulletHellTime + 120f)
            {
                shootTimer = 0f;
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_EyeLaserbeams(NPC npc, Player target, float lifeRatio, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
            // Use the open eye fly animation.
            frameType = (int)BrimmyFrameType.OpenEye;

            int hoverTime = (int)MathHelper.Lerp(105f, 200f, 1f - lifeRatio);
            int totalLaserbeamBursts = 4;
            Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection * 20f, -70f);

            if (pissedOff || shouldBeBuffed)
                hoverTime -= 25;

            ref float attackState = ref npc.Infernum().ExtraAI[0];

            switch ((int)attackState)
            {
                // Teleport near the target and immediately go to the next attack state.
                case 0:
                    attackState++;
                    attackTimer = 0f;
                    Vector2 teleportDestination = target.Center - Vector2.UnitY * 350f;
                    CreateTeleportTelegraph(npc.Center, teleportDestination, 250);
                    npc.Center = target.Center - Main.rand.NextVector2CircularEdge(300f, 300f);
                    npc.netUpdate = true;
                    break;

                // Hover near the player for a bit and create charge dust.
                // This serves as a sort of telegraph as well as a way for Brimmy to redirect.
                case 1:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 300f, -180f);
                    Vector2 minVelocity = npc.SafeDirectionTo(target.Center) * MathHelper.Min(8f, npc.Distance(hoverDestination));
                    Vector2 maxVelocity = ((hoverDestination - npc.Center) / 30f).ClampMagnitude(0f, 36f);

                    // Hover more quickly if far from the destination.
                    npc.velocity = Vector2.Lerp(minVelocity, maxVelocity, Utils.InverseLerp(150f, 425f, npc.Distance(hoverDestination), true));
                    npc.rotation = npc.velocity.X * 0.04f;

                    // Create the charge dust.
                    int dustCount = attackTimer >= 100f ? 2 : 1;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float dustScale = i % 2 == 1 ? 1.5f : 0.8f;

                        Vector2 dustSpawnPosition = eyePosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(45f, 60f);
                        Dust chargeDust = Dust.NewDustPerfect(dustSpawnPosition, ModContent.DustType<BrimstoneCinderDust>());
                        chargeDust.velocity = (eyePosition - dustSpawnPosition).SafeNormalize(Vector2.UnitY) * (dustCount == 2 ? 7f : 5.6f);
                        chargeDust.scale = dustScale;
                        chargeDust.noGravity = true;
                    }

                    // Go to the next attack state after hovering for a small amount of time.
                    if (attackTimer >= hoverTime)
                    {
                        attackState++;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Sit for a short amount of time and release laserbeams.
                case 2:
                    if (attackTimer < 35f)
                        npc.velocity *= 0.9f;
                    else
                    {
                        npc.velocity = Vector2.Zero;

                        int laserbeamDamage = shouldBeBuffed ? 450 : 205;
                        if ((attackTimer - 40f) % 120f == 119f)
                        {
                            Main.PlaySound(SoundID.Item74, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 deathrayDirection = (target.Center - eyePosition).SafeNormalize(Vector2.UnitX * npc.spriteDirection);
                                Utilities.NewProjectileBetter(eyePosition, deathrayDirection, ModContent.ProjectileType<BrimstoneDeathray>(), laserbeamDamage, 0f);
                            }
                        }

                        if (attackTimer >= (totalLaserbeamBursts + 0.85f) * 120f + 40f)
                        {
                            attackState = 0f;
                            SelectNewAttack(npc);
                        }
                    }

                    npc.rotation = npc.velocity.X * 0.04f;
                    break;
            }

            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
        }

        public static void CreateTeleportTelegraph(Vector2 start, Vector2 end, int dustCount, bool canCreateDust = true)
        {
            if (canCreateDust)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust magic = Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(50f, 50f), 264);
                    magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 4f);
                    magic.color = Color.Red;
                    magic.scale = 1.4f;
                    magic.fadeIn = 0.5f;
                    magic.noGravity = true;
                    magic.noLight = true;

                    magic = Dust.CloneDust(magic);
                    magic.position = end + Main.rand.NextVector2Circular(50f, 50f);
                }
            }

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustDrawPosition = Vector2.Lerp(start, end, i / (float)dustCount);

                Dust magic = Dust.NewDustPerfect(dustDrawPosition, 267);
                magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.235f);
                magic.color = Color.OrangeRed;
                magic.color.A = 0;
                magic.scale = 0.8f;
                magic.fadeIn = 1.4f;
                magic.noGravity = true;
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            List<BrimmyAttackType> possibleAttacks = new List<BrimmyAttackType>
            {
                BrimmyAttackType.FlameChargeSkullBlasts,
                BrimmyAttackType.BrimstoneRoseBurst,
                BrimmyAttackType.FlameChargeSkullBlasts,
                BrimmyAttackType.GrimmBulletHellCopyLmao
            };
            possibleAttacks.AddWithCondition(BrimmyAttackType.EyeLaserbeams, lifeRatio < 0.5f);

            possibleAttacks.Remove((BrimmyAttackType)(int)npc.ai[0]);

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing
        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;

            switch ((BrimmyFrameType)(int)npc.localAI[0])
            {
                case BrimmyFrameType.TypicalFly:
                    if (npc.frameCounter >= 13f)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0f;
                    }
                    if (npc.frame.Y >= frameHeight * 4)
                        npc.frame.Y = 0;
                    break;
                case BrimmyFrameType.OpenEye:
                    if (npc.frameCounter >= 13f)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0f;
                    }
                    if (npc.frame.Y >= frameHeight * 8 || npc.frame.Y < frameHeight * 4)
                        npc.frame.Y = frameHeight * 4;
                    break;
                case BrimmyFrameType.ClosedShell:
                    if (npc.frameCounter >= 8f)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0f;
                    }
                    if (npc.frame.Y >= frameHeight * 12 || npc.frame.Y < frameHeight * 8)
                        npc.frame.Y = frameHeight * 8;
                    break;
            }
        }
        #endregion
    }
}
