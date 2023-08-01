using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class ExplodingFlower : ModProjectile
    {
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Flower");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(150f, 130f, Projectile.timeLeft, true);
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                CloudParticle sporeGas = new(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Color.Pink, Color.Lime, 36, Main.rand.NextFloat(0.6f, 0.85f));
                GeneralParticleHandler.SpawnParticle(sporeGas);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                float offsetAngle = Lerp(-0.38f, 0.38f, i / 2f);
                Vector2 petalShootVelocity = Projectile.SafeDirectionTo(closestPlayer.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 7.5f;
                Utilities.NewProjectileBetter(Projectile.Center, petalShootVelocity, ModContent.ProjectileType<Petal>(), PlanteraBehaviorOverride.PetalDamage, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
