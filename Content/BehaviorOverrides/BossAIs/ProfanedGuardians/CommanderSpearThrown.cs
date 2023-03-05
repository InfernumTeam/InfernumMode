using CalamityMod;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class CommanderSpearThrown : ModProjectile
    {
        public bool ExplodeOnImpact => Projectile.ai[0] == 1;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/CommanderSpear";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (ExplodeOnImpact)
                Projectile.tileCollide = Projectile.Center.Y > Main.player[HolySineSpear.Commander.target].Center.Y;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);

            // Accelerate.
            if (Projectile.velocity.Length() < 36f)
                Projectile.velocity *= 1.028f;

            for (int i = 0; i < 40; i++)
            {
                // Bias towards lower values. 
                float size = MathF.Pow(Main.rand.NextFloat(), 2f);
                FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(Projectile.Center - (Projectile.velocity * 0.5f) + (Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f) * size),
                    Main.rand.NextFloat(10f, 15f));
            }

            Lighting.AddLight(Projectile.Center, Vector3.One);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (!ExplodeOnImpact)
                return;
            ScreenEffectSystem.SetBlurEffect(Projectile.position, 1f, 45);   
            GuardianComboAttackManager.CreateFireExplosion(Projectile.Center, true);
            for (int i = 0; i < 100; i++)
                FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(Projectile.Center + Main.rand.NextVector2Circular(100f, 100f), Main.rand.NextFloat(52f, 85f));

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int crossWaves = 3;
            int crossCount = 20;
            for (int i = 0; i < crossWaves; i++)
            {
                float speed = i switch
                { 
                    0 => 9.5f,
                    1 => 8f,
                    _ => 6.5f
                };
                
                for (int j = 0; j < crossCount; j++)
                {
                    Vector2 crossVelocity = (MathHelper.TwoPi * j / crossCount + MathHelper.PiOver4 * i).ToRotationVector2() * speed;
                    Utilities.NewProjectileBetter(Projectile.Center + crossVelocity, crossVelocity, ModContent.ProjectileType<HolyCross>(), 250, 0f);
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            if (ExplodeOnImpact)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
                // Draw the spear as a white hot flame with additive blending before it converge inward to create the actual spear.
                for (int i = 0; i < 10; i++)
                {
                    float rotation = Projectile.rotation + MathHelper.Lerp(-0.16f, 0.16f, i / 9f);
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 2f;
                    Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                    Main.EntitySpriteDraw(texture, drawPosition, null, WayfinderSymbol.Colors[0], rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
                }
                return false;
            }
            float alpha = 1f - (float)Projectile.alpha / 255;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor * alpha, 1);
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 2f);
            return false;
        }
    }
}
