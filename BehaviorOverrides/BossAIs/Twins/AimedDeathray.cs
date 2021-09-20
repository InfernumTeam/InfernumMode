using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Guardians
{
    public class AimedDeathray : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 1.05f;
        public override float MaxLaserLength => 2820f;
        public override float Lifetime => TelegraphTime + LaserDamageTime;
        public override Color LaserOverlayColor => Color.Lerp(Color.IndianRed, Color.Red, 0.6f) * 1.2f;
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayStart");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd");

        internal const float TelegraphTime = 120f;
        internal const float LaserDamageTime = 120f;

        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deathray");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.alpha = 255;
        }

        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange((int)projectile.ai[1]) || !Main.npc[(int)projectile.ai[1]].active)
                projectile.Kill();

            projectile.velocity = (Main.npc[(int)projectile.ai[1]].rotation + MathHelper.PiOver2).ToRotationVector2();
            projectile.Center = Main.npc[(int)projectile.ai[1]].Center + projectile.velocity * 96f;
        }

        public override void DetermineScale()
        {
            float maxScale = Time > TelegraphTime ? MaxScale : MaxScale * 0.25f;
            projectile.scale = MathHelper.Lerp(projectile.scale, (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * 5f * maxScale, 0.1f);
            if (projectile.scale > maxScale)
                projectile.scale = maxScale;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool CanDamage() => Time > TelegraphTime;
    }
}
