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
	public class AresPlasmaCannonBehaviorOverride : NPCBehaviorOverride
	{
		public override int NPCOverrideType => ModContent.NPCType<AresPlasmaFlamethrower>();

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

			// Shamelessly steal variables from Ares.
			npc.target = aresBody.target;
			npc.Opacity = aresBody.Opacity;
			npc.dontTakeDamage = aresBody.dontTakeDamage;
			int projectileDamageBoost = (int)aresBody.Infernum().ExtraAI[8];
			Player target = Main.player[npc.target];

			// Define attack variables.
			bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
			int shootTime = 150;
			int totalFlamesPerBurst = 2;
			float flameShootSpeed = 10f;
			float aimPredictiveness = 20f;

			// Nerf things while Ares' complement mech is present.
			if (ExoMechManagement.CurrentAresPhase == 4)
				flameShootSpeed *= 0.75f;

			if (ExoMechManagement.CurrentAresPhase >= 5)
				flameShootSpeed *= 1.25f;

			int shootRate = shootTime / totalFlamesPerBurst;
			ref float attackTimer = ref npc.ai[0];
			ref float chargeDelay = ref npc.ai[1];

			// Initialize delays and other timers.
			if (chargeDelay == 0f)
				chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

			// Don't do anything if this arm should be disabled.
			if (currentlyDisabled && attackTimer >= chargeDelay)
				attackTimer = chargeDelay;

			// Hover near Ares.
			AresBodyBehaviorOverride.DoHoverMovement(npc, aresBody.Center + new Vector2(375f, 100f), 32f, 75f);

			// Choose a direction and rotation.
			// Rotation is relative to predictiveness, unless disabled.
			Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
			Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 66f + Vector2.UnitY * 16f;
			float idealRotation = aimDirection.ToRotation();
			if (currentlyDisabled)
				idealRotation = MathHelper.Clamp(npc.velocity.X * -0.016f, -0.81f, 0.81f) + MathHelper.PiOver2;

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
				Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
				plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
				plasma.scale = 1.25f;
				plasma.noGravity = true;
			}

			// Fire plasma.
			if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int fireballCount = ExoMechManagement.CurrentAresPhase >= 3 ? 2 : 1;

					for (int i = 0; i < fireballCount; i++)
					{
						Vector2 flameShootVelocity = aimDirection * flameShootSpeed;
						int fireballType = ModContent.ProjectileType<AresPlasmaFireball>();
						if (ExoMechManagement.CurrentAresPhase >= 2)
							fireballType = ModContent.ProjectileType<AresPlasmaFireball2>();
						if (fireballCount > 1)
						{
							flameShootVelocity = flameShootVelocity.RotatedByRandom(0.34f);
							if (i > 0)
								flameShootVelocity *= Main.rand.NextFloat(0.6f, 0.9f);
						}

						Utilities.NewProjectileBetter(endOfCannon, flameShootVelocity, fireballType, projectileDamageBoost + 550, 0f);
					}

					npc.netUpdate = true;
				}
			}

			// Reset the attack timer after an attack cycle ends.
			if (attackTimer >= chargeDelay + shootTime)
			{
				attackTimer = 0f;
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

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrowerGlow");

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
