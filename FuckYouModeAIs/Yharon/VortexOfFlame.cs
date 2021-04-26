using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Yharon
{
	public class VortexOfFlame : ModProjectile
    {
        public const int Lifetime = 600;
        public const int AuraCount = 4;
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Vortex of Flame");
        }
        public override void SetDefaults()
        {
            projectile.width = 408;
            projectile.height = 408;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.alpha = 255;
            projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            projectile.rotation += MathHelper.ToRadians(12f);
            projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 0.4f, 0.15f);
            if (projectile.owner == Main.myPlayer)
            {
                Player player = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                if (projectile.timeLeft % 120 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * i / 3f;
                        Utilities.NewProjectileBetter(projectile.Center, projectile.DirectionTo(player.Center).RotatedBy(offsetAngle) * 7f, ProjectileID.CultistBossFireBall, 560, 0f, Main.myPlayer);
                    }
                }
            }
        }

        public override void Kill(int timeLeft)
		{
            if (!Main.dedServ)
            {
                for (int i = 0; i < 200; i++)
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Fire);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.fadeIn = 1.4f;
                    dust.scale = 1.6f;
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<LethalLavaBurn>(), 600);
        }

        public override bool CanDamage() => projectile.Opacity >= 0.35f;

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture(Texture);
            for (int j = 0; j < 12f; j++)
            {
                float angle = MathHelper.TwoPi / j * 12f;
                Vector2 offset = angle.ToRotationVector2() * 40f;
                spriteBatch.Draw(texture, projectile.Center + offset - Main.screenPosition, null, Color.White * projectile.Opacity * 0.125f, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }
        }
    }
}