using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class BombingTelegraph : ModProjectile
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
            projectile.timeLeft = 180;
            projectile.scale = 0.01f;
        }

        public override void AI()
        {
            if (Countdown > 0f)
                Countdown--;
            else
            {
                if (projectile.ai[1] == 1f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TankCannon"), Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 missileSpawnPosition = new Vector2(projectile.Center.X, Target.Center.Y) - Vector2.UnitY.RotatedBy(projectile.rotation) * 1000f;
                    Vector2 missileVelocity = Vector2.UnitY.RotatedBy(projectile.rotation) * 29f;
                    int missile = Utilities.NewProjectileBetter(missileSpawnPosition, missileVelocity, ModContent.ProjectileType<PlagueMissile2>(), 170, 0f);
                    if (Main.projectile.IndexInRange(missile))
                        Main.projectile[missile].ai[0] = Target.whoAmI;
                }

                projectile.Kill();
            }

            projectile.scale = MathHelper.Clamp(projectile.scale + 0.05f, 0f, 1f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation) * 4350f;
            Vector2 end = projectile.Center + Vector2.UnitY.RotatedBy(projectile.rotation) * 4350f;
            Utilities.DrawLineBetter(spriteBatch, start, end, Color.Lime * projectile.scale, projectile.scale * 3f);
            return false;
        }
    }
}
