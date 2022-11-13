using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
	public class MoonLordLeechBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.MoonLeech;
		
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI | ProjectileOverrideContext.ProjectilePreDraw;

        public override bool PreAI(Projectile projectile)
		{
			Vector2 mouthOffset = new(0f, 216f);

			// Fade in.
			projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.07f, 0f, 1f);

			// Do animation stuff.
			projectile.frameCounter++;
			projectile.frame = projectile.frameCounter / 5 % 3;

			// Die if the head or target are invalid.
			
			int headIndex = (int)Math.Abs(projectile.ai[0]) - 1;
			int target = (int)projectile.ai[1];
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (!Main.npc[headIndex].active || Main.npc[headIndex].type != NPCID.MoonLordHead)
				{
					projectile.Kill();
					return false;
				}
			}
			// Retract if the player couldn't be hooked to in time.
			projectile.localAI[0]++;
			if (projectile.localAI[0] >= 330f && projectile.ai[0] > 0f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				projectile.ai[0] *= -1f;
				projectile.netUpdate = true;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient && projectile.ai[0] > 0f && (!Main.player[target].active || Main.player[target].dead))
			{
				projectile.ai[0] *= -1f;
				projectile.netUpdate = true;
			}

			// Approach the player if they don't have the moon leech buff.
			projectile.rotation = (Main.npc[headIndex].Center - Main.player[target].Center + mouthOffset).ToRotation() + MathHelper.PiOver2;
			if (projectile.ai[0] > 0f && projectile.localAI[0] < 210f)
			{
				Vector2 playerOffset = Main.player[target].Center - projectile.Center;
				projectile.velocity = playerOffset.SafeNormalize(Vector2.Zero) * Math.Min(18f, playerOffset.Length() + 6f);

				if (playerOffset.Length() < 30f)
				{
					projectile.velocity = Vector2.Zero;
					projectile.Center = Main.player[target].Center;

					if (projectile.localAI[1] == 0f)
					{
						projectile.localAI[1] = 1f;
						if (!Main.player[target].creativeGodMode)
						{
							Main.player[target].AddBuff(BuffID.MoonLeech, 960);
							return false;
						}
					}
				}
				return false;
			}

			// Return to the mouth.
			Vector2 offsetFromMouth = Main.npc[headIndex].Center - projectile.Center + mouthOffset;
			projectile.velocity = offsetFromMouth.SafeNormalize(Vector2.Zero) * Math.Min(16f, offsetFromMouth.Length());			
			if (offsetFromMouth.Length() < 20f)
				projectile.Kill();
			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
			return false;
        }

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D leechEndTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/MoonLord/MoonLordLeech").Value;
			Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/MoonLord/CustomSprites/LeechBody").Value;
			Texture2D penultimateBodyTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/MoonLord/CustomSprites/LeechBodyPenumltimate").Value;
			Vector2 mouthOffset = Main.npc[(int)Math.Abs(projectile.ai[0]) - 1].Center - projectile.Center + Vector2.UnitY * 216f;
			float remainingDistance = mouthOffset.Length();
			Vector2 directionToMouth = mouthOffset.SafeNormalize(Vector2.UnitY);
			Rectangle leechEndFrame = leechEndTexture.Frame(1, 4, 0, projectile.frame);
			lightColor = Color.Lerp(lightColor, Color.White, 0.3f);
			
			// Draw the end of the leech.
			Main.EntitySpriteDraw(leechEndTexture, projectile.Center - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(leechEndFrame), projectile.GetAlpha(lightColor), projectile.rotation, leechEndFrame.Size() / 2f, projectile.scale, 0, 0);

			remainingDistance -= (leechEndFrame.Height / 2 + penultimateBodyTexture.Height) * projectile.scale;
			Vector2 drawPosition = projectile.Center;
			drawPosition += directionToMouth * projectile.scale * leechEndFrame.Height / 2f;
			if (remainingDistance > 0f)
			{
				float movedDistance = 0f;
                Rectangle rectangle26 = new(0, 0, bodyTexture.Width, bodyTexture.Height);
				while (movedDistance + 1f < remainingDistance)
				{
					// Cap the height of the last frame.
					if (remainingDistance - movedDistance < rectangle26.Height)
						rectangle26.Height = (int)(remainingDistance - movedDistance);

                    Color c = Color.Lerp(Lighting.GetColor(drawPosition.ToTileCoordinates()), Color.White, 0.3f);
					Main.EntitySpriteDraw(bodyTexture, drawPosition - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(rectangle26), projectile.GetAlpha(c), projectile.rotation, rectangle26.Bottom(), projectile.scale, 0, 0);
					movedDistance += rectangle26.Height * projectile.scale;
					drawPosition += directionToMouth * rectangle26.Height * projectile.scale;
				}
			}
            Color color = Color.Lerp(Lighting.GetColor(drawPosition.ToTileCoordinates()), Color.White, 0.3f);
            Rectangle penultimateFrame = penultimateBodyTexture.Frame(1, 1, 0, 0);
			if (remainingDistance < 0f)
				penultimateFrame.Height += (int)remainingDistance;

			Main.EntitySpriteDraw(penultimateBodyTexture, drawPosition - Main.screenPosition, penultimateFrame, color, projectile.rotation, new Vector2(penultimateFrame.Width / 2f, penultimateFrame.Height), projectile.scale, 0, 0);
			return false;
        }
    }
}
