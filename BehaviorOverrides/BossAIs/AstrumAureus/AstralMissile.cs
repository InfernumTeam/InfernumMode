using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralMissile : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Missile");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = 1;
            projectile.timeLeft = 360;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(projectile.Center, 0.5f, 0.5f, 0.5f);

            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            // Fly towards the closest player.
            if (Time < 45f && !projectile.WithinRange(closestPlayer.Center, 75f))
                projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(closestPlayer.Center), 0.02f);

            if (projectile.WithinRange(closestPlayer.Center, 30f))
                projectile.Kill();

            if (Time >= 45f && projectile.velocity.Length() < 35f)
                projectile.velocity *= 1.03f;

            Vector2 backOfMissile = projectile.Center - (projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 20f;
            Dust.NewDustDirect(backOfMissile, 5, 5, ModContent.DustType<AstralOrange>());

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/AstrumAureus/AstralMissileGlowmask");
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 1, glowmask);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Zombie, (int)projectile.position.X, (int)projectile.position.Y, 103, 1f, 0f);

            projectile.position = projectile.Center;
            projectile.width = projectile.height = 96;
            projectile.position -= projectile.Size * 0.5f;

            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralBlue>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            projectile.Damage();
        }
    }
}
