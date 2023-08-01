using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class Brimrose : ModProjectile
    {
        public const int Lifetime = 420;

        public ref float Time => ref Projectile.ai[0];

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Brimrose");

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 126;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // Choose a direction on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.spriteDirection = Main.rand.NextBool().ToDirectionInt();
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            // Fall to the side if on ground.
            if (Math.Abs(Projectile.velocity.Y) <= 0.41f)
            {
                Projectile.rotation = Lerp(Projectile.rotation + Projectile.spriteDirection * 0.08f, Projectile.spriteDirection * PiOver2, 0.1f);
                Projectile.rotation = Clamp(Projectile.rotation, -PiOver2, PiOver2);
            }
            else
                Projectile.rotation = 0f;

            // Fade away over time.
            Projectile.Opacity = Utils.GetLerpValue(0f, 120f, Projectile.timeLeft, true);

            Projectile.gfxOffY = Math.Abs(Projectile.rotation / PiOver2) * 24f;

            // Fall down.
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.4f, -8f, 20f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.localAI[1] == 0f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.BrimstoneElementalShellGroundHit, Projectile.Center);
                Projectile.localAI[1] = 1f;
            }
            return false;
        }
    }
}
