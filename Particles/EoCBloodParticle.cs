using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Particles
{
    public class EoCBloodParticle : Particle
    {
        public Color InitialColor;

		public float Gravity;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override string Texture => "CalamityMod/Particles/Blood";

		public EoCBloodParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color, float gravity = 22)
		{
			Position = relativePosition;
			Velocity = velocity;
			Scale = scale;
			Lifetime = lifetime;
			Color = InitialColor = color;
			Gravity = gravity;
		}

		public override void Update()
		{
			Scale *= 0.98f;
			Velocity.X *= 0.97f;
			// Gravity gets changed here.
			Velocity.Y = MathHelper.Clamp(Velocity.Y + 0.9f, -22f, Gravity);
			Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow((double)LifetimeCompletion, 3.0));
			Rotation = Velocity.ToRotation() + (float)Math.PI / 2f;
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{			
			float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(Velocity.Y), clamped: true) * 0.84f;
			float brightness = (float)Math.Pow((double)Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15);
			Vector2 scale = new Vector2(1f, verticalStretch + 1f) * Scale * 0.1f;
			Texture2D texture = ModContent.Request<Texture2D>(Texture, (AssetRequestMode)2).Value;
			spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * brightness, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
		}
	}
}
