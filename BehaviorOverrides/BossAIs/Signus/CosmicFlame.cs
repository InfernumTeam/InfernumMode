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
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Flame");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 210;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Time, true) * Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Time > 35f)
            {
                Vector2 idealVelocity = Projectile.SafeDirectionTo(target.Center) * 13f;
                Projectile.velocity = (Projectile.velocity * 39f + idealVelocity) / 40f;
            }

            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.4f);
            Time++;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(new Color(198, 118, 204, 0), lightColor, Utils.GetLerpValue(0f, 12f, Time, true)) * Projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => Projectile.alpha < 20;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
