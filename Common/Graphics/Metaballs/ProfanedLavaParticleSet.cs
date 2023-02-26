using CalamityMod.Particles.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;


namespace InfernumMode.Common.Graphics.Metaballs
{
    public class ProfanedLavaParticleSet : BaseFusableParticleSet
    {
        public override Color BorderColor => Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0f);

        public override bool BorderShouldBeSolid => true;

        public override float BorderSize => 2f;

        public override List<Effect> BackgroundShaders => new()
        {
            GameShaders.Misc["CalamityMod:BaseFusableParticleEdge"].Shader,
            GameShaders.Misc["CalamityMod:BaseFusableParticleEdge"].Shader,
        };

        public override List<Texture2D> BackgroundTextures => new()
        {
            Main.gameMenu ? TextureAssets.MagicPixel.Value : InfernumTextureRegistry.HolyFireLayer.Value,
            Main.gameMenu ? TextureAssets.MagicPixel.Value : InfernumTextureRegistry.HolyFireLayer.Value,
        };

        public override void DrawParticles()
        {
            Texture2D fusableParticleBase = ModContent.Request<Texture2D>("CalamityMod/Particles/Metaballs/FusableParticleBase").Value;
            foreach (FusableParticle particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Color drawColor = Color.Lerp(BorderColor, WayfinderSymbol.Colors[1], 0.5f);
                Vector2 origin = fusableParticleBase.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / fusableParticleBase.Size();
                Main.spriteBatch.Draw(fusableParticleBase, drawPosition, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        public override FusableParticle SpawnParticle(Vector2 center, float sizeStrength)
        {
            Particles.Add(new FusableParticle(center, sizeStrength));
            return Particles.Last();
        }

        public override void UpdateBehavior(FusableParticle particle)
        {
            particle.Size = MathHelper.Clamp(particle.Size - 1.5f, 0f, 400f) * 0.97f;
        }

        public override void PrepareOptionalShaderData(Effect effect, int index)
        {
            switch (index)
            {
                // Background 1.
                case 0:
                    Vector2 offset = Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.03f;
                    effect.Parameters["generalBackgroundOffset"].SetValue(offset);
                    break;

                // Background 2.
                case 1:
                    offset = -Vector2.UnitY * Main.GlobalTimeWrappedHourly * 0.027f;
                    effect.Parameters["generalBackgroundOffset"].SetValue(offset);
                    break;
            }
        }
    }
}
