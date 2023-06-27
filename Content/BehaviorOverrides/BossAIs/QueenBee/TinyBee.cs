using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenBee
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
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.03f, 0f, 1f);
            Projectile.rotation = Clamp(Projectile.velocity.X * 0.15f, -0.7f, 0.7f);
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
                Dust honey = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.t_Honey, 0f, 0f, 0, default, 0.8f);
                if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 } * Pow(Projectile.Opacity, 2f), lightColor, Projectile.Opacity * 6f);
            return false;
        }
    }
}
