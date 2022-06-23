using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarFlare : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];
        public Player Target => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Flare");
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 5;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Projectile.ai[1] != -1f && Projectile.position.Y > Projectile.ai[1])
                Projectile.tileCollide = true;

            if (Projectile.position.HasNaNs())
            {
                Projectile.Kill();
                return;
            }
            Dust electrivity = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 229, 0f, 0f, 0, default, 1f);
            electrivity.position = Projectile.Center;
            electrivity.velocity = Vector2.Zero;
            electrivity.noGravity = true;
            if (WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16)))
                electrivity.noLight = true;

            if (Projectile.ai[1] == -1f)
            {
                Projectile.ai[0]++;

                if (Projectile.ai[0] == 2f)
                    SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);

                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.penetrate = -1;
                Projectile.alpha = Utils.Clamp(Projectile.alpha - 10, 0, 255);

                Projectile.frameCounter++;
                if (Projectile.frameCounter >= Projectile.MaxUpdates * 3)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.ai[0] >= Main.projFrames[Projectile.type] * Projectile.MaxUpdates * 3)
                {
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
