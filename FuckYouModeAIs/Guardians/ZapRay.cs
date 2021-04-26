using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Guardians
{
    public class ZapRay : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 0.8f;
        public override float MaxLaserLength => 2200f;
        public override float Lifetime => 60f;
        public override Color LaserOverlayColor => Color.LightGoldenrodYellow;
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayStart");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd");

        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Zap Ray");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
        }

        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange((int)projectile.ai[1]) || !Main.npc[(int)projectile.ai[1]].active)
                projectile.Kill();
            projectile.Center = (Main.npc[(int)projectile.ai[1]].modNPC as EtherealHand).PointerFingerPosition + projectile.velocity * 8f;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
