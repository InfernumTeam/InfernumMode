using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class BigEnergyBlast : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blast");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 210;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 45;
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

            for (int i = 0; i < 13; i++)
            {
                Dust energy = Dust.NewDustPerfect(projectile.Center, Main.rand.NextBool(2) ? 132 : 130);
                energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(15f, 22f);
                energy.scale = Main.rand.NextFloat(1.3f, 1.65f);
                energy.noGravity = true;
            }
        }
    }
}
