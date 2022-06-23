using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class ShadowOrb : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Orb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // Make the nearby light more dim.
            Lighting.AddLight(Projectile.Center, Color.DarkGray.ToVector3() * Projectile.Opacity * 0.5f);

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(120f, 90f, Projectile.timeLeft, true);
        }

        // Summon a random enemy after disappearing.
        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
        }
    }
}
