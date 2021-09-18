using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;


using BrimmyNPC = CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental;

namespace InfernumMode.FuckYouModeAIs.BrimstoneElemental
{
    /*
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
            BrimstoneRoseWither
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

            // Reset DR and every frame.
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
                case BrimmyAttackType.BrimstoneRoseWither:
                    DoBehavior_BrimstoneRoseWither(npc, target, lifeRatio, pissedOff, shouldBeBuffed, ref attackTimer, ref frameType);
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

        public static void DoBehavior_BrimstoneRoseWither(NPC npc, Player target, float lifeRatio, bool pissedOff, bool shouldBeBuffed, ref float attackTimer, ref float frameType)
        {
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

                    // Create charge dust.
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

        public static void SelectNewAttack(NPC npc)
        {
            WeightedRandom<BrimmyAttackType> attackSelector = new WeightedRandom<BrimmyAttackType>();
            switch ((BrimmyAttackType)(int)npc.ai[0])
            {
                case BrimmyAttackType.FlameTeleportBombardment:
                    attackSelector.Add(BrimmyAttackType.BrimstoneRoseWither);
                    break;
                case BrimmyAttackType.BrimstoneRoseWither:
                    attackSelector.Add(BrimmyAttackType.FlameTeleportBombardment);
                    break;
            }

            npc.ai[0] = (int)attackSelector.Get();
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
    */
}
