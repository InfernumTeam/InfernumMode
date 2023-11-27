using CalamityMod.DataStructures;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class LingeringHolyFire : ModProjectile, IAdditiveDrawer
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => InfernumTextureRegistry.InvisPath;


        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 70;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.alpha = 255;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.scale = Sin(Time / 150f * Pi) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            Projectile.Opacity = Projectile.scale;
            Projectile.scale *= Lerp(0.8f, 1.1f, Projectile.identity % 9f / 9f);
            Projectile.velocity *= 0.98f;
            Projectile.rotation += Clamp(Projectile.velocity.X * 0.04f, -0.06f, 0.06f);

            Time++;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = InfernumTextureRegistry.Cloud2.Value;
            Color color = Projectile.GetAlpha(Color.White);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.6f, 0, 0f);
            spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity * 0.7f, Projectile.rotation, texture.Size() * 0.6f, Projectile.scale * 0.5f, 0, 0f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color c = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[0], Projectile.identity % 10f / 16f);
            return c * 1.15f;
        }
    }
}
