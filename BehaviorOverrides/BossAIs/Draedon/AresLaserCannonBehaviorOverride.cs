using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class AresLaserCannonBehaviorOverride : NPCBehaviorOverride
	{
		public override int NPCOverrideType => ModContent.NPCType<AresLaserCannon>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

		#region AI
		public override bool PreAI(NPC npc)
		{
			// Die if Ares is not present.
			if (CalamityGlobalNPC.draedonExoMechPrime == -1)
			{
				npc.active = false;
				return false;
			}

			// Locate Ares' body as an NPC.
			NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

			// Define the life ratio.
			npc.life = aresBody.life;
			npc.lifeMax = aresBody.lifeMax;
			float lifeRatio = npc.life / (float)npc.lifeMax;

			// Shamelessly steal variables from Ares.
			npc.target = aresBody.target;
			npc.Opacity = aresBody.Opacity;
			npc.dontTakeDamage = aresBody.dontTakeDamage;
			int projectileDamageBoost = (int)aresBody.Infernum().ExtraAI[8];
			Player target = Main.player[npc.target];

			// Define attack variables.
			bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
			int shootTime = 240;
			int totalLasersPerBurst = 6;
			float aimPredictiveness = 25f;
			float laserShootSpeed = 7f;
			ref float attackTimer = ref npc.ai[0];
			ref float chargeDelay = ref npc.ai[1];
			ref float laserCounter = ref npc.ai[2];
			ref float currentDirection = ref npc.ai[3];
			int laserCount = laserCounter % 3f == 2f ? 3 : 1;

			if (ExoMechManagement.CurrentAresPhase >= 2)
			{
				laserCount += 2;
				totalLasersPerBurst = 12;
				shootTime += 210;
				laserShootSpeed *= 1.1f;
			}

			if (ExoMechManagement.CurrentAresPhase >= 3)
			{
				laserShootSpeed *= 0.85f;

				if (laserCount == 3)
					laserCount += 2;
			}

			// Nerf things while Ares' complement mech is present.
			if (ExoMechManagement.CurrentAresPhase == 4)
			{
				shootTime += 70;
				if (laserCount > 4)
					laserCount = 4;
				laserCount--;
				laserShootSpeed *= 0.6f;
			}

			if (ExoMechManagement.CurrentAresPhase >= 5)
			{
				shootTime += 120;
				laserShootSpeed *= 0.8f;
			}

			int shootRate = shootTime / totalLasersPerBurst;

			// Initialize delays and other timers.
			if (chargeDelay == 0f)
				chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

			// Don't do anything if this arm should be disabled.
			if (currentlyDisabled && attackTimer >= chargeDelay)
				attackTimer = chargeDelay;

			// Hover near Ares.
			AresBodyBehaviorOverride.DoHoverMovement(npc, aresBody.Center - Vector2.UnitX * 575f, 32f, 75f);

			// Choose a direction and rotation.
			// Rotation is relative to predictiveness.
			Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
			Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 74f + Vector2.UnitY * 8f;
			float idealRotation = aimDirection.ToRotation();
			if (currentlyDisabled)
				idealRotation = MathHelper.Clamp(npc.velocity.X * -0.016f, -0.81f, 0.81f) + MathHelper.PiOver2;

			currentDirection = idealRotation;
			if (npc.spriteDirection == 1)
				idealRotation += MathHelper.Pi;
			if (idealRotation < 0f)
				idealRotation += MathHelper.TwoPi;
			if (idealRotation > MathHelper.TwoPi)
				idealRotation -= MathHelper.TwoPi;
			npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

			int direction = Math.Sign(target.Center.X - npc.Center.X);
			if (direction != 0)
			{
				npc.direction = direction;

				if (npc.spriteDirection != -npc.direction)
					npc.rotation += MathHelper.Pi;

				npc.spriteDirection = -npc.direction;
			}

			// Create a dust telegraph before firing.
			if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
			{
				Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
				Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 182);
				electricity.velocity = (endOfCannon - electricity.position) * 0.04f;
				electricity.scale = 1.25f;
				electricity.noGravity = true;
			}

			// Fire lasers.
			if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (int i = 0; i < laserCount; i++)
					{
						Vector2 shootVelocity = aimDirection * laserShootSpeed;
						if (laserCount > 1)
							shootVelocity = shootVelocity.RotatedBy(MathHelper.Lerp(-0.41f, 0.41f, i / (float)(laserCount - 1f)));
						shootVelocity = shootVelocity.RotatedByRandom(0.07f);
						int laser = Utilities.NewProjectileBetter(endOfCannon, shootVelocity, ModContent.ProjectileType<CannonLaser>(), projectileDamageBoost + 575, 0f);
						if (Main.projectile.IndexInRange(laser))
							Main.projectile[laser].ai[1] = npc.whoAmI;
					}

					laserCounter++;
					npc.netUpdate = true;
				}
			}

			// Reset the attack and laser counter after an attack cycle ends.
			if (attackTimer >= chargeDelay + shootTime)
			{
				attackTimer = 0f;
				laserCounter = 0f;
				npc.netUpdate = true;
			}
			attackTimer++;
			return false;
		}

		#endregion AI

		#region Frames and Drawcode
		public override void FindFrame(NPC npc, int frameHeight)
		{
			int currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / npc.ai[1]));

			if (npc.ai[0] > npc.ai[1])
			{
				npc.frameCounter++;
				if (npc.frameCounter >= 66f)
					npc.frameCounter = 0D;
				currentFrame = (int)Math.Round(MathHelper.Lerp(36f, 47f, (float)npc.frameCounter / 66f));
			}
			else
				npc.frameCounter = 0D;

			npc.frame = new Rectangle(npc.width * (currentFrame / 8), npc.height * (currentFrame % 8), npc.width, npc.height);
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (npc.spriteDirection == 1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			Texture2D texture = Main.npcTexture[npc.type];
			Rectangle frame = npc.frame;
			Vector2 origin = frame.Size() * 0.5f;
			Color afterimageBaseColor = Color.White;
			int numAfterimages = 5;

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
					Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
				}
			}

			Vector2 center = npc.Center - Main.screenPosition;
			spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannonGlow");

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
					Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
				}
			}

			spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
			return false;
		}
		#endregion Frames and Drawcode
	}
}
