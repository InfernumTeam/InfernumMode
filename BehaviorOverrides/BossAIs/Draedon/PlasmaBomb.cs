using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class PlasmaBomb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Bomb");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 270;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(270f, 265f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true);

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.1f * Projectile.Opacity, 0.25f * Projectile.Opacity, 0.25f * Projectile.Opacity);

            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Create a burst of dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 60; i++)
                {
                    Dust plasma = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 110 : 107);
                    plasma.position += Main.rand.NextVector2Circular(20f, 20f);
                    plasma.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 16f);
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Lime * 0.6f;
                    plasma.scale *= Main.rand.NextFloat(1.5f, 2f);
                    plasma.noGravity = true;
                }
                Projectile.localAI[0] = 1f;
            }

            // Slow down over time.
            Projectile.velocity *= 0.96f;
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.CursedInferno, 300);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Color.White * Projectile.Opacity, 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into electric sparks on death.
            for (int i = 0; i < 7; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2() * 6f;
                Utilities.NewProjectileBetter(Projectile.Center, sparkVelocity, ModContent.ProjectileType<TypicalPlasmaSpark>(), 500, 0f);
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
