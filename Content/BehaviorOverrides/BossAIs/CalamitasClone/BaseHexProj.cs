using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public abstract class BaseHexProj : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public ref float HorizontalOffset => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Calamitous Hex");
            Main.projFrames[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 66;
            Projectile.height = 86;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5;
            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.Kill();

            Projectile.Bottom = Owner.Top + Vector2.UnitX * HorizontalOffset;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
