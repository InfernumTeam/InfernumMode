using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class PlasmaDrop : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Plasma Droplet");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.27f, -20f, 7f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.scale = Utils.GetLerpValue(-5f, 45f, Projectile.timeLeft, true);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity > 0.7f;

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
            color = Color.Lerp(color, Color.White, 0.55f);
            color.A = 0;
            return color * Projectile.Opacity * 0.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D plasmaTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2.5f;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;

                Main.spriteBatch.Draw(plasmaTexture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, plasmaTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
    }
}
