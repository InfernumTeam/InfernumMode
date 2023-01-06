using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class HotMetal : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Superheated Metal");
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Initialize frames.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.067f, 0f, 1f);
            Projectile.rotation += Projectile.velocity.Y * 0.02f;

            Projectile.velocity.X *= 0.993f;
            if (Projectile.velocity.Y < 11f)
                Projectile.velocity.Y += 0.25f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(Color.White with { A = 192 }, Color.Red with { A = 156 }, Projectile.ai[0]) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Rectangle frame = TextureAssets.Projectile[Projectile.type].Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 4f, frame);
            return false;
        }
    }
}
