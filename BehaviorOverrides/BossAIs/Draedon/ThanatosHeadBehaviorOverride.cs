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
			ProjectileShooting_RedLaser,
			AggressiveCharge,
			ProjectileShooting_PurpleLaser,
			ProjectileShooting_GreenLaser,
			VomitNuke
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
			ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[7];
			ref float complementMechIndex = ref npc.Infernum().ExtraAI[10];
			ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[11];
			ref float finalMechIndex = ref npc.Infernum().ExtraAI[12];
			NPC initialMech = ExoMechManagement.FindInitialMech();
			NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active ? Main.npc[(int)complementMechIndex] : null;
			NPC finalMech = ExoMechManagement.FindFinalMech();

			// Define rotation and direction.
			int oldDirection = npc.direction;
			npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
			npc.direction = npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
			if (oldDirection != npc.direction)
				npc.netUpdate = true;

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

				finalMechIndex = -1f;
				complementMechIndex = -1f;
				segmentsSpawned++;
				npc.netUpdate = true;
			}

			// Summon the complement mech and reset things once ready.
			if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
			{
				ExoMechManagement.SummonComplementMech(npc);
				hasSummonedComplementMech = 1f;
				attackTimer = 0f;
				npc.netUpdate = true;
			}

			// Summon the final mech once ready.
			if (wasNotInitialSummon == 0f && finalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
			{
				ExoMechManagement.SummonFinalMech(npc);
				npc.netUpdate = true;
			}

			// Get a target.
			npc.TargetClosest(false);
			Player target = Main.player[npc.target];

			// Become invincible if the complement mech is at high enough health.
			npc.dontTakeDamage = false;
			if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
				npc.dontTakeDamage = true;

			// Become invincible and disappear if the final mech is present.
			npc.Calamity().newAI[1] = 0f;
			if (finalMech != null && finalMech != npc)
			{
				npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.08f, 0f, 1f);
				attackTimer = 0f;
				attackState = (int)ThanatosHeadAttackType.AggressiveCharge;
				npc.Calamity().newAI[1] = (int)ThanatosHead.SecondaryPhase.PassiveAndImmune;
				npc.Calamity().ShouldCloseHPBar = true;
				npc.dontTakeDamage = true;
			}
			else
				npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

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
				case ThanatosHeadAttackType.ProjectileShooting_PurpleLaser:
					DoBehavior_ProjectileShooting_PurpleLaser(npc, target, ref attackTimer, ref frameType);
					break;
				case ThanatosHeadAttackType.ProjectileShooting_GreenLaser:
					DoBehavior_ProjectileShooting_GreenLaser(npc, target, ref attackTimer, ref frameType);
					break;
				case ThanatosHeadAttackType.VomitNuke:
					DoBehavior_VomitNuke(npc, target, ref attackTimer, ref frameType);
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
				npc.takenDamageMultiplier = 103.184f;
				if (npc.Opacity > 0.6f)
				{
					npc.ModNPC<ThanatosHead>().SmokeDrawer.BaseMoveRotation = npc.rotation - MathHelper.PiOver2;
					npc.ModNPC<ThanatosHead>().SmokeDrawer.ParticleSpawnRate = 5;
				}
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
			float flyAcceleration = MathHelper.Lerp(0.042f, 0.03f, lifeRatio);
			float idealFlySpeed = MathHelper.Lerp(13f, 9.6f, lifeRatio);
			float generalSpeedFactor = Utils.InverseLerp(0f, 35f, attackTimer, true) * 0.825f + 1f;

			Vector2 destination = target.Center;

			float distanceFromDestination = npc.Distance(destination);
			if (!npc.WithinRange(destination, 550f))
			{
				distanceFromDestination = npc.Distance(destination);
				flyAcceleration *= 1.2f;
			}

			// Charge if the player is far away.
			// Don't do this at the start of the fight though. Doing so might lead to an unfair
			// charge.
			if (distanceFromDestination > 1750f && attackTimer > 90f)
				idealFlySpeed = 22f;

			if (ExoMechManagement.CurrentThanatosPhase == 4)
			{
				generalSpeedFactor *= 0.75f;
				flyAcceleration *= 0.5f;
			}
			else
			{
				if (ExoMechManagement.CurrentThanatosPhase >= 2)
					generalSpeedFactor *= 1.1f;
				if (ExoMechManagement.CurrentThanatosPhase >= 3)
				{
					generalSpeedFactor *= 1.1f;
					flyAcceleration *= 1.1f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 5)
				{
					generalSpeedFactor *= 1.1f;
					flyAcceleration *= 1.1f;
				}
			}

			// Enforce a lower bound on the speed factor.
			if (generalSpeedFactor < 1f)
				generalSpeedFactor = 1f;

			float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

			// Adjust the speed based on how the direction towards the target compares to the direction of the
			// current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
			if (!npc.WithinRange(destination, 250f))
			{
				float flySpeed = npc.velocity.Length();
				if (flySpeed < 13f)
					flySpeed += 0.06f;

				if (flySpeed > 15f)
					flySpeed -= 0.065f;

				if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
					flySpeed += 0.16f;

				if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
					flySpeed -= 0.1f;

				flySpeed = MathHelper.Clamp(flySpeed, 12f, 19f) * generalSpeedFactor;
				npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * flySpeed;
			}

			if (!npc.WithinRange(target.Center, 200f))
				npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), generalSpeedFactor);

			// Lunge if near the player.
			bool canCharge = ExoMechManagement.CurrentThanatosPhase != 4 && directionToPlayerOrthogonality > 0.75f && distanceFromDestination < 400f;
			if (canCharge && npc.velocity.Length() < idealFlySpeed * generalSpeedFactor * 1.8f)
				npc.velocity *= 1.2f;

			if (attackTimer > 720f)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_ProjectileShooting_RedLaser(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Closed;

			int segmentShootDelay = 100;
			ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
			ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
			ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

			if (ExoMechManagement.CurrentThanatosPhase == 4)
				segmentShootDelay += 60;

			// Do movement.
			DoProjectileShootInterceptionMovement(npc, target);

			// Select segment shoot attributes.
			if (attackTimer % segmentShootDelay == segmentShootDelay - 1f)
			{
				totalSegmentsToFire = 20f;
				segmentFireTime = 75f;

				if (ExoMechManagement.CurrentThanatosPhase == 4)
				{
					totalSegmentsToFire -= 4f;
					segmentFireTime += 10f;
				}
				else
				{
					if (ExoMechManagement.CurrentThanatosPhase >= 2)
						totalSegmentsToFire += 6f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 3)
					totalSegmentsToFire += 4f;
				if (ExoMechManagement.CurrentThanatosPhase >= 5)
				{
					totalSegmentsToFire += 4f;
					segmentFireTime += 10f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 6)
				{
					totalSegmentsToFire += 6f;
					segmentFireTime += 8f;
				}

				segmentFireCountdown = segmentFireTime;
				npc.netUpdate = true;
			}

			if (segmentFireCountdown > 0f)
				segmentFireCountdown--;

			if (attackTimer > 600f)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_ProjectileShooting_PurpleLaser(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Closed;

			int segmentShootDelay = 80;
			ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
			ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
			ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

			if (ExoMechManagement.CurrentThanatosPhase == 4)
				segmentShootDelay += 60;

			// Do movement.
			DoProjectileShootInterceptionMovement(npc, target);

			// Select segment shoot attributes.
			if (attackTimer % segmentShootDelay == segmentShootDelay - 1f)
			{
				totalSegmentsToFire = 20f;
				segmentFireTime = 60f;

				if (ExoMechManagement.CurrentThanatosPhase == 4)
				{
					totalSegmentsToFire -= 4f;
					segmentFireTime += 10f;
				}
				else
				{
					if (ExoMechManagement.CurrentThanatosPhase >= 2)
						totalSegmentsToFire += 6f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 3)
					totalSegmentsToFire += 4f;
				if (ExoMechManagement.CurrentThanatosPhase >= 5)
				{
					totalSegmentsToFire += 4f;
					segmentFireTime += 10f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 6)
				{
					totalSegmentsToFire += 6f;
					segmentFireTime += 8f;
				}

				segmentFireCountdown = segmentFireTime;
				npc.netUpdate = true;
			}

			if (segmentFireCountdown > 0f)
				segmentFireCountdown--;

			if (attackTimer > 600f)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_ProjectileShooting_GreenLaser(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Closed;

			int segmentShootDelay = 120;
			ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
			ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
			ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

			if (ExoMechManagement.CurrentThanatosPhase == 4)
				segmentShootDelay += 75;

			// Do movement.
			DoProjectileShootInterceptionMovement(npc, target);

			// Select segment shoot attributes.
			if (attackTimer % segmentShootDelay == segmentShootDelay - 1f)
			{
				totalSegmentsToFire = 30f;
				segmentFireTime = 80f;

				if (ExoMechManagement.CurrentThanatosPhase == 4)
				{
					totalSegmentsToFire -= 6f;
					segmentFireTime += 10f;
				}
				else
				{
					if (ExoMechManagement.CurrentThanatosPhase >= 2)
						totalSegmentsToFire += 6f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 3)
					totalSegmentsToFire += 4f;
				if (ExoMechManagement.CurrentThanatosPhase >= 5)
				{
					totalSegmentsToFire += 4f;
					segmentFireTime += 10f;
				}
				if (ExoMechManagement.CurrentThanatosPhase >= 6)
				{
					totalSegmentsToFire += 6f;
					segmentFireTime += 8f;
				}

				segmentFireCountdown = segmentFireTime;
				npc.netUpdate = true;
			}

			if (segmentFireCountdown > 0f)
				segmentFireCountdown--;

			if (attackTimer > 600f)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_VomitNuke(NPC npc, Player target, ref float attackTimer, ref float frameType)
		{
			// Decide frames.
			frameType = (int)ThanatosFrameType.Open;

			int nukeShootCount = 3;
			int nukeShootRate = 95;

			if (ExoMechManagement.CurrentThanatosPhase >= 5)
			{
				nukeShootCount += 2;
				nukeShootRate -= 30;
			}
			if (ExoMechManagement.CurrentThanatosPhase >= 6)
			{
				nukeShootCount++;
				nukeShootRate -= 20;
			}

			DoProjectileShootInterceptionMovement(npc, target, 0.6f);

			// Fire the nuke.
			if (attackTimer % nukeShootRate == nukeShootRate - 1f)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 nukeShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 18f;
					Utilities.NewProjectileBetter(npc.Center, nukeShootVelocity, ModContent.ProjectileType<ThanatosNuke>(), 0, 0f, npc.target);

					npc.netUpdate = true;
				}
			}

			if (attackTimer >= nukeShootRate * (nukeShootCount + 0.8f))
				SelectNextAttack(npc);
		}

		public static void DoProjectileShootInterceptionMovement(NPC npc, Player target, float speedMultiplier = 1f)
		{
			// Attempt to intercept the target.
			Vector2 hoverDestination = target.Center + target.velocity.SafeNormalize(Vector2.UnitX * target.direction) * new Vector2(675f, 550f);
			hoverDestination.Y -= 550f;

			float idealFlySpeed = 17f;

			if (ExoMechManagement.CurrentThanatosPhase == 4)
				idealFlySpeed *= 0.7f;
			else
			{
				if (ExoMechManagement.CurrentThanatosPhase >= 2)
					idealFlySpeed *= 1.2f;
			}
			if (ExoMechManagement.CurrentThanatosPhase >= 3)
				idealFlySpeed *= 1.2f;
			if (ExoMechManagement.CurrentThanatosPhase >= 5)
				idealFlySpeed *= 1.225f;

			idealFlySpeed += npc.Distance(target.Center) * 0.004f;
			idealFlySpeed *= speedMultiplier;

			// Move towards the target if far away from them.
			if (!npc.WithinRange(target.Center, 1600f))
				hoverDestination = target.Center;

			if (!npc.WithinRange(hoverDestination, 210f))
			{
				float flySpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.05f);
				npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(hoverDestination), flySpeed / 580f, true) * flySpeed;
			}
			else
				npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * idealFlySpeed, idealFlySpeed / 24f);
		}

		public static void SelectNextAttack(NPC npc)
		{
			ThanatosHeadAttackType oldAttackType = (ThanatosHeadAttackType)(int)npc.ai[0];
			ThanatosHeadAttackType newAttackType;

			// Update learning stuff.
			ExoMechManagement.DoPostAttackSelections(npc);

			if (oldAttackType == ThanatosHeadAttackType.AggressiveCharge)
			{
				newAttackType = (ThanatosHeadAttackType)(Main.player[npc.target].Infernum().ThanatosLaserTypeSelector.MakeSelection() + 1);
				if (Main.rand.NextBool(4) && ExoMechManagement.CurrentThanatosPhase >= 3)
					newAttackType = ThanatosHeadAttackType.VomitNuke;
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

				// Play a vent sound (sus).
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
