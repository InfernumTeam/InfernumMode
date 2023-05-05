using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralPlasmaSpark : ModProjectile
    {
        public bool Cyan => Projectile.ai[0] == 1f;

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Plasma Spark");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide frames and rotation.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 32f, Projectile.timeLeft, true);

            // Weakly home in on the target before accelerating.
            if (Time < 135f)
            {
                float flySpeed = BossRushEvent.BossRushActive ? 11f : 9f;
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (!Projectile.WithinRange(target.Center, 200f))
                    Projectile.velocity = (Projectile.velocity * 39f + Projectile.SafeDirectionTo(target.Center) * flySpeed) / 40f;
            }
            else if (Projectile.velocity.Length() < 14.5f)
                Projectile.velocity *= 1.015f;

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.8f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (Cyan)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AstrumDeus/AstralPlasmaSparkCyan").Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
