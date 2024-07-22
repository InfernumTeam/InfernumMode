using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class AquaticAberrationProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Aquatic Aberration");
            Main.projFrames[Type] = 7;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.Opacity = 0f;
            
        }

        public override void AI()
        {
            // Determine opacity.
            Projectile.Opacity = Utils.GetLerpValue(0f, 36f, Time, true);
            Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();

            // Determine frames and rotation.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == 1)
                Projectile.rotation += Pi;

            // Try to hover towards the target at first.
            if (Time < 54f)
            {
                float inertia = 16f;
                float oldSpeed = Projectile.velocity.Length();
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 homingVelocity = Projectile.SafeDirectionTo(target.Center) * oldSpeed;

                Projectile.velocity = (Projectile.velocity * (inertia - 1f) + homingVelocity) / inertia;
                Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * oldSpeed;
            }
            Time++;

            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.5f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 20; k++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, Math.Sign(Projectile.velocity.X), -1f, 0, default, 1f);

            SoundEngine.PlaySound(SoundID.NPCDeath12, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 shootVelocity = (TwoPi * i / 4f).ToRotationVector2() * 8.5f;
                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<LeviathanVomit>(), LeviathanComboAttackManager.LeviathanVomitDamage, 0f);
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            LumUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
