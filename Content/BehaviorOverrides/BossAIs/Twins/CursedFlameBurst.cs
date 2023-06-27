using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class CursedFlameBurst : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FireDrawer;

        public const int Lifetime = 240;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Sin(Projectile.timeLeft / (float)Lifetime * Pi) * 5f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= BossRushEvent.BossRushActive ? 1.056f : 1.03f;

            Lighting.AddLight(Projectile.Center, Color.Green.ToVector3() * 1.6f);

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center, 27);
                fire.velocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                fire.scale *= 1.1f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 bottom = Projectile.oldPos[i + 1] + Projectile.Size * 0.5f;

                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 24f, ref _))
                    return true;
            }
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Pow(Utils.GetLerpValue(0f, 0.27f, completionRatio, true), 0.4f) * Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return SmoothStep(3f, Projectile.width, squeezeInterpolant) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Green, Color.LimeGreen, Pow(completionRatio, 2f));
            color *= 1f - 0.5f * Pow(completionRatio, 3f);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            InfernumEffectsRegistry.FireVertexShader.UseSaturation(0.4f);
            InfernumEffectsRegistry.FireVertexShader.UseImage1("Images/Misc/Perlin");
            FireDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 38);
        }
    }
}
