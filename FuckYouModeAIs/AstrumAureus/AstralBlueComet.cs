using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.AstrumAureus
{
    public class AstralBlueComet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Comet");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 50;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 100;
            projectile.penetrate = -1;
            projectile.timeLeft = 420;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter >= 5)
            {
                projectile.frame++;
                if (projectile.frame >= Main.projFrames[projectile.type])
                    projectile.frame = 0;

                projectile.frameCounter = 0;
            }

            projectile.spriteDirection = (projectile.velocity.X > 0f).ToDirectionInt();
            projectile.rotation = projectile.velocity.ToRotation();
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;

            // Emit light.
            Lighting.AddLight(projectile.Center, 0.3f, 0.5f, 0.1f);

            // Emit astral flame dusts.
            projectile.ai[0]++;
            if (projectile.ai[0] > 15f)
            {
                Dust astralFire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 100, default, 0.8f);
                astralFire.noGravity = true;
                astralFire.velocity *= 0f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, projectile.alpha);

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Zombie, (int)projectile.position.X, (int)projectile.position.Y, 103, 1f, 0f);
            projectile.position = projectile.Center;
            projectile.width = projectile.height = 96;
            projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
            for (int i = 0; i < 2; i++)
                Dust.NewDust(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust astralFire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                astralFire.noGravity = true;
                astralFire.velocity *= 3f;

                astralFire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 173, 0f, 0f, 50, default, 1f);
                astralFire.velocity *= 2f;
                astralFire.noGravity = true;
            }
            projectile.Damage();
        }
    }
}
