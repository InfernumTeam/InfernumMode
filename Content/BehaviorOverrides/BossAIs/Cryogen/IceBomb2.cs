using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen
{
    public class IceBomb2 : ModProjectile
    {
        public ref float Time => ref Projectile.localAI[0];
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Bomb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.scale = Lerp(1f, 1.7f, 1f - Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true));
            Projectile.velocity *= 0.98f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
            {
                Vector2 spikeVelocity = -Vector2.UnitY.RotatedBy(Lerp(-0.43f, 0.43f, i / 4f)) * 12f;
                Utilities.NewProjectileBetter(Projectile.Center, spikeVelocity, ModContent.ProjectileType<IceRain2>(), CryogenBehaviorOverride.IceRainDamage, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Draw backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * 4f;
                Color afterimageColor = new Color(46, 188, 234, 0f) * 0.3f;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + afterimageOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, 1, 0, 0);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;
    }
}
