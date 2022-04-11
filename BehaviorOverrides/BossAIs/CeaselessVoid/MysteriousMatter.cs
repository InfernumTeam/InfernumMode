using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class MysteriousMatter : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mysterious Matter");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 22f, Time, true) * Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

            // Fire a bunch of ceasless energy at the nearest target once at the apex of the projectile's lifetime.
            if (Time == 30f)
            {
                Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item28, closestTarget.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.63f, 0.63f, i / 2f);
                        Vector2 shootVelocity = Projectile.SafeDirectionTo(closestTarget.Center).RotatedByRandom(offsetAngle) * 4f;
                        Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<CeaselessEnergy>(), 250, 0f);
                    }
                }
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float alpha = Utils.GetLerpValue(0f, 30f, Time, true);
            return new Color(1f, 1f, 1f, alpha) * Projectile.Opacity * MathHelper.Lerp(0.6f, 1f, alpha);
        }

        public override bool CanDamage() => Projectile.Opacity >= 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float scale = Projectile.scale;
            spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
