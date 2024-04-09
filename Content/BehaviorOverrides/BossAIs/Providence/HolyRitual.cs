using System.Collections.Generic;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyRitual : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static int Lifetime => 180;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = false;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, 30f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.scale * 0.05f;

            // Emit fire particles.
            Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(225f, 225f) * Projectile.scale, 6);
            fire.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 5f);
            fire.scale *= 2.7f;
            fire.noGravity = true;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ritual1 = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Ritual").Value;
            Texture2D ritual2 = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Ritual2").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale1 = Vector2.One * Projectile.scale * 240f / ritual2.Size() * 2f;

            float colorInterpolant = Cos(TwoPi * Time / 60f) * 0.5f + 0.5f;
            Color color1 = Color.Lerp(Color.Wheat, ProvidenceBehaviorOverride.IsEnraged ? Color.Cyan : Color.Yellow, colorInterpolant * 0.6f) * Projectile.scale;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(ritual1, drawPosition, null, color1 * 1.35f, 0f, ritual1.Size() * 0.5f, scale1 * 2.22f, 0, 0);
            Main.EntitySpriteDraw(ritual2, drawPosition, null, color1, Projectile.rotation, ritual2.Size() * 0.5f, scale1, 0, 0);
            Main.spriteBatch.ResetBlendState();

            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
    }
}
