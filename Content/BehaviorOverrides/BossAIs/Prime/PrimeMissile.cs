using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeMissile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Missile");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 14f)
                Projectile.velocity *= 1.02f;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());

            // Interact with tiles after enough time has passed.
            Projectile.tileCollide = Time > 75f;

            // Very, very weakly home in on players.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead)
            {
                float oldSpeed = Projectile.velocity.Length();
                Projectile.velocity = (Projectile.velocity * 90f + Projectile.SafeDirectionTo(target.Center) * oldSpeed) / 91f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            }

            Time++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Explode when a tile is hit.
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                int randomDustType = Main.rand.NextBool(2) ? 222 : 219;
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 1f);
                fire.velocity *= 3f;
                fire.noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    fire.scale = 0.5f;
                    fire.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 15; i++)
            {
                int randomDustType = Main.rand.NextBool(2) ? 222 : 219;
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 1.3f);
                fire.noGravity = true;
                fire.velocity *= 5f;

                fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 1f);
                fire.noGravity = true;
                fire.velocity *= 2f;
            }

            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }
    }
}
