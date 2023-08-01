using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class FistBulletTelegraph : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 45;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = Sin(Pi * Time / 45f);
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * 7000f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.Orange * 0.4f, Projectile.scale * 4f);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
