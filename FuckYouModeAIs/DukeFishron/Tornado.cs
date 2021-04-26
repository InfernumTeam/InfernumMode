using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DukeFishron
{
    public class Tornado : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float TornadoHeight => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tornado");
        }

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 1020;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.width = (int)MathHelper.Lerp(projectile.width, 200f, 0.05f);
            TornadoHeight = MathHelper.Lerp(TornadoHeight, 1600f, 0.05f);
            if (!CalamityPlayer.areThereAnyDamnBosses)
            {
                projectile.active = false;
                projectile.netUpdate = true;
                return;
            }

            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 300f) * 10f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
        }

        internal Color ColorFunction(float completionRatio)
		{
            return Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTime))) * projectile.Opacity;
		}

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(projectile.width * 0.6f, projectile.width + 16f, 1f - completionRatio);
        }

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), 
                targetHitbox.Size(), 
                projectile.Bottom, 
                projectile.Bottom - Vector2.UnitY * TornadoHeight,
                (int)(projectile.width * 0.525), 
                ref _);
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));
            Vector2[] drawPoints = new Vector2[5];
            Vector2 upwardAscent = Vector2.UnitY * TornadoHeight * 0.65f;

            Vector2 bottom = projectile.Bottom + Vector2.UnitY * 80f;
            Vector2 top = bottom - upwardAscent;
            for (int i = 0; i < drawPoints.Length - 1; i++)
                drawPoints[i] = Vector2.Lerp(top, bottom, i / (float)(drawPoints.Length - 1));

            drawPoints[drawPoints.Length - 1] = bottom;
            TornadoDrawer.Draw(drawPoints, -Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
