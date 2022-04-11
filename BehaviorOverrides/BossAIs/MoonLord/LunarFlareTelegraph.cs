using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarFlareTelegraph : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];
        public Player Target => Main.player[Projectile.owner];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.scale = 0.01f;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            if (MoonLordCoreBehaviorOverride.CurrentActiveArms <= 0)
            {
                Projectile.active = false;
                return;
            }

            if (Countdown > 0f)
                Countdown--;
            else
            {
                if (Projectile.ai[1] == 1f)
                    SoundEngine.PlaySound(SoundID.Item92, Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 flareSpawnPosition = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * 1000f;
                    if (flareSpawnPosition.Y < 100f)
                        flareSpawnPosition.Y = 100f;

                    Vector2 flareVelocity = Vector2.UnitY.RotatedBy(Projectile.rotation) * Main.rand.NextFloat(11f, 13f);
                    int flare = Utilities.NewProjectileBetter(flareSpawnPosition, flareVelocity, ProjectileID.PhantasmalBolt, 205, 0f);
                    if (Main.projectile.IndexInRange(flare))
                    {
                        Main.projectile[flare].ai[1] = Target.Center.Y + 400f;
                        Main.projectile[flare].tileCollide = false;
                    }
                }

                Projectile.Kill();
            }

            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.08f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * 4350f;
            Vector2 end = Projectile.Center + Vector2.UnitY.RotatedBy(Projectile.rotation) * 4350f;
            Color lineColor = new(50, 255, 156);
            Utilities.DrawLineBetter(spriteBatch, start, end, lineColor * Projectile.scale, Projectile.scale * 3f);
            return false;
        }
    }
}
