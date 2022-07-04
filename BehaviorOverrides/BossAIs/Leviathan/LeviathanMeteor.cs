using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
    public class LeviathanMeteor : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Leviathan Meteor");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 154;
            Projectile.height = 154;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 125;
            Projectile.Opacity = 0f;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }
        
        public override void AI()
        {
            // Determine opacity and rotation.
            Projectile.scale = Utils.GetLerpValue(0f, 18f, Time, true);
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Projectile.scale;
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.velocity *= 0.987f;

            Lighting.AddLight(Projectile.Center, 0f, 0f, 0.5f * Projectile.Opacity);
            Time++;
        }

        public override bool CanHitPlayer(Player target) => Projectile.scale >= 0.9f;

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 15; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 15f).ToRotationVector2() * 12f;
                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<LeviathanVomit>(), 175, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
