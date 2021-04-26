using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SignusNPC = CalamityMod.NPCs.Signus.Signus;

namespace InfernumMode.FuckYouModeAIs.Sentinels
{
    public class OrbitingScythe : ModProjectile
    {
        float radius = 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Scythe");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 18;
            projectile.height = 34;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 350;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.rotation += 0.15f;
            projectile.ai[0] += 0.15f;
            if (projectile.timeLeft >= 310)
            {
                radius += 900f / 40f;
                projectile.damage = 0;
            }
            else projectile.damage = 100;
            if (projectile.timeLeft < 20f)
            {
                projectile.damage = 0;
                radius -= 900f / 20f;
            }
            radius = MathHelper.Clamp(radius, 0f, 900f);
            if (!NPC.AnyNPCs(ModContent.NPCType<SignusNPC>()))
            {
                projectile.active = false;
                return;
            }
            projectile.Center = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<SignusNPC>())].Center + projectile.ai[0].ToRotationVector2() * radius;

            Lighting.AddLight(projectile.Center, 0.65f, 0.12f, 0.6f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }
    }
}
