using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

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
            Projectile.width = Projectile.height = 140;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0] += 1f;
            }

            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust redEnergy = Dust.NewDustPerfect(Projectile.Center, 130);
                redEnergy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f, 18f);
                redEnergy.scale = Main.rand.NextFloat(1.3f, 1.55f);
            }
        }
    }
}
