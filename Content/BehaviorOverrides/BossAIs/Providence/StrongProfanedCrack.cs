using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class StrongProfanedCrack : ModProjectile
    {
        public override string Texture => InfernumTextureRegistry.InvisPath;

        public bool Pink => Projectile.ai[1] == 1f;

        public ref float Timer => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Profaned Cracks");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity * 3f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * 24f;
            Texture2D zap = InfernumTextureRegistry.StreakLightning.Value;
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;

            // Draw an orange backglow.
            Color backglow1 = WayfinderSymbol.Colors[1];
            Color backglow2 = WayfinderSymbol.Colors[2];
            if (Pink)
                backglow2 = Color.Lerp(backglow2, Color.HotPink, 0.6f);

            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, backglow1 * Projectile.Opacity, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.26f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, backglow2 * Projectile.Opacity * 0.67f, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.52f, 0, 0f);

            // Draw strong yellow lightning zaps above the ground.
            ulong lightningSeed = (ulong)Projectile.identity * 7218432uL;
            for (int i = 0; i < 8; i++)
            {
                Vector2 lightningScale = new Vector2(1.8f, Projectile.scale) * Lerp(0.3f, 0.5f, Utils.RandomFloat(ref lightningSeed)) * 1;
                float lightningRotation = Projectile.rotation + Lerp(-1.6f, 1.6f, i / 8f + Utils.RandomFloat(ref lightningSeed) * 0.1f);
                Color lightningColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Utils.RandomFloat(ref lightningSeed) * 0.56f);
                if (ProvidenceBehaviorOverride.IsEnraged)
                    lightningColor = Color.Lerp(lightningColor, Color.LightSkyBlue, 0.6f);
                if (Pink)
                    lightningColor = Color.Lerp(lightningColor, Color.HotPink, 0.85f);

                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor * Projectile.Opacity, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale, 0, 0f);
                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor * Projectile.Opacity * 0.5f, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale * new Vector2(1f, 1.3f), 0, 0f);
                lightningSeed += 854175uL;
            }

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;
    }
}