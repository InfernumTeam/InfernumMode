using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class IceMass : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int ShardBurstCount = 9;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Mass");

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 102;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 180;
            Projectile.Opacity = 0f;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 40f, Time, true);
            Projectile.rotation += MathHelper.Pi / 30f;

            if (Time >= 110)
                Projectile.velocity *= 0.975f;
            Time++;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw telegraph lines.
            // The amount of these will create a somewhat geometric pattern.
            if (Time is > 60f and < 170f)
            {
                float lineWidth = Utils.GetLerpValue(60f, 90f, Time, true) * Utils.GetLerpValue(170f, 140f, Time, true) * 2.5f + 0.2f;

                if (lineWidth > 1f)
                    lineWidth += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.15f;

                for (int i = 0; i < ShardBurstCount; i++)
                {
                    Vector2 lineDirection = (MathHelper.TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2();
                    Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + lineDirection * 5980f, Color.SkyBlue, lineWidth);
                }
            }
            return true;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Release ice shards based on the telegraphs.
            for (int i = 0; i < ShardBurstCount; i++)
            {
                for (float speed = 6f; speed <= 21f; speed += 3.3f)
                {
                    Vector2 iceVelocity = (MathHelper.TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2() * speed * (BossRushEvent.BossRushActive ? 1.6f : 1f);
                    Utilities.NewProjectileBetter(Projectile.Center, iceVelocity, ModContent.ProjectileType<IceShard>(), 185, 0f);
                }
            }
        }
    }
}
