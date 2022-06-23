using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeNuke : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Nuke");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(240f, 210f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            float movementSpeed = BossRushEvent.BossRushActive ? 18f : 6.5f;
            if (!Projectile.WithinRange(closestPlayer.Center, 180) && Projectile.timeLeft > 70)
                Projectile.velocity = (Projectile.velocity * 19f + Projectile.SafeDirectionTo(closestPlayer.Center) * movementSpeed) / 20f;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float fadeToRed = Utils.GetLerpValue(65f, 10f, Projectile.timeLeft, true) * 0.8f;
            Color redFade = Color.Red * 0.67f;
            redFade.A = 0;

            Color drawColor = Projectile.GetAlpha(Color.Lerp(lightColor, redFade, fadeToRed));
            float outwardness = fadeToRed * 3f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * outwardness;
                spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + drawOffset, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeMechGaussRifle"), Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NuclearExplosion>(), 220, 0f);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => false;
    }
}
