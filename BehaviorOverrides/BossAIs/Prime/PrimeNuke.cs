using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeNuke : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Nuke");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 52;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(240f, 210f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            float movementSpeed = BossRushEvent.BossRushActive ? 18f : 6.5f;
            if (!projectile.WithinRange(closestPlayer.Center, 180) && projectile.timeLeft > 70)
                projectile.velocity = (projectile.velocity * 19f + projectile.SafeDirectionTo(closestPlayer.Center) * movementSpeed) / 20f;

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            float fadeToRed = Utils.InverseLerp(65f, 10f, projectile.timeLeft, true) * 0.8f;
            Color redFade = Color.Red * 0.67f;
            redFade.A = 0;

            Color drawColor = projectile.GetAlpha(Color.Lerp(lightColor, redFade, fadeToRed));
            float outwardness = fadeToRed * 3f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * outwardness;
                spriteBatch.Draw(texture, projectile.Center - Main.screenPosition + drawOffset, null, drawColor, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeMechGaussRifle"), projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<NuclearExplosion>(), 220, 0f);
        }

        public override bool CanDamage() => false;
    }
}
