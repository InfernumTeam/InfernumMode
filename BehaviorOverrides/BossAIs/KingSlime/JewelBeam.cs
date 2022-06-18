using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.KingSlime
{
    public class JewelBeam : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Beam");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 8;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
        }

        public override void AI()
        {
            if (Main.dedServ)
                return;

            // Release a burst of circular dust on the first frame.
            if (projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 36; i++)
                {
                    Dust gleamingBurst = Dust.NewDustPerfect(projectile.Center, 264);
                    gleamingBurst.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 2.5f;
                    gleamingBurst.color = Color.Red;
                    gleamingBurst.noLight = true;
                    gleamingBurst.noGravity = true;
                    gleamingBurst.fadeIn = 0.4f;
                    gleamingBurst.scale = 1.2f;
                }
                projectile.localAI[0] = 1f;
            }

            Dust gleamingRed = Dust.NewDustPerfect(projectile.Center, 182);
            gleamingRed.velocity = Vector2.Zero;
            gleamingRed.noGravity = true;
            gleamingRed.scale = 1.05f;

            for (int direction = -1; direction <= 1; direction += 2)
            {
                gleamingRed = Dust.NewDustPerfect(projectile.Center, 182);
                gleamingRed.velocity = -projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Pi + direction * 0.53f).RotatedByRandom(0.06f) * Main.rand.NextFloat(0.9f, 1.1f) * 3f;
                gleamingRed.noGravity = true;
                gleamingRed.scale = Main.rand.NextFloat(1.3f, 1.45f);
            }
        }
    }
}
