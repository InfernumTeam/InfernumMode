using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
{
    public class HomingAcid : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public Player ClosestPlayer => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Acid");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 135;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 35f, Time, true) * Utils.GetLerpValue(0f, 56f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Time++;

            if (Time < 80f)
                return;

            float idealFlySpeed = BossRushEvent.BossRushActive ? 32f : 24f;
            if (!Projectile.WithinRange(ClosestPlayer.Center, 1200f))
                idealFlySpeed *= 0.5f;

            if (!Projectile.WithinRange(ClosestPlayer.Center, 150f))
                Projectile.velocity = (Projectile.velocity * 69f + Projectile.SafeDirectionTo(ClosestPlayer.Center) * idealFlySpeed) / 70f;

            if (Projectile.WithinRange(ClosestPlayer.Center, 20f))
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.position + Projectile.Size * 0.5f - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = Projectile.GetAlpha(new Color(85, 224, 60, 0) * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, new Color(117, 95, 133, 184) * Projectile.Opacity, ProjectileID.Sets.TrailingMode[Projectile.type], 2);

            return false;
        }
    }
}
