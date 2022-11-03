using CalamityMod.Dusts;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneRose : ModProjectile
    {
        public Vector2 StartingVelocity;
        public ref float Time => ref Projectile.ai[0];
        public bool SpawnedWhileAngry => Projectile.ai[1] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Rose");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, 25f, Time, true);
            Projectile.Opacity = (float)Math.Sqrt(Projectile.scale) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            // Initialize rotation.
            if (Projectile.rotation == 0f)
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.9f, 0f, 0f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 5; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int petalCount = 2;
                int petalDamage = 135;
                float petalShootSpeed = 10f;
                if (BossRushEvent.BossRushActive)
                {
                    petalCount = 3;
                    petalShootSpeed = 14f;
                }

                if (SpawnedWhileAngry)
                {
                    petalShootSpeed *= 1.6f;
                    petalCount = 5;
                }
                for (int i = 0; i < petalCount; i++)
                {
                    Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.68f, 0.68f, i / (float)petalCount)) * petalShootSpeed;
                    Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<BrimstonePetal>(), petalDamage, 0f);
                }
            }
        }
    }
}
