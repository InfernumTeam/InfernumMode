using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class LeechFeeder : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Feeder");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Be invisible outside of water.
            if (!Collision.WetCollision(Projectile.TopLeft, Projectile.width, Projectile.height))
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.06f, 0f, 1f);

            // Look at the target.
            NPC target = Main.npc[(int)Projectile.ai[0]];

            // Disappear if the target has no more flesh to consume or no longer exists.
            if (!target.active || target.localAI[1] >= 1f)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * -8f;
                Projectile.Opacity -= 0.02f;
            }

            // Consume the flesh of the target if there is any.
            else
            {
                if (!Projectile.WithinRange(target.Center, 45f))
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 6f, 0.08f);
                if (Projectile.Hitbox.Intersects(target.Hitbox))
                {
                    target.localAI[1] += 0.01f;
                    Dust.NewDustDirect(target.TopLeft, target.width, target.height, (int)CalamityDusts.SulfurousSeaAcid);

                    if (Main.rand.NextBool(150))
                        SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
                }

                Projectile.Opacity += 0.02f;
            }

            Projectile.spriteDirection = Projectile.velocity.X.DirectionalSign();
        }
    }
}
