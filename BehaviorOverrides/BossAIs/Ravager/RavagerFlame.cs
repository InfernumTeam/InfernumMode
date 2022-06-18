using CalamityMod.NPCs;
using InfernumMode.Dusts;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RitualFlame : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Blue Flame");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 8;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.extraUpdates = 1;
            projectile.timeLeft = 540;
        }

        public override void AI()
        {
            Dust blueFlame = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<RavagerMagicDust>(), 0f, 0f, 100, default, 1.5f);
            blueFlame.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
            blueFlame.noGravity = true;

            if (projectile.ai[0] == 0f)
            {
                projectile.ai[0] = 1f;
                Main.PlaySound(SoundID.Item20, projectile.position);
            }

            // Fuck off if the Ravager isn't around.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                projectile.active = false;
                return;
            }

            NPC ravager = Main.npc[CalamityGlobalNPC.scavenger];

            projectile.velocity = (projectile.velocity * 24f + projectile.SafeDirectionTo(ravager.Center) * 13f) / 25f;
            if (projectile.WithinRange(ravager.Center, 100f))
                projectile.Kill();
        }
    }
}
