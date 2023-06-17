using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class WaterBolt : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Water Spear");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Create ice dust on the first frame.
            if (Projectile.localAI[1] == 0f)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust ice = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 33, 0f, 0f, 100, default, 2f);
                    ice.velocity *= 3f;
                    if (Main.rand.NextBool())
                    {
                        ice.scale = 0.5f;
                        ice.fadeIn = Main.rand.NextFloat(1f, 2f);
                    }
                }
                Projectile.localAI[1] = 1f;
            }

            // Decide frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }

            // Determine opacity and rotation.
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 30f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
            Lighting.AddLight(Projectile.Center, 0f, 0f, 0.5f * Projectile.Opacity);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.6f;

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, 33, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
    }
}
