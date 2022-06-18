using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class TinyBee : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bee");
            Main.projFrames[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 40;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
            projectile.scale = 1f;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 50, 0, 255);
            projectile.rotation = MathHelper.Clamp(projectile.velocity.X * 0.15f, -0.7f, 0.7f);
            projectile.spriteDirection = (projectile.velocity.X < 0f).ToDirectionInt();

            if (Time < 80f)
                projectile.velocity = Vector2.Lerp(projectile.velocity, Vector2.UnitX * (projectile.velocity.X > 0f).ToDirectionInt() * 10f, 0.02f);

            projectile.frame = projectile.timeLeft / 4 % Main.projFrames[projectile.type];

            Time++;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Poisoned, 90);

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.NPCDeath1, projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust honey = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 147, 0f, 0f, 0, default, 0.8f);
                if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;
            }
        }
    }
}
