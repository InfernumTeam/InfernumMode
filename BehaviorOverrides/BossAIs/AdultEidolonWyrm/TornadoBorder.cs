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
            Projectile.width = 50;
            Projectile.height = 1020;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 9000000;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.width = (int)MathHelper.Lerp(Projectile.width, 200f, 0.085f);
            if (!CalamityPlayer.areThereAnyDamnBosses)
            {
                Projectile.active = false;
                Projectile.netUpdate = true;
                return;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
        }

        internal Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.BlueViolet, Color.Black, 0.85f) * Projectile.Opacity * 1.6f;
        }

        internal float WidthFunction(float completionRatio) => Projectile.width + 10f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Bottom + Vector2.UnitY * TornadoHeight * 0.48f,
                Projectile.Bottom - Vector2.UnitY * TornadoHeight * 0.48f,
                (int)(Projectile.width * 0.525),
                ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));
            Vector2 upwardAscent = Vector2.UnitY * TornadoHeight * 0.5f;
            Vector2 top = Projectile.Bottom - upwardAscent;
            List<Vector2> drawPoints = new()
            {
                top
            };
            for (int i = 0; i < 15; i++)
                drawPoints.Add(Vector2.Lerp(top, Projectile.Bottom + upwardAscent, i / 14f));

            TornadoDrawer.Draw(drawPoints, -Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
