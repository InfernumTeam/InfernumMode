using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.World.Generation;

using BrimmyNPC = CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental;

namespace InfernumMode.FuckYouModeAIs.BrimstoneElemental
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
            FlameTeleportBombardment
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
			}

            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, float spawnAnimationTimer, ref float frameType)
		{
            frameType = (int)BrimmyFrameType.ClosedShell;
            npc.velocity = Vector2.UnitY * Utils.InverseLerp(135f, 45f, spawnAnimationTimer, true) * -4f;
            npc.Opacity = Utils.InverseLerp(0f, 40f, spawnAnimationTimer, true);

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
            int bombardCount = lifeRatio < 0.5f ? 5 : 4;
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

        public static void SelectNewAttack(NPC npc)
		{
            WeightedRandom<BrimmyAttackType> attackSelector = new WeightedRandom<BrimmyAttackType>();
            switch ((BrimmyAttackType)(int)npc.ai[0])
			{
                case BrimmyAttackType.FlameTeleportBombardment:
                    break;
			}
            attackSelector.Add(BrimmyAttackType.FlameTeleportBombardment);

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
}
