using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SlowerSandTooth : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Sand Tooth");

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 20;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 450;
        }

        public override void AI()
        {
            if (Time < 250f && Time > 60f)
            {
                float oldSpeed = projectile.velocity.Length();
                projectile.velocity = (projectile.velocity * 24f + projectile.SafeDirectionTo(Target.Center) * oldSpeed) / 25f;
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = Main.projectileTexture[projectile.type];
            spriteBatch.Draw(tex, projectile.Center - Main.screenPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, tex.Size() / 2f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
