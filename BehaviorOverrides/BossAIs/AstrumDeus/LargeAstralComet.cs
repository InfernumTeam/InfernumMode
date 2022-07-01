using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class LargeAstralComet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Comet");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            Projectile.velocity.X = (Projectile.velocity.X * 11f + Projectile.SafeDirectionTo(target.Center).X * 16f) / 12f;
            Projectile.velocity.Y *= 1.01f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.35f, 0.16f, 0.095f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, Projectile.alpha);

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 96;
            Projectile.position -= Projectile.Size * 0.5f;

            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            Projectile.Damage();
        }
    }
}
