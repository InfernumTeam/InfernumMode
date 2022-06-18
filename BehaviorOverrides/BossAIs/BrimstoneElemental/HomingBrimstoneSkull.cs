using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class HomingBrimstoneSkull : ModProjectile
    {
        public Vector2 StartingVelocity;
        public ref float Time => ref projectile.ai[0];
        public static float MaxSpeed
        {
            get
            {
                if ((CalamityWorld.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI)
                    return 17f;
                return 13f;
            }
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Hellblast");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 40;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 420;
            projectile.alpha = 225;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter >= 8)
            {
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
                projectile.frameCounter = 0;
            }

            if (StartingVelocity == Vector2.Zero)
                StartingVelocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * 2f;

            if (Time < 0f)
            {
                float speedInterpolant = (float)Math.Pow(Utils.InverseLerp(-150f, -1f, Time, true), 4D);
                Vector2 endingVelocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * MaxSpeed;
                projectile.velocity = Vector2.Lerp(StartingVelocity, endingVelocity, speedInterpolant);
            }
            else if (Time < 50f)
            {
                float initialSpeed = projectile.velocity.Length();
                Player closestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                projectile.velocity = (projectile.velocity * 34f + projectile.SafeDirectionTo(closestTarget.Center) * initialSpeed) / 35f;
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * initialSpeed;
            }
            else
                projectile.velocity *= 1.022f;

            projectile.spriteDirection = (projectile.velocity.X > 0f).ToDirectionInt();
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.04f, 0f, 1f);
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;

            Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor.R = (byte)(255 * projectile.Opacity);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if ((CalamityWorld.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI)
                target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 180);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            for (int dust = 0; dust < 6; dust++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
