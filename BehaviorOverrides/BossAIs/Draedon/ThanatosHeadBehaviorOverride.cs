using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class ThanatosHeadBehaviorOverride : NPCBehaviorOverride
	{
		public enum ThanatosFrameType
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

		public const int SegmentCount = 100;
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
			frameType = (int)ThanatosFrameType.Closed;

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
				int previous = npc.whoAmI;
				for (int i = 0; i < SegmentCount; i++)
				{
					int lol;
					if (i < SegmentCount - 1)
					{
						if (i % 2 == 0)
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody1>(), npc.whoAmI);
						else
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody2>(), npc.whoAmI);
					}
					else
						lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosTail>(), npc.whoAmI);

					Main.npc[lol].realLife = npc.whoAmI;
					Main.npc[lol].ai[0] = i;
					Main.npc[lol].ai[1] = previous;
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
				case ThanatosHeadAttackType.ProjectileShooting_RedLaser:
					DoBehavior_ProjectileShooting_RedLaser(npc, target, ref attackTimer, ref frameType);
					break;
			}

			// Handle smoke venting and open/closed DR.
			npc.Calamity().DR = ClosedSegmentDR;
			npc.Calamity().unbreakableDR = true;
			npc.chaseable = false;
			npc.defense = 0;
			npc.takenDamageMultiplier = 1f;
			npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 9999999;
			if (frameType == (int)ThanatosFrameType.Open)
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
			typeof(ThanatosHead).GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, frameType == (int)ThanatosFrameType.Open);

			npc.ModNPC<ThanatosHead>().SmokeDrawer.Update();
			attackTimer++;

			return false;
		}
		
		public static void DoBehavior_AggressiveCharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Open;

			float lifeRatio = npc.life / (float)npc.lifeMax;
			float flyAcceleration = MathHelper.Lerp(0.045f, 0.03f, lifeRatio);
			float idealFlySpeed = MathHelper.Lerp(13f, 9.6f, lifeRatio);
			float generalSpeedFactor = 1.5f;

			Vector2 destination = target.Center;

			float distanceFromDestination = npc.Distance(destination);
			if (!npc.WithinRange(destination, 550f))
			{
				distanceFromDestination = npc.Distance(destination);
				flyAcceleration *= 1.45f;
			}

			// Charge if the player is far away.
			// Don't do this at the start of the fight though. Doing so might lead to an unfair
			// charge.
			if (distanceFromDestination > 1500f && attackTimer > 90f)
				idealFlySpeed = 22f;

			if (AresBodyBehaviorOverride.ComplementMechIsPresent(npc))
            {
				generalSpeedFactor *= 0.7f;
				flyAcceleration *= 0.7f;
			}

			float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

			// Adjust the speed based on how the direction towards the target compares to the direction of the
			// current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
			if (!npc.WithinRange(destination, 250f))
			{
				float flySpeed = npc.velocity.Length();
				if (flySpeed < 9f)
					flySpeed += 0.06f;

				if (flySpeed > 15f)
					flySpeed -= 0.065f;

				if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
					flySpeed += 0.16f;

				if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
					flySpeed -= 0.1f;

				flySpeed = MathHelper.Clamp(flySpeed, 8f, 18f) * generalSpeedFactor;
				npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * flySpeed;
			}

			if (npc.WithinRange(target.Center, 500f) && !npc.WithinRange(target.Center, 200f))
				npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), 1f);

			// Lunge if near the player.
			if (distanceFromDestination < 400f && directionToPlayerOrthogonality > 0.75f && npc.velocity.Length() < idealFlySpeed * 2.4f)
				npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.3f;

			if (attackTimer > 720f)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_ProjectileShooting_RedLaser(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Closed;

			// Attempt to intercept the target.
			Vector2 hoverDestination = target.Center + target.velocity.SafeNormalize(Vector2.UnitX * target.direction) * new Vector2(675f, 950f);
			hoverDestination.Y -= 650f;

			int segmentShootDelay = 110;
			float idealFlySpeed = 13f;

			if (AresBodyBehaviorOverride.ComplementMechIsPresent(npc))
			{
				segmentShootDelay += 60;
				idealFlySpeed *= 0.7f;
			}

			ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
			ref float segmentSelectionOffset = ref npc.Infernum().ExtraAI[1];
			ref float segmentFireTime = ref npc.Infernum().ExtraAI[2];
			ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[3];

			// Move towards the target if far away from them.
			if (!npc.WithinRange(target.Center, 1600f))
				hoverDestination = target.Center;

			if (!npc.WithinRange(hoverDestination, 210f))
			{
				float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.05f);
				npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(hoverDestination), flySpeed / 580f, true) * flySpeed;
			}

			// Select segment shoot attributes.
			if (attackTimer % segmentShootDelay == segmentShootDelay - 1f)
			{
				totalSegmentsToFire = 24f;
				segmentSelectionOffset = Main.rand.Next(8);
				segmentFireTime = 75f;

				if (AresBodyBehaviorOverride.ComplementMechIsPresent(npc))
                {
					totalSegmentsToFire *= 0.625f;
					segmentFireTime += 10f;
				}

				segmentFireCountdown = segmentFireTime;
				npc.netUpdate = true;
			}

			if (segmentFireCountdown > 0f)
				segmentFireCountdown--;
		}

		public static void SelectNextAttack(NPC npc)
		{
			// TODO: Incorporate intelligent attack selection into the projectile shot choosing.
			ThanatosHeadAttackType oldAttackType = (ThanatosHeadAttackType)(int)npc.ai[0];
			ThanatosHeadAttackType newAttackType;

			if (oldAttackType == ThanatosHeadAttackType.AggressiveCharge)
			{
				newAttackType = ThanatosHeadAttackType.ProjectileShooting_RedLaser;
			}
			else
			{
				newAttackType = ThanatosHeadAttackType.AggressiveCharge;
			}

			for (int i = 0; i < 5; i++)
				npc.Infernum().ExtraAI[i] = 0f;

			npc.ai[0] = (int)newAttackType;
			npc.ai[1] = 0f;
			npc.netUpdate = true;
		}

		public override void FindFrame(NPC npc, int frameHeight)
		{
			// Swap between venting and non-venting frames.
			npc.frameCounter++;
			if (npc.localAI[0] == (int)ThanatosFrameType.Closed)
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
