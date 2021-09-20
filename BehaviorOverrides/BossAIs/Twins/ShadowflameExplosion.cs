using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{   
    public class ShadowflameExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadowflame");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 80;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 120;
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

            for (int i = 0; i < 5; i++)
            {
                Dust shadowflame = Dust.NewDustPerfect(projectile.Center, 27);
                shadowflame.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(7f, 11f);
                shadowflame.scale = Main.rand.NextFloat(1.15f, 1.35f);
                shadowflame.noGravity = true;
            }
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 90);
        }
	}
}
