using InfernumMode.Common.Graphics.Fluids;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public class QueenSlimeExplosion : ModProjectile
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        internal FluidFieldInfernum ExplosionDrawer;

        public bool CanUpdate => Main.LocalPlayer.WithinRange(Projectile.Center, 900f);

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Slime Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 96;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            if (ExplosionDrawer is not null && CanUpdate)
                ExplosionDrawer.ShouldUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!CanUpdate)
                return false;

            ExplosionDrawer ??= new(Projectile.width, Projectile.height, new(0.01f, 2.9f, 0.98f, 1.19f, 0.38f, -0.17f, 0.98f));

            // Draw the explosion.
            float explosionCompletion = Utils.GetLerpValue(0f, 16f, Time, true);
            if (explosionCompletion < 1f)
            {
                ExplosionDrawer.MovementUpdateSteps = 2;
                for (int i = 0; i < 16; i++)
                {
                    Vector2 sourceOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 2f;
                    Color explosionColor = Color.Lerp(Color.HotPink, Color.DeepSkyBlue, MathF.Pow(explosionCompletion, 3f));
                    ExplosionDrawer.CreateSource((ExplosionDrawer.Center.ToVector2() + sourceOffset).ToPoint(), Vector2.One * 3f, sourceOffset * 100f, explosionColor, 1f);
                }
            }
            ExplosionDrawer.Draw(Projectile.Center - Main.screenPosition, Projectile.scale * 2f);
            ExplosionDrawer.ShouldUpdate = false;

            return false;
        }

        public override void Kill(int timeLeft) => ExplosionDrawer?.Dispose();

        public override bool? CanDamage() => false;
    }
}
