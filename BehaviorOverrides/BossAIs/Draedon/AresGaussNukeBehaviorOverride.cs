using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class AresGaussNukeBehaviorOverride : NPCBehaviorOverride
	{
		public override int NPCOverrideType => ModContent.NPCType<AresGaussNuke>();

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
			Player target = Main.player[npc.target];

			// Define attack variables.
			bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
			int shootTime = 150;
			float aimPredictiveness = 10f;
			ref float attackTimer = ref npc.ai[0];
			ref float chargeDelay = ref npc.ai[1];
			ref float rechargeTime = ref npc.ai[2];

			// Initialize delays and other timers.
			float idealChargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime * 2.3f;
			float idealRechargeTime = AresBodyBehaviorOverride.Phase1ArmChargeupTime * 2.3f;

			if (AresBodyBehaviorOverride.CurrentAresPhase >= 2)
			{
				idealChargeDelay *= 0.7f;
				idealRechargeTime *= 0.7f;
			}
			if (AresBodyBehaviorOverride.CurrentAresPhase >= 3)
			{
				idealChargeDelay *= 0.7f;
				idealRechargeTime *= 0.7f;
			}

			// Nerf things while Ares' complement mech is present.
			if (AresBodyBehaviorOverride.ComplementMechIsPresent(aresBody))
			{
				idealChargeDelay += 125f;
				idealRechargeTime += 125f;
			}

			if (chargeDelay != idealChargeDelay || rechargeTime != idealRechargeTime)
			{
				chargeDelay = idealChargeDelay;
				rechargeTime = idealRechargeTime;
				attackTimer = 0f;
				npc.netUpdate = true;
			}

			// Don't do anything if this arm should be disabled.
			if (currentlyDisabled && attackTimer >= chargeDelay - 50f)
				attackTimer = 0f;

			// Hover near Ares.
			AresBodyBehaviorOverride.DoHoverMovement(npc, aresBody.Center - Vector2.UnitX * 575f, 32f, 75f);

			// Choose a direction and rotation.
			// Rotation is relative to predictiveness.
			Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
			Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 40f;
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

			// Fire the nuke.
			if (attackTimer == (int)chargeDelay)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), npc.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					float nukeShootSpeed = 13.5f;
					Utilities.NewProjectileBetter(endOfCannon, aimDirection * nukeShootSpeed, ModContent.ProjectileType<AresGaussNukeProjectile>(), 1225, 0f, npc.target);

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
			int currentFrame;
			if (npc.ai[0] < npc.ai[1] - 30f)
				currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / (npc.ai[1] - 30f)));
			else if (npc.ai[0] <= npc.ai[1] + 30f)
				currentFrame = (int)Math.Round(MathHelper.Lerp(35f, 47f, Utils.InverseLerp(npc.ai[1] - 30f, npc.ai[1] + 30f, npc.ai[0], true)));
			else
				currentFrame = (int)Math.Round(MathHelper.Lerp(49f, 107f, Utils.InverseLerp(npc.ai[1] + 30f, npc.ai[1] + npc.ai[2], npc.ai[0], true)));

			npc.frame = new Rectangle(npc.width * (currentFrame / 12), npc.height * (currentFrame % 12), npc.width, npc.height);
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

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresGaussNukeGlow");

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
