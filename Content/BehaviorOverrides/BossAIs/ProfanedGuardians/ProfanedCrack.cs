using CalamityMod.Particles.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Metaballs.CalMetaballs;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedCrack : ModProjectile
    {
        public override string Texture => InfernumTextureRegistry.InvisPath;

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
            Projectile.timeLeft = 27;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Timer == 0)
            {
                ulong lightningSeed = (ulong)Projectile.identity * 6342791uL;
                for (int i = 0; i < 8; i++)
                {
                    float lightningRotation = Lerp(-1.6f, 1.6f, i / 8f + Utils.RandomFloat(ref lightningSeed) * 0.1f) + PiOver2;

                    InfernumTextureRegistry.StreakLightning.Value.CreateMetaballsFromTexture(ref FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>().Particles,
                        Projectile.Left, lightningRotation, Projectile.scale, 50f, 100);
                }
            }
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity;
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * 24f;
            Texture2D zap = InfernumTextureRegistry.StreakLightning.Value;
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;

            // Draw an orange backglow.
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, WayfinderSymbol.Colors[2] * Projectile.Opacity, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.26f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, WayfinderSymbol.Colors[1] * Projectile.Opacity * 0.67f, 0f, backglowTexture.Size() * 0.5f, Projectile.scale * 0.52f, 0, 0f);

            // Draw strong red lightning zaps above the ground.
            ulong lightningSeed = (ulong)Projectile.identity * 6342791uL;
            for (int i = 0; i < 8; i++)
            {
                Vector2 lightningScale = new Vector2(1f, Projectile.scale) * Lerp(0.3f, 0.5f, Utils.RandomFloat(ref lightningSeed)) * 1.4f;
                float lightningRotation = Lerp(-1.6f, 1.6f, i / 8f + Utils.RandomFloat(ref lightningSeed) * 0.1f) + PiOver2;
                Color lightningColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Utils.RandomFloat(ref lightningSeed) * 0.56f) * Projectile.Opacity;
                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale, 0, 0f);
                Main.spriteBatch.Draw(zap, drawPosition, null, lightningColor * 0.5f, lightningRotation, zap.Size() * Vector2.UnitY * 0.5f, lightningScale * new Vector2(1f, 1.3f), 0, 0f);
                lightningSeed += (ulong)12346f;
            }

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;
    }
}
