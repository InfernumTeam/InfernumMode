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
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 30f * MathHelper.Pi) * 1.6f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 1600f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 1600f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.DarkRed, Projectile.Opacity * 6f + 0.5f);
            Main.spriteBatch.DrawLineBetter(start, end, Color.Red, (Projectile.Opacity * 6f + 0.5f) * 0.5f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool shouldBeBuffed = DownedBossSystem.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
            int fireDamage = shouldBeBuffed ? 380 : 160;
            Utilities.NewProjectileBetter(Projectile.Center, Projectile.velocity, ModContent.ProjectileType<BrimstoneBurst>(), fireDamage, 0f);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false ? null : false;
    }
}
