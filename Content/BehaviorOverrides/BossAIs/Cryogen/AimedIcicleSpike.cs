using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen
{
    public class AimedIcicleSpike : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float AimAheadFactor => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Icicle Spike");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 12;

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Time < 60f)
            {
                float spinSlowdown = Utils.GetLerpValue(56f, 40f, Time, true);
                Projectile.velocity *= 0.93f;
                Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * spinSlowdown * 0.3f;
                if (spinSlowdown < 1f)
                {
                    Vector2 aimAhead = closestPlayer.velocity * AimAheadFactor;
                    Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(closestPlayer.Center + aimAhead) - PiOver2, (1f - spinSlowdown) * 0.6f);
                }
            }

            if (Time == 60f)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(closestPlayer.Center + closestPlayer.velocity * AimAheadFactor) * 9f;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
            }
            if (Time > 60f && Projectile.velocity.Length() < 18f)
                Projectile.velocity *= BossRushEvent.BossRushActive ? 1.02f : 1.01f;

            if (Time % 10 == 0)
            {
                // Leave a trail of particles.
                Particle iceParticle = new SnowyIceParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }

            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.4f);
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Draw backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * 4f;
                Color afterimageColor = new Color(46, 188, 234, 0f) * 0.4f * Projectile.Opacity;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + afterimageOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, 1, 0, 0);
            return false;
        }
        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 60f;
    }
}
