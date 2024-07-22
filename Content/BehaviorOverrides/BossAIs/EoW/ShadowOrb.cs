using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EoW
{
    // This is completely useless as it no longer spawns an enemy. However, it remains as a testament to how difficult EoW
    // is to try to make any sort of decent fight from.
    public class ShadowOrb : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.ShadowOrb}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Orb");

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

        // Summon a random enemy after disappearing. Nah, get trolled play a sound instead lol
        public override void OnKill(int timeLeft) => SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
    }
}
