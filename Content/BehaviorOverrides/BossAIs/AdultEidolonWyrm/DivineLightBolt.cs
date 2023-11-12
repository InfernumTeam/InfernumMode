using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class DivineLightBolt : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Divine Light Bolt");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Accelerate over time.
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = 35f;
            if (Projectile.velocity.Length() < Projectile.ai[1])
                Projectile.velocity *= 1.034f;

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 108, 50, 0) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0));
            return false;
        }
    }
}
