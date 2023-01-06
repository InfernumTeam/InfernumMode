using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
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
            Projectile.width = Projectile.height = 210;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
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

            for (int i = 0; i < 13; i++)
            {
                Dust energy = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(2) ? 132 : 130);
                energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(15f, 22f);
                energy.scale = Main.rand.NextFloat(1.3f, 1.65f);
                energy.noGravity = true;
            }
        }
    }
}
