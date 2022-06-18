using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class ShadowOrb : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Orb");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // Make the nearby light more dim.
            Lighting.AddLight(projectile.Center, Color.DarkGray.ToVector3() * projectile.Opacity * 0.5f);

            // Fade in and out.
            projectile.Opacity = Utils.InverseLerp(0f, 30f, projectile.timeLeft, true) * Utils.InverseLerp(120f, 90f, projectile.timeLeft, true);
        }

        // Summon a random enemy after disappearing.
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item8, projectile.Center);
        }
    }
}
