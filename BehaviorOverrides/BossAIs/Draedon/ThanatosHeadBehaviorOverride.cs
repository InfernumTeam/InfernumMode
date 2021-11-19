using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class ThanatosHeadBehaviorOverride : NPCBehaviorOverride
	{
		public enum ThanatosHeadFrameType
		{
			Closed,
			Open
		}

		public enum ThanatosHeadAttackType
		{
			AggressiveCharge,
			ProjectileShooting_RedLaser
		}

		public override int NPCOverrideType => ModContent.NPCType<ThanatosHead>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

		public const float OpenSegmentDR = 0f;
		public const float ClosedSegmentDR = 0.98f;

		public override bool PreAI(NPC npc)
		{
			// Define the life ratio.
			float lifeRatio = npc.life / (float)npc.lifeMax;

			// Define the whoAmI variable.
			CalamityGlobalNPC.draedonExoMechWorm = npc.whoAmI;

			// Reset frame states.
			ref float frameType = ref npc.localAI[0];
			frameType = (int)ThanatosHeadFrameType.Closed;

			// Define attack variables.
			ref float attackState = ref npc.ai[0];
			ref float attackTimer = ref npc.ai[1];
			ref float segmentsSpawned = ref npc.ai[2];

			// Define rotation and direction.
			int oldDirection = npc.direction;
			npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
			npc.direction = npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
			if (oldDirection != npc.direction)
				npc.netUpdate = true;

			// Fade in.
			npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

			// Create segments.
			if (Main.netMode != NetmodeID.MultiplayerClient && segmentsSpawned == 0f)
			{
				int segmentCount = 100;
				int previous = npc.whoAmI;
				for (int i = 0; i < segmentCount; i++)
				{
					int lol;
					if (i < segmentCount - 1)
					{
						if (i % 2 == 0)
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody1>(), npc.whoAmI);
						else
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody2>(), npc.whoAmI);
					}
					else
						lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosTail>(), npc.whoAmI);

					Main.npc[lol].realLife = npc.whoAmI;
					Main.npc[lol].ai[2] = npc.whoAmI;
					Main.npc[lol].ai[1] = previous;
					if (i > 0)
						Main.npc[previous].ai[0] = lol;
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
					previous = lol;
				}

				segmentsSpawned++;
				npc.netUpdate = true;
			}

			// Get a target.
			npc.TargetClosest(false);
			Player target = Main.player[npc.target];

			// Despawn if the target is gone.
			if (!target.active || target.dead)
			{
				npc.TargetClosest(false);
				target = Main.player[npc.target];
				if (!target.active || target.dead)
					npc.active = false;
			}

			switch ((ThanatosHeadAttackType)(int)attackState)
			{
				case ThanatosHeadAttackType.AggressiveCharge:
					DoBehavior_AggressiveCharge(npc, target, ref attackTimer, ref frameType);
					break;
			}

			// Handle smoke venting and open/closed DR.
			npc.Calamity().DR = ClosedSegmentDR;
			npc.Calamity().unbreakableDR = true;
			npc.chaseable = false;
			npc.defense = 0;
			npc.takenDamageMultiplier = 1f;
			npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 9999999;
			if (frameType == (int)ThanatosHeadFrameType.Open)
			{
				// Emit light.
				Lighting.AddLight(npc.Center, 0.35f * npc.Opacity, 0.05f * npc.Opacity, 0.05f * npc.Opacity);

				// Emit smoke.
				npc.takenDamageMultiplier = 18.115f;
				npc.ModNPC<ThanatosHead>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
				npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 5;
				npc.Calamity().DR = OpenSegmentDR;
				npc.Calamity().unbreakableDR = false;
				npc.chaseable = true;
			}
			// Emit light.
			else
				Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

			// Become vulnerable on the map.
			typeof(ThanatosHead).GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, frameType == (int)ThanatosHeadFrameType.Open);

			npc.ModNPC<ThanatosHead>().SmokeDrawer.Update();
			attackTimer++;

			return false;
		}
		
		public static void DoBehavior_AggressiveCharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosHeadFrameType.Open;

			float lifeRatio = npc.life / (float)npc.lifeMax;
			float idealFlyAcceleration = MathHelper.Lerp(0.045f, 0.03f, lifeRatio);
			float idealFlySpeed = MathHelper.Lerp(13f, 9.6f, lifeRatio);

			Vector2 destination = target.Center;

			float distanceFromDestination = npc.Distance(destination);
			if (!npc.WithinRange(destination, 550f))
			{
				distanceFromDestination = npc.Distance(destination);
				idealFlyAcceleration *= 1.45f;
			}

			// Charge if the player is far away.
			// Don't do this at the start of the fight though. Doing so might lead to an unfair
			// charge.
			if (distanceFromDestination > 1500f && attackTimer > 90f)
				idealFlySpeed = 22f;

			float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

			// Adjust the speed based on how the direction towards the target compares to the direction of the
			// current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
			if (!npc.WithinRange(destination, 250f))
			{
				float speed = npc.velocity.Length();
				if (speed < 9f)
					speed += 0.06f;

				if (speed > 15f)
					speed -= 0.065f;

				if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
					speed += 0.16f;

				if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
					speed -= 0.1f;

				speed = MathHelper.Clamp(speed, 8f, 18f) * 1.5f;
				npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), idealFlyAcceleration, true) * speed;
			}

			// Lunge if near the player.
			if (distanceFromDestination < 400f && directionToPlayerOrthogonality > 0.45f && npc.velocity.Length() < idealFlySpeed * 2.4f)
				npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.3f;
		}

		public override void FindFrame(NPC npc, int frameHeight)
		{
			// Swap between venting and non-venting frames.
			npc.frameCounter++;
			if (npc.localAI[0] == (int)ThanatosHeadFrameType.Closed)
			{
				if (npc.frameCounter >= 6D)
				{
					npc.frame.Y -= frameHeight;
					npc.frameCounter = 0D;
				}
				if (npc.frame.Y < 0)
					npc.frame.Y = 0;
			}
			else
			{
				if (npc.frameCounter >= 6D)
				{
					npc.frame.Y += frameHeight;
					npc.frameCounter = 0D;
				}
				int finalFrame = Main.npcFrameCount[npc.type] - 1;

				// Play a vent sound (sus)
				if (Main.netMode != NetmodeID.Server && npc.frame.Y == frameHeight * (finalFrame - 1))
				{
					SoundEffectInstance sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ThanatosVent"), npc.Center);
					if (sound != null)
						sound.Volume *= 0.1f;
				}

				if (npc.frame.Y >= frameHeight * finalFrame)
					npc.frame.Y = frameHeight * finalFrame;
			}
		}
    }
}
