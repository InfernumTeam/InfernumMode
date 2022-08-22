using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class NonConnectingAresTeslaOrb : ModProjectile
    {
        public ref float Identity => ref Projectile.ai[0];
        public PrimitiveTrail LightningDrawer;
        public PrimitiveTrail LightningBackgroundDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tesla Sphere");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 125;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.01f;

            Projectile.Opacity = Utils.GetLerpValue(125f, 120f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);

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
                    Dust electricity = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 206 : 229);
                    electricity.position += Main.rand.NextVector2Circular(20f, 20f);
                    electricity.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 16f);
                    electricity.fadeIn = 1f;
                    electricity.color = Color.Cyan * 0.6f;
                    electricity.scale *= Main.rand.NextFloat(1.5f, 2f);
                    electricity.noGravity = true;
                }
                Projectile.localAI[0] = 1f;
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.Electrified, 240);
        }

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
            SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            
        }
    }
}
