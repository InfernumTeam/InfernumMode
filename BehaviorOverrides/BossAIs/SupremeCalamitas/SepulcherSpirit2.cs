using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherSpirit2 : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float SpiritHue => ref projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sepulcher Spirit");
            Main.projFrames[projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(0f, 24f, Time, true);

            // Attempt to hover above the target.
            Vector2 destination = Main.player[projectile.owner].Center;
            if (Time < 15f)
            {
                float flySpeed = MathHelper.Lerp(8f, 20f, Time / 15f);
                projectile.velocity = (projectile.velocity * 9f + projectile.SafeDirectionTo(destination) * flySpeed) / 10f;
            }
            else if (projectile.velocity.Length() < 43f)
            {
                projectile.velocity *= 1.035f;
                if (Time < 25f)
                    projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(destination), 0.05f);
                projectile.tileCollide = true;
            }

            if (Time == 15f && Main.rand.NextBool(12))
                Main.PlaySound(SoundID.Item8, projectile.Center);

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Time++;
        }

		public override void Kill(int timeLeft)
		{
            Main.PlaySound(SoundID.NPCDeath52, projectile.Center);
            for (int i = 0; i < 5; i++)
			{
                Dust fire = Dust.NewDustDirect(projectile.Center - Vector2.One * 12f, 6, 6, 267);
                fire.color = Color.Red;
                fire.noGravity = true;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Main.hslToRgb(SpiritHue, 1f, 0.5f);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1, Main.projectileTexture[projectile.type], false);
            return false;
        }
    }
}
