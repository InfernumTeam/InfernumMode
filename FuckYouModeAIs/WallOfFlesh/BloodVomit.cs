using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.WallOfFlesh
{
    public class BloodVomit : ModProjectile
    {
        public const float Gravity = 0.3f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, Color.DarkRed.ToVector3());

            projectile.velocity.Y += Gravity;
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            projectile.tileCollide = projectile.timeLeft <= 150;

            GenerateIdleDust();
        }

        internal void GenerateIdleDust()
        {
            if (Main.dedServ)
                return;

            Dust blood = Dust.NewDustPerfect(projectile.Center - projectile.velocity, DustID.Blood);
            blood.noGravity = true;
            blood.scale = 1.5f;
            blood.alpha = 38;
        }

		public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Dust blood = Dust.NewDustDirect(projectile.position - projectile.velocity, 2, 2, DustID.Blood, 0f, 0f, 0, default, 1f);
                blood.position.X -= 2f;
                blood.velocity *= 0.1f;
                blood.velocity -= projectile.velocity * 0.025f;
                blood.scale = 0.75f;
                blood.alpha = 38;
            }
        }
	}
}
