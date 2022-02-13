using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class TornadoBorder : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public const float TornadoHeight = 60000f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tornado");
        }

        public override void SetDefaults()
        {
            projectile.width = 50;
            projectile.height = 1020;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 9000000;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.width = (int)MathHelper.Lerp(projectile.width, 200f, 0.085f);
            if (!CalamityPlayer.areThereAnyDamnBosses)
            {
                projectile.active = false;
                projectile.netUpdate = true;
                return;
            }

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
        }

        internal Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.BlueViolet, Color.Black, 0.85f) * projectile.Opacity * 1.6f;
        }

        internal float WidthFunction(float completionRatio) => projectile.width + 10f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                projectile.Bottom + Vector2.UnitY * TornadoHeight * 0.48f,
                projectile.Bottom - Vector2.UnitY * TornadoHeight * 0.48f,
                (int)(projectile.width * 0.525),
                ref _);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));
            Vector2 upwardAscent = Vector2.UnitY * TornadoHeight * 0.5f;
            Vector2 top = projectile.Bottom - upwardAscent;
            List<Vector2> drawPoints = new List<Vector2>()
            {
                top
            };
            for (int i = 0; i < 15; i++)
                drawPoints.Add(Vector2.Lerp(top, projectile.Bottom + upwardAscent, i / 14f));

            TornadoDrawer.Draw(drawPoints, -Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
