using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarFlare : ModProjectile
    {
        public ref float Countdown => ref projectile.ai[0];
        public Player Target => Main.player[projectile.owner];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Flare");
        }

        public override void SetDefaults()
        {
            projectile.width = 4;
            projectile.height = 4;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.extraUpdates = 5;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (projectile.ai[1] != -1f && projectile.position.Y > projectile.ai[1])
                projectile.tileCollide = true;

            if (projectile.position.HasNaNs())
            {
                projectile.Kill();
                return;
            }
            Dust electrivity = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 229, 0f, 0f, 0, default, 1f);
            electrivity.position = projectile.Center;
            electrivity.velocity = Vector2.Zero;
            electrivity.noGravity = true;
            if (WorldGen.SolidTile(Framing.GetTileSafely((int)projectile.position.X / 16, (int)projectile.position.Y / 16)))
                electrivity.noLight = true;

            if (projectile.ai[1] == -1f)
            {
                projectile.ai[0]++;

                if (projectile.ai[0] == 2f)
                    Main.PlaySound(SoundID.Item122, projectile.Center);

                projectile.velocity = Vector2.Zero;
                projectile.tileCollide = false;
                projectile.penetrate = -1;
                projectile.alpha = Utils.Clamp(projectile.alpha - 10, 0, 255);

                projectile.frameCounter++;
                if (projectile.frameCounter >= projectile.MaxUpdates * 3)
                {
                    projectile.frameCounter = 0;
                    projectile.frame++;
                }
                if (projectile.ai[0] >= Main.projFrames[projectile.type] * projectile.MaxUpdates * 3)
                {
                    projectile.Kill();
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            return false;
        }
    }
}
