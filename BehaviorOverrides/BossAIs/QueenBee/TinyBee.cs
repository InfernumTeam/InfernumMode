using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class TinyBee : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bee");
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.scale = 1f;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 50, 0, 255);
            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.15f, -0.7f, 0.7f);
            Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();

            if (Time < 80f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.UnitX * (Projectile.velocity.X > 0f).ToDirectionInt() * 10f, 0.02f);

            Projectile.frame = Projectile.timeLeft / 4 % Main.projFrames[Projectile.type];

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust honey = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 147, 0f, 0f, 0, default, 0.8f);
                if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;
            }
        }
    }
}
