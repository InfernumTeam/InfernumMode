using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class CosmicKunai : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Kunai");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 6f, Time, true) * Utils.InverseLerp(0f, 6f, projectile.timeLeft, true);

            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time < 30f)
            {
                float spinSlowdown = Utils.InverseLerp(28f, 15f, Time, true);
                projectile.velocity *= 0.85f;
                projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * spinSlowdown * 0.3f;
                if (spinSlowdown < 1f)
                    projectile.rotation = projectile.rotation.AngleLerp(projectile.AngleTo(closestPlayer.Center) + MathHelper.PiOver2, (1f - spinSlowdown) * 0.6f);
            }

            if (Time == 30f)
            {
                projectile.velocity = projectile.SafeDirectionTo(closestPlayer.Center) * 18f;
                Main.PlaySound(SoundID.Item73, projectile.Center);
            }
            if (Time > 30f && projectile.velocity.Length() < 30f)
                projectile.velocity *= 1.0185f;

            Lighting.AddLight(projectile.Center, Vector3.One * projectile.Opacity * 0.4f);
            Time++;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(new Color(198, 118, 204, 0), lightColor, Utils.InverseLerp(8f, 24f, Time, true)) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            // Draw afterimages.
            for (int i = 0; i < 5; i++)
            {
                Vector2 afterimageOffset = projectile.velocity.SafeNormalize(Vector2.Zero) * i * -20f;
                Color afterimageColor = new Color(198, 118, 204, 0) * (1f - i / 5f) * 0.7f;
                spriteBatch.Draw(texture, drawPosition + afterimageOffset, null, projectile.GetAlpha(afterimageColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale * 0.7f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => projectile.alpha < 20;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
