using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class Crimera : ModProjectile
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.Crimera}";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimera");
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 490;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI() => Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.Yellow;
            drawColor.A = 0;
            drawColor *= 0.5f;

            Utilities.DrawAfterimagesCentered(Projectile, drawColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return true;
        }
    }
}
