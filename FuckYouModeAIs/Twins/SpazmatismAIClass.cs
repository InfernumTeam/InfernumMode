using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.FuckYouModeAIs.Twins
{
	public class SpazmatismAIClass
    {
        [OverrideAppliesTo(NPCID.Spazmatism, typeof(SpazmatismAIClass), "SpazmatismAI", EntityOverrideContext.NPCAI)]
        public static bool SpazmatismAI(NPC npc) => TwinsAttackSynchronizer.DoAI(npc);

        [OverrideAppliesTo(NPCID.Spazmatism, typeof(SpazmatismAIClass), "SpazmatismFindFrame", EntityOverrideContext.NPCFindFrame)]
        public static void SpazmatismFindFrame(NPC npc, int frameHeight)
		{
			npc.frameCounter++;
			npc.frame.Y = (int)npc.frameCounter % 21 / 7 * frameHeight;

			if (TwinsAttackSynchronizer.PersonallyInPhase2(npc))
				npc.frame.Y += frameHeight * 3;
        }

		[OverrideAppliesTo(NPCID.Spazmatism, typeof(SpazmatismAIClass), "SpazmatismPreDraw", EntityOverrideContext.NPCPreDraw)]
		public static bool SpazmatismPreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Main.npc[i].active || npc.whoAmI == i || Main.npc[i].type != NPCID.Retinazer || Main.npc[i].type != NPCID.Spazmatism)
					continue;

				float rotation = npc.AngleTo(Main.npc[i].Center) - MathHelper.PiOver2;

				Vector2 currentChainPosition = npc.Center;
				bool chainIsTooLong = Vector2.Distance(currentChainPosition, Main.npc[i].Center) > 2000f;

				Texture2D chainTexture = Main.chain12Texture;
				while (!chainIsTooLong)
				{
					float distanceFromDestination = Vector2.Distance(currentChainPosition, Main.npc[i].Center);
					if (distanceFromDestination < 40f)
						break;

					currentChainPosition += (Main.npc[i].Center - currentChainPosition).SafeNormalize(Vector2.Zero) * chainTexture.Height;
					Color chainColor = npc.GetAlpha(lightColor);
					spriteBatch.Draw(chainTexture, currentChainPosition - Main.screenPosition, null, chainColor, rotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
				}
			}

			Texture2D texture = Main.npcTexture[npc.type];
			void drawInstance(Vector2 drawPosition, Color drawColor, float rotation)
			{
				Vector2 origin = texture.Size() * 0.5f / new Vector2(1f, Main.npcFrameCount[npc.type]);
				spriteBatch.Draw(texture, drawPosition - Main.screenPosition, npc.frame, npc.GetAlpha(drawColor), rotation, origin, npc.scale, SpriteEffects.None, 0f);
			}

			int totalInstancesToDraw = 1;
			Color color = lightColor;
			float overdriveTimer = npc.Infernum().ExtraAI[4];
			if (TwinsAttackSynchronizer.CurrentAttackState == TwinsAttackSynchronizer.TwinsAttackState.SuperAttack || overdriveTimer > 0f) 
			{
				float fadeCompletion = Utils.InverseLerp(0f, 60f, TwinsAttackSynchronizer.UniversalAttackTimer, true);
				if (overdriveTimer > 0f)
					fadeCompletion = overdriveTimer / TwinsShield.HealTime;

				totalInstancesToDraw += (int)MathHelper.Lerp(1f, 20f, fadeCompletion);

				Color endColor = Color.GreenYellow;
				if (overdriveTimer > 0f)
				{
					endColor = Color.Lerp(Color.White, Color.MediumPurple, overdriveTimer / TwinsShield.HealTime);
					endColor.A = 0;
				}

				color = Color.Lerp(color, endColor * (4f / totalInstancesToDraw), fadeCompletion);
				color.A = 0;
			}

			color *= npc.Opacity;

			for (int i = 0; i < totalInstancesToDraw; i++)
			{
				Vector2 drawOffset = (MathHelper.TwoPi * i / totalInstancesToDraw).ToRotationVector2() * 8f;
				drawOffset *= MathHelper.Lerp(0.85f, 1.2f, (float)Math.Sin(MathHelper.TwoPi * i / totalInstancesToDraw + Main.GlobalTime * 3f) * 0.5f + 0.5f);
				drawInstance(npc.Center + drawOffset, color, npc.rotation);
			}
			return false;
		}
	}
}
