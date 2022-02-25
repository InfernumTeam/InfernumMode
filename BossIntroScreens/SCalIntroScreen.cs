using CalamityMod.NPCs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class SCalIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Red;

        public override Color ScreenCoverColor => Color.Black;

        public override int AnimationTime => 210;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override string TextToDisplay => "The Brimstone Witch\nCalamitas";

        public override float TextScale => MajorBossTextScale;

        public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:SCalIntro"].Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(Color.Red.ToVector3());
            shader.Parameters["uSecondaryColor"].SetValue(Color.Orange.ToVector3());
            shader.GraphicsDevice.Textures[1] = ModContent.GetTexture("Terraria/Misc/Perlin");
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>());

        public override LegacySoundStyle SoundToPlayWithText => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/SupremeCalamitasSpawn");
    }
}