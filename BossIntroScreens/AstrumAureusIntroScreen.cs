using CalamityMod.NPCs.AstrumAureus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
	public class AstrumAureusIntroScreen : BaseIntroScreen
	{
		public override TextColorData TextColor => Color.White;

		public override int AnimationTime => 210;

		public override bool TextShouldBeCentered => true;

		public override string TextToDisplay => "The Corrupted Stomper\nAstrum Aureus";

		public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:MechsIntro"].Shader;

		public override void PrepareShader(Effect shader)
		{
			Color gleamColor = Color.Lerp(new Color(255, 164, 94), new Color(109, 242, 196), (float)Math.Cos(Main.GlobalTime * 6f) * 0.5f + 0.5f);
			shader.Parameters["uColor"].SetValue(gleamColor.ToVector3());
			shader.GraphicsDevice.Textures[1] = ModContent.GetTexture("InfernumMode/ExtraTextures/DiagonalGleam");
		}

		public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AstrumAureus>());

		public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.Instance.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/ThanatosTransition");

		public override LegacySoundStyle SoundToPlayWithLetterAddition => SoundID.NPCHit4;

		public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

		public override float LetterDisplayCompletionRatio(int animationTimer)
		{
			float completionRatio = Utils.InverseLerp(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

			// If the completion ratio exceeds the point where the name is displayed, display all letters.
			int startOfLargeTextIndex = TextToDisplay.IndexOf('\n');
			int currentIndex = (int)(completionRatio * TextToDisplay.Length);
			if (currentIndex >= startOfLargeTextIndex)
				completionRatio = 1f;

			return completionRatio;
		}
	}
}