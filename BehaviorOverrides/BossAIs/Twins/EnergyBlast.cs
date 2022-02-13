using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class EnergyBlast : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blast");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 140;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0.55f, 0.25f, 0f);
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item20, projectile.position);
                projectile.localAI[0] += 1f;
            }

            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust redEnergy = Dust.NewDustPerfect(projectile.Center, 130);
                redEnergy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f, 18f);
                redEnergy.scale = Main.rand.NextFloat(1.3f, 1.55f);
            }
        }
    }
}
