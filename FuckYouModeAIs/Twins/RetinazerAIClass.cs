using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.FuckYouModeAIs.Twins
{
	public class RetinazerAIClass : NPCBehaviorOverride
	{
		public override int NPCOverrideType => NPCID.Retinazer;

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc) => TwinsAttackSynchronizer.DoAI(npc);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = Main.npcTexture[npc.type];
			void drawInstance(Vector2 drawPosition, Color drawColor, float rotation)
			{
				Vector2 origin = texture.Size() * 0.5f / new Vector2(1f, Main.npcFrameCount[npc.type]);
				spriteBatch.Draw(texture, drawPosition - Main.screenPosition, npc.frame, npc.GetAlpha(drawColor), rotation, origin, npc.scale, SpriteEffects.None, 0f);
			}

			int totalInstancesToDraw = 1;
			Color color = lightColor;
			float overdriveTimer = npc.Infernum().ExtraAI[4];
			if (overdriveTimer > 0f)
			{
				float fadeCompletion = overdriveTimer / TwinsShield.HealTime;
				totalInstancesToDraw += (int)MathHelper.Lerp(1f, 40f, fadeCompletion);

				Color endColor = Color.Lerp(Color.White, Color.IndianRed, overdriveTimer / TwinsShield.HealTime);
				endColor.A = 0;

				color = Color.Lerp(color, endColor * (3f / totalInstancesToDraw), fadeCompletion);
				color.A = 0;
			}

			for (int i = 0; i < totalInstancesToDraw; i++)
			{
				Vector2 drawOffset = (MathHelper.TwoPi * i / totalInstancesToDraw).ToRotationVector2() * 5f;
				drawOffset *= MathHelper.Lerp(0.85f, 1.2f, (float)Math.Sin(MathHelper.TwoPi * i / totalInstancesToDraw + Main.GlobalTime * 3f) * 0.5f + 0.5f);
				drawInstance(npc.Center + drawOffset, color, npc.rotation);
			}
			return false;
		}

		public override void FindFrame(NPC npc, int frameHeight)
		{
			npc.frameCounter++;
			npc.frame.Y = (int)npc.frameCounter % 21 / 7 * frameHeight;

			if (TwinsAttackSynchronizer.PersonallyInPhase2(npc))
				npc.frame.Y += frameHeight * 3;
		}
	}
}
