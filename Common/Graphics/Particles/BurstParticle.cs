using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class BurstParticle : Particle
    {
        public static Texture2D BurstBloom
        {
            get;
            private set;
        }

        public readonly bool UseExtraBloom;

        public readonly Color ExtraBloomColor;

        public float Opacity;

        public Vector2 DrawScale;

        // Particle limit can suck my ass.
        public override bool Important => true;

        // Why do I need to specify this??
        public override bool SetLifetime => true;

        // Or this??
        public override bool UseCustomDraw => true;

        // Is this seriously not auto-gotten?????
        public override string Texture => "InfernumMode/Common/Graphics/Particles/BurstParticle";

        public BurstParticle(Vector2 position, Vector2 velocity, Color drawColor, int lifetime, bool useExtraBloom = false, Color? extraBloomColor = null)
        {
            BurstBloom ??= ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Particles/BurstParticleBloom", AssetRequestMode.ImmediateLoad).Value;
            Position = position;
            Velocity = velocity;
            Color = drawColor;
            Lifetime = lifetime;
            UseExtraBloom = useExtraBloom;
            ExtraBloomColor = extraBloomColor ?? Color.White;
            Opacity = 1;
        }
        public override void Update()
        {
            Opacity = Lerp(1f, 0f, CalamityUtils.SineInOutEasing(LifetimeCompletion, 1));
            DrawScale += Vector2.One * 0.28f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D mainTexture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(mainTexture, Position - Main.screenPosition, null, Color with { A = 0 } * Opacity, Rotation, mainTexture.Size() * 0.5f, DrawScale, SpriteEffects.None, 0f);

            if (UseExtraBloom)
                spriteBatch.Draw(BurstBloom, Position - Main.screenPosition, null, ExtraBloomColor with { A = 0 } * 0.4f * Opacity, Rotation, BurstBloom.Size() * 0.5f, DrawScale, SpriteEffects.None, 0f);
        }
    }
}
