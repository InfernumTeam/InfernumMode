using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.FuckYouModeAIs.OldDuke
{
	public class OldDukeBehaviorOverride : NPCBehaviorOverride
    {
        public enum OldDukeAttackState
        {
            SpawnAnimation,
            AttackSelectionWait,
            Charge,
            AcidBelch,
            AcidBubbleFountain
        }

        public enum OldDukeFrameType
		{
            FlapWings,
            Charge,
            Roar,
		}

        public override int NPCOverrideType => ModContent.NPCType<OldDukeBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        #region Phase Patterns
        public static readonly List<OldDukeAttackState> Phase1AttackPattern = new List<OldDukeAttackState>()
        {
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.AcidBelch,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.AcidBubbleFountain,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.AcidBelch,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.Charge,
            OldDukeAttackState.AcidBubbleFountain,
        };
        #endregion Phase Patterns

        #region AI
        public override bool PreAI(NPC npc)
		{
            npc.TargetClosest();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            Player target = Main.player[npc.target];
			bool outOfOcean = !BossRushEvent.BossRushActive && target.position.X > 8400f && target.position.X < Main.maxTilesX * 16f - 8400f;

            // Reset variables. They may be changed by behaviors below.
            npc.dontTakeDamage = false;
            npc.damage = npc.defDamage;

            Vector2 mouthPosition = Vector2.UnitX * (float)Math.Cos(npc.rotation) * (npc.width + 28f) * -npc.spriteDirection * 0.5f + npc.Center;
            mouthPosition.Y += 45f;

            switch ((OldDukeAttackState)(int)attackState)
			{
                case OldDukeAttackState.SpawnAnimation:
                    DoBehavior_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AttackSelectionWait:
                    DoBehavior_AttackSelectionWait(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.Charge:
                    DoBehavior_Charge(npc, target, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AcidBelch:
                    DoBehavior_AcidBelch(npc, target, mouthPosition, attackTimer, ref frameType);
                    break;
                case OldDukeAttackState.AcidBubbleFountain:
                    DoBehavior_AcidBubbleFountain(npc, target, mouthPosition, attackTimer, ref frameType);
                    break;
			}

            attackTimer++;

            return false;
		}

		#region Specific Behaviors
        public static void DoBehavior_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
		{
            if (attackTimer < 20f)
            {
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                npc.alpha -= 5;
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.alpha += 15;
                npc.alpha = Utils.Clamp(npc.alpha, 0, 150);
                npc.velocity = -Vector2.UnitY * 6f;
            }
            else
                npc.velocity *= 0.97f;

            // Right before and after the spawn animation dust stuff, roar.
            if (attackTimer > 52f && attackTimer < 64f)
                frameType = (int)OldDukeFrameType.Roar;

            // Otherwise, flap wings.
			else
			{
                frameType = (int)OldDukeFrameType.FlapWings;
                npc.frameCounter++;
			}

            // Play a sound and emit sulphurous acid dust.
            if (attackTimer == 55f)
            {
                for (int i = 0; i < 36; i++)
                {
                    Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2(npc.width / 2f, npc.height) * 0.4f).RotatedBy(MathHelper.TwoPi * i / 36f);
                    Dust acid = Dust.NewDustPerfect(dustSpawnPosition, (int)CalamityDusts.SulfurousSeaAcid);
                    acid.noGravity = true;
                    acid.noLight = true;
                    acid.velocity = npc.SafeDirectionTo(dustSpawnPosition) * 3f;
                }

                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeRoar"), npc.Center);
            }

            if (attackTimer >= 75f)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_AttackSelectionWait(NPC npc, Player target, float attackTimer, ref float frameType)
		{
            ref float horizontalHoverOffset = ref npc.Infernum().ExtraAI[0];
            OldDukeAttackState upcomingAttack = (OldDukeAttackState)(int)npc.ai[2];
            bool goingToCharge = upcomingAttack == OldDukeAttackState.Charge;

            // Hover near the target.
            if (horizontalHoverOffset == 0f)
                horizontalHoverOffset = Math.Sign(target.Center.X - npc.Center.X) * 500f;
            Vector2 hoverDestination = target.Center + new Vector2(horizontalHoverOffset, -350f) - npc.velocity;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 14.5f, 1.4f);

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (goingToCharge)
                npc.rotation = npc.AngleTo(target.Center + target.velocity * 20f);

            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Handle frames.
            frameType = (int)OldDukeFrameType.FlapWings;
            npc.frameCounter++;

            if (attackTimer >= (goingToCharge ? 40f : 45f))
                GotoNextAttackState(npc);
		}

        public static void DoBehavior_Charge(NPC npc, Player target, float attackTimer, ref float frameType)
		{
            int chargeTime = 22;
            float chargeSpeed = 40f;
            float aimAheadFactor = MathHelper.Lerp(1f, 1.45f, Utils.InverseLerp(200f, 525f, npc.Distance(target.Center), true));

            if (attackTimer >= chargeTime)
			{
                GotoNextAttackState(npc);
                return;
			}

            frameType = (int)OldDukeFrameType.Charge;

            // Do the charge on the first frame.
            if (attackTimer == 1f)
			{
                int chargeDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * aimAheadFactor * 15f) * chargeSpeed;
                npc.spriteDirection = chargeDirection;

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;

                return;
            }

            // Otherwise accelerate and emit sulphurous dust.
            npc.velocity *= 1.01f;

            // Spawn dust
            int dustCount = 7;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 50) / 2f, npc.height) * 0.75f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                Dust acid = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, (int)CalamityDusts.SulfurousSeaAcid, dustVelocity);
                acid.scale *= 1.45f;
                acid.velocity *= 0.25f;
                acid.velocity -= npc.velocity;
                acid.noGravity = true;
                acid.noLight = true;
            }
        }

        public static void DoBehavior_AcidBelch(NPC npc, Player target, Vector2 mouthPosition, float attackTimer, ref float frameType)
        {
            int shootDelay = 55;
            int belchCount = 3;
            int belchRate = 36;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 500f, -300f) - npc.velocity;

            if (npc.WithinRange(hoverDestination, 26f))
            {
                npc.Center = hoverDestination;
                npc.velocity = Vector2.Zero;
            }
            else
            {
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 2f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 8f);
            }


            // Handle frames.
            if (attackTimer <= shootDelay)
            {
                npc.frameCounter += 1.5f;
                frameType = (int)OldDukeFrameType.FlapWings;
            }
            else
            {
                if (attackTimer == shootDelay + 1f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/OldDukeVomit"), npc.Center);

                frameType = (int)OldDukeFrameType.Roar;
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Release balls of acid at the target from the mouth.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay && (attackTimer - shootDelay) % belchRate == belchRate - 1f)
            {
                Vector2 shootVelocity = (mouthPosition - npc.Center).SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 12f;
                Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<SulphuricBlob>(), 290, 0f);
            }

            if (attackTimer >= shootDelay + belchRate * (belchCount + 0.7f))
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_AcidBubbleFountain(NPC npc, Player target, Vector2 mouthPosition, float attackTimer, ref float frameType)
        {
            int shootDelay = 55;
            int bubbleCount = 15;
            int bubbleSummonRate = 20;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 500f, -300f) - npc.velocity;

            if (npc.WithinRange(hoverDestination, 26f))
            {
                npc.Center = hoverDestination;
                npc.velocity = Vector2.Zero;
            }
            else
            {
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 2f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 8f);
            }

            // Look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(target.Center);
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            // Handle frames.
            npc.frameCounter += 1.5f;
            frameType = (int)OldDukeFrameType.FlapWings;
            
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > shootDelay && (attackTimer - shootDelay) % bubbleSummonRate == bubbleSummonRate - 1f)
            {
                Vector2 bubbleSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 1000f + target.velocity.X * 60f, 800f);
                Vector2 bubbleVelocity = -Vector2.UnitY * Main.rand.NextFloat(8f, 10f);
                Utilities.NewProjectileBetter(bubbleSpawnPosition, bubbleVelocity, ModContent.ProjectileType<AcidFountainBubble>(), 260, 0f);
            }

            if (attackTimer >= shootDelay + bubbleSummonRate * (bubbleCount + 0.5f))
                GotoNextAttackState(npc);
        }
        #endregion Specific Behaviors

        #region Utilities
        public static void GotoNextAttackState(NPC npc)
		{
            OldDukeAttackState oldAttackState = (OldDukeAttackState)npc.ai[0];
            OldDukeAttackState newAttackState;

            newAttackState = Phase1AttackPattern[(int)(npc.ai[3] + 1) % Phase1AttackPattern.Count];

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[1] = 0f;
            if (oldAttackState == OldDukeAttackState.AttackSelectionWait)
            {
                npc.ai[0] = npc.ai[2];
                npc.ai[2] = 0f;
            }
            else
            {
                npc.ai[0] = (int)OldDukeAttackState.AttackSelectionWait;
                npc.ai[2] = (int)newAttackState;
                npc.ai[3]++;
            }
            npc.netUpdate = true;
		}
        #endregion Utilities

        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
		{
            switch ((OldDukeFrameType)(int)npc.localAI[0])
			{
                case OldDukeFrameType.FlapWings:
                    if (npc.frameCounter >= 8)
                    {
                        npc.frameCounter = 0;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 6)
                        npc.frame.Y = 0;
                    break;

                case OldDukeFrameType.Charge:
                    npc.frame.Y = frameHeight * 2;
                    npc.frameCounter = 0;
                    break;
                case OldDukeFrameType.Roar:
                    npc.frame.Y = frameHeight * 6;
                    npc.frameCounter = 0;
                    break;
			}
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            return true;
        }

        #endregion Frames and Drawcode
    }
}
