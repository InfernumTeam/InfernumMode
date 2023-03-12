using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Ogre
{
    public class BouncingSpitBall : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spit Ball");

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 470;
            Projectile.penetrate = -1;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(470f, 463f, Projectile.timeLeft, true);
            Projectile.velocity.Y += 0.2f;
            Projectile.rotation += Projectile.velocity.X * 0.04f;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.75f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.Y = MathHelper.Clamp(oldVelocity.Y * -1.2f, -28f, 28f);
            Projectile.velocity.Y += Projectile.SafeDirectionTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center).Y * 10f;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
            for (int i = 0; i < 4; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                        4,
                        256
                });
                Dust spit = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, Projectile.velocity.X, Projectile.velocity.Y, 100);
                spit.velocity = spit.velocity / 4f + Projectile.velocity / 2f - Vector2.UnitY * 5f;
                spit.scale = Main.rand.NextFloat(1f, 1.45f);
                spit.position = Projectile.Center;
                spit.position += Main.rand.NextVector2Circular(Projectile.width, Projectile.width) * 2f;
                spit.noLight = true;
                if (spit.type == 4)
                    spit.color = new Color(80, 170, 40, 120);
            }
            return false;
        }
    }
}
