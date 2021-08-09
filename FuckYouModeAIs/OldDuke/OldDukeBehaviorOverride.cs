using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
            Charge
        }

        public enum OldDukeFrameType
		{
            FlapWings,
            Charge,
            Roar,
		}

        public override int NPCOverrideType => ModContent.NPCType<OldDukeBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

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
                npc.velocity *= 0.98f;

            // Right before and after the spawn animation dust stuff, roar.
            if (attackTimer > 52f && attackTimer < 64f)
            {
                frameType = (int)OldDukeFrameType.Roar;
                npc.frameCounter = 0f;
            }

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
            Vector2 hoverOffset = target.Center + new Vector2(horizontalHoverOffset, -300f) - npc.velocity;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverOffset) * 14.5f, 1.4f);

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

            if (attackTimer >= (goingToCharge ? 28f : 45f))
            {
                npc.frameCounter = 0;
                GotoNextAttackState(npc);
            }
		}

        public static void DoBehavior_Charge(NPC npc, Player target, float attackTimer, ref float frameType)
		{
            int chargeTime = 30;
            float chargeSpeed = 21f;
            float wrappedTime = attackTimer % chargeTime;

            if (attackTimer >= chargeTime)
			{
                GotoNextAttackState(npc);
                return;
			}

            frameType = (int)OldDukeFrameType.Charge;

            // Do the charge on the first frame.
            if (wrappedTime == 1f)
			{
                int chargeDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * chargeSpeed;
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
		#endregion Specific Behaviors

		#region Utilities
        public static void GotoNextAttackState(NPC npc)
		{
            OldDukeAttackState oldAttackState = (OldDukeAttackState)npc.ai[0];
            OldDukeAttackState newAttackState = oldAttackState;

            switch (oldAttackState)
			{
                case OldDukeAttackState.SpawnAnimation:
                    newAttackState = OldDukeAttackState.Charge;
                    break;
			}

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
            }
            npc.netUpdate = true;
		}
		#endregion Utilities

		#endregion AI

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
                    break;
                case OldDukeFrameType.Roar:
                    npc.frame.Y = frameHeight * 6;
                    break;
			}
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            return true;
        }
    }
}
