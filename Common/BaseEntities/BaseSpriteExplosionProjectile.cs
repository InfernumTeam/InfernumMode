using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.BaseEntities
{
    public abstract class BaseSpriteExplosionProjectile : ModProjectile
    {
        public abstract Color ExplosionColor { get; }

        public abstract int GetFrameUpdateRate { get; }

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Common/BaseEntities/BaseSpriteExplosionProjectile";

        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
            Projectile.width = Projectile.height = 4;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = GetFrameUpdateRate * Main.projFrames[Type] + 1;
        }

        public override void AI()
        {
            Projectile.frame = (int)(Time / GetFrameUpdateRate) % Main.projFrames[Type];
            Time++;
        }

        public sealed override Color? GetAlpha(Color lightColor) => ExplosionColor * Projectile.Opacity;
    }
}
