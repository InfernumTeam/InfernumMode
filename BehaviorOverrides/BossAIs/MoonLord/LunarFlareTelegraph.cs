using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarFlareTelegraph : ModProjectile
    {
        public ref float Countdown => ref projectile.ai[0];
        public Player Target => Main.player[projectile.owner];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            projectile.scale = 0.01f;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                projectile.netUpdate = true;
            }

            if (MoonLordCoreBehaviorOverride.CurrentActiveArms <= 0)
            {
                projectile.active = false;
                return;
            }

            if (Countdown > 0f)
                Countdown--;
            else
            {
                if (projectile.ai[1] == 1f)
                    Main.PlaySound(SoundID.Item92, Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 flareSpawnPosition = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation) * 1000f;
                    if (flareSpawnPosition.Y < 100f)
                        flareSpawnPosition.Y = 100f;

                    Vector2 flareVelocity = Vector2.UnitY.RotatedBy(projectile.rotation) * Main.rand.NextFloat(11f, 13f);
                    int flare = Utilities.NewProjectileBetter(flareSpawnPosition, flareVelocity, ProjectileID.PhantasmalBolt, 205, 0f);
                    if (Main.projectile.IndexInRange(flare))
                    {
                        Main.projectile[flare].ai[1] = Target.Center.Y + 400f;
                        Main.projectile[flare].tileCollide = false;
                    }
                }

                projectile.Kill();
            }

            projectile.scale = MathHelper.Clamp(projectile.scale + 0.08f, 0f, 1f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation) * 4350f;
            Vector2 end = projectile.Center + Vector2.UnitY.RotatedBy(projectile.rotation) * 4350f;
            Color lineColor = new Color(50, 255, 156);
            Utilities.DrawLineBetter(spriteBatch, start, end, lineColor * projectile.scale, projectile.scale * 3f);
            return false;
        }
    }
}
