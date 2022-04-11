using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastWave : ModProjectile
    {
        public float Radius
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public float MaxRadius
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        public const int Lifetime = 80;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shockwave");
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                MaxRadius = Main.rand.NextFloat(2000f, 2200f);
                Projectile.localAI[0] = 1f;
            }

            Radius = MathHelper.Lerp(Radius, MaxRadius, 0.15f);
            Projectile.scale = MathHelper.Lerp(1.2f, 5f, Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));
            CalamityGlobalProjectile.ExpandHitboxBy(Projectile, (int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            float pulseCompletionRatio = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            Vector2 scale = new(1.5f, 1f);
            DrawData drawData = new(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Polterghast/NecroplasmicRoar").Value,
                Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f - (float)Math.Sqrt(pulseCompletionRatio))) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None, 0);

            Color pulseColor = Color.Lerp(Color.Cyan, Color.Pink * 0.65f, MathHelper.Clamp(pulseCompletionRatio * 1.75f, 0f, 1f));
            GameShaders.Misc["ForceField"].UseColor(pulseColor);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
