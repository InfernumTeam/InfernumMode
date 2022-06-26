using CalamityMod.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class BombingTelegraph : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];
        public Player Target => Main.player[Projectile.owner];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.01f;
        }

        public override void AI()
        {
            if (Countdown > 0f)
                Countdown--;
            else
            {
                if (Projectile.ai[1] == 1f)
                    SoundEngine.PlaySound(HandheldTank.UseSound, Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 missileSpawnPosition = new Vector2(Projectile.Center.X, Target.Center.Y) - Vector2.UnitY.RotatedBy(Projectile.rotation) * 1000f;
                    Vector2 missileVelocity = Vector2.UnitY.RotatedBy(Projectile.rotation) * 29f;
                    int missile = Utilities.NewProjectileBetter(missileSpawnPosition, missileVelocity, ModContent.ProjectileType<PlagueMissile2>(), 170, 0f);
                    if (Main.projectile.IndexInRange(missile))
                        Main.projectile[missile].ai[0] = Target.whoAmI;
                }

                Projectile.Kill();
            }

            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.05f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * 4350f;
            Vector2 end = Projectile.Center + Vector2.UnitY.RotatedBy(Projectile.rotation) * 4350f;
            Utilities.DrawLineBetter(Main.spriteBatch, start, end, Color.Lime * Projectile.scale, Projectile.scale * 3f);
            return false;
        }
    }
}
