using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class CosmicFlame : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Flame");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 36;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 210;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 10f, Time, true) * Utils.InverseLerp(0f, 15f, projectile.timeLeft, true);
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time > 35f)
            {
                Vector2 idealVelocity = projectile.SafeDirectionTo(target.Center) * 13f;
                projectile.velocity = (projectile.velocity * 39f + idealVelocity) / 40f;
            }

            Lighting.AddLight(projectile.Center, Vector3.One * projectile.Opacity * 0.4f);
            Time++;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(new Color(198, 118, 204, 0), lightColor, Utils.InverseLerp(0f, 12f, Time, true)) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            spriteBatch.Draw(texture, drawPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation, frame.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => projectile.alpha < 20;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
