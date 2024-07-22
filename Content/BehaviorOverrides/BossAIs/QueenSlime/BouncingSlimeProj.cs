using System;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class BouncingSlimeProj : ModProjectile
    {
        public ref float BounceCounter => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.QueenSlimeMinionPink}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bouncy Slime");
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 540;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            
        }

        public override void AI()
        {
            // Slam downward.
            if (Projectile.velocity.Y >= 0.1f && BounceCounter <= 0f)
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y * 1.1f + 0.4f, 0.1f, 18f);

            if (BounceCounter >= 1f)
            {
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.42f, -60f, 16f);
                if (Projectile.velocity.Y > 0f)
                    Projectile.velocity.X *= 0.98f;
            }

            // Decide frames.
            Projectile.frame = 1;

            Time++;

            // Interact with tiles after a short amount of time has passed.
            Projectile.tileCollide = Time >= 36f;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server && Projectile.WithinRange(Main.LocalPlayer.Center, 1500f))
            {
                // Create a slime hit sound.
                if (Main.rand.NextBool(6))
                    SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BounceCounter++;
            Projectile.velocity.Y = -Main.rand.NextFloat(22f, 25f);
            Projectile.velocity.X = 6f;

            if (Main.rand.NextBool(6))
                SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);

            for (int i = 0; i < 8; i++)
            {
                Color gelColor = Color.Lerp(Color.Pink, Color.HotPink, Main.rand.NextFloat());
                Particle gelParticle = new EoCBloodParticle(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), -Vector2.UnitY.RotatedByRandom(0.98f) * Main.rand.NextFloat(4f, 20f), 60, Main.rand.NextFloat(0.8f, 1f), gelColor * 0.9f, 5f);
                GeneralParticleHandler.SpawnParticle(gelParticle);
            }

            return BounceCounter >= 2f;
        }

        public override bool? CanDamage() => Math.Abs(Projectile.velocity.Y) >= 0.1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.DrawProjectileWithBackglowTemp(Color.Pink with { A = 0 } * 0.75f, lightColor, Projectile.Opacity * 5f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.HotPink with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 16f, Time, true)) * Projectile.Opacity;
    }
}
