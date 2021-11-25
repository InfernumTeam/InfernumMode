using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonlordPendulum : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => Main.npc[OwnerIndex].type == ModContent.NPCType<EldritchSeal>() ? 120f : 300f;
        public override Color LaserOverlayColor => new Color(200, 200, 200, 0) * 0.9f;
        public override Color LightCastColor => new Color(0.3f, 0.65f, 0.7f);
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGDeathray");
        public override Texture2D LaserMiddleTexture => Main.extraTexture[21];
        public override Texture2D LaserEndTexture => Main.extraTexture[22];
        public override float MaxLaserLength => 3000f;
        public override float MaxScale => 0.5f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Deathray");
        }

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 570;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void PostAI()
        {
            if (Main.npc[(int)projectile.ai[1]].type == ModContent.NPCType<EldritchSeal>())
                projectile.damage = Time >= 30f ? 70 : 0;
        }
    }
}
