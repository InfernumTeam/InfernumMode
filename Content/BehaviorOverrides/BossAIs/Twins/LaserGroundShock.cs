using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class LaserGroundShock : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ground Shock");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 27;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * 24f;
            Texture2D zap = InfernumTextureRegistry.StreakLightning.Value;
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;

            // Draw an orange backglow.
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.LightGoldenrodYellow * Projectile.Opacity, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.56f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.Orange * Projectile.Opacity * 0.67f, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.72f, 0, 0f);

            // Draw strong red lightning zaps above the ground.
            ulong lightningSeed = (ulong)Projectile.identity * 6342791uL;
            for (int i = 0; i < 5; i++)
            {
                Vector2 lightningScale = new Vector2(1f, Projectile.scale) * Lerp(0.3f, 0.5f, Utils.RandomFloat(ref lightningSeed)) * 1.4f;
                float lightningRotation = Lerp(-0.8f, 0.8f, i / 4f + Utils.RandomFloat(ref lightningSeed) * 0.1f) + PiOver2;
                Color lightningColor = Color.Lerp(Color.Red, Color.Yellow, Utils.RandomFloat(ref lightningSeed) * 0.56f) * Projectile.Opacity;
                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale, 0, 0f);
                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor * 0.5f, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale * new Vector2(1f, 1.3f), 0, 0f);
            }

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;
    }
}
