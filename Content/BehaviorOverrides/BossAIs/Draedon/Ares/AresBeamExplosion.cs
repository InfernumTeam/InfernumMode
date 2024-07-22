using CalamityMod;
using CalamityMod.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresBeamExplosion : ModProjectile
    {
        public ref float Identity => ref Projectile.ai[0];
        public PrimitiveTrailCopy LightningDrawer;
        public PrimitiveTrailCopy LightningBackgroundDrawer;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Exoburst Explosion");
            Main.projFrames[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.3f * Projectile.Opacity, 0.3f * Projectile.Opacity, 0.3f * Projectile.Opacity);

            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4;

            // Die once the final frame is passed.
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.Kill();
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaExplosionSound, Projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into plasma sparks on death.
            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(10f, 10f);
                Utilities.NewProjectileBetter(Projectile.Center, sparkVelocity, ModContent.ProjectileType<ExoburstSpark>(), DraedonBehaviorOverride.StrongerNormalShotDamage, 0f);
            }
        }
    }
}
