using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class ArcingBrimstoneDart : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Dart");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
            
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.2f, 0f, 1f);

            Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);

            float maxSpeed = 16f;
            float acceleration = 1.034f;
            if (CalamityGlobalNPC.calamitas != -1 && Main.player[Main.npc[CalamityGlobalNPC.calamitas].target].Infernum_CalShadowHex().HexIsActive("Zeal"))
            {
                maxSpeed = 21.5f;
                acceleration = 1.031f;
            }

            // Home in weakly if the shadow's target has the appropriate hex.
            if (CalamityGlobalNPC.calamitas != -1 && Main.player[Main.npc[CalamityGlobalNPC.calamitas].target].Infernum_CalShadowHex().HexIsActive("Accentuation"))
            {
                float idealDirection = Projectile.AngleTo(Main.player[Main.npc[CalamityGlobalNPC.calamitas].target].Center);
                Projectile.velocity = Projectile.velocity.RotateTowards(idealDirection, 0.006f);
            }

            if (Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= acceleration;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBackglow(Color.HotPink with { A = 0 }, Projectile.Opacity * 2f);
            LumUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
