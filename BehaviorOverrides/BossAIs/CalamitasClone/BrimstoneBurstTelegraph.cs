using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class BrimstoneBurstTelegraph : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 42;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 30;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 30f * MathHelper.Pi) * 1.6f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center - projectile.velocity.SafeNormalize(Vector2.Zero) * 1600f;
            Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(Vector2.Zero) * 1600f;
            spriteBatch.DrawLineBetter(start, end, Color.DarkRed, projectile.Opacity * 6f + 0.5f);
            spriteBatch.DrawLineBetter(start, end, Color.Red, (projectile.Opacity * 6f + 0.5f) * 0.5f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            int fireDamage = shouldBeBuffed ? 380 : 160;
            Utilities.NewProjectileBetter(projectile.Center, projectile.velocity, ModContent.ProjectileType<BrimstoneBurst>(), fireDamage, 0f);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool CanDamage() => false;
    }
}
