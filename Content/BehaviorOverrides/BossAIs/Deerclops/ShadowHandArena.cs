using System;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class ShadowHandArena : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int HandCount = 24;

        public const float RingRadius = 600f;

        public float Radius
        {
            get
            {
                int deerclopsIndex = NPC.FindFirstNPC(NPCID.Deerclops);
                float radius = RingRadius * Lerp(2.5f, 1f, Projectile.Opacity);
                if (deerclopsIndex >= 0)
                    radius *= 1f - Main.npc[deerclopsIndex].Infernum().ExtraAI[DeerclopsBehaviorOverride.ShadowRadiusDecreaseInterpolantIndex];
                return radius;
            }
        }

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.InsanityShadowHostile}";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Hand");

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade out if Deerclops is gone.
            int deerclopsIndex = NPC.FindFirstNPC(NPCID.Deerclops);
            if (deerclopsIndex < 0)
            {
                Projectile.Opacity = Projectile.timeLeft / 30f;
                return;
            }

            // Fade in.
            Projectile.timeLeft = 30;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.017f, 0f, 1f);

            // Move towards the target.
            Projectile.Center = Projectile.Center.MoveTowards(Main.npc[deerclopsIndex].Center, 1.1f);

            // Give the player the madness effect if they leave the circle.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (!target.WithinRange(Projectile.Center, Radius + 12f))
                target.AddBuff(ModContent.BuffType<Madness>(), 8);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Black * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D blackCircle = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;

            // Draw the circle.
            Vector2 circleScale = new Vector2(MathF.Max(Main.screenWidth, Main.screenHeight)) * 5f;
            Main.spriteBatch.EnterShaderRegion();

            var circleCutoutShader = InfernumEffectsRegistry.CircleCutoutShader;
            circleCutoutShader.Shader.Parameters["uImageSize0"].SetValue(circleScale);
            circleCutoutShader.Shader.Parameters["uCircleRadius"].SetValue(Radius * 1.414f);
            circleCutoutShader.Apply();
            Main.spriteBatch.Draw(blackCircle, drawPosition, null, Color.Black, 0f, blackCircle.Size() * 0.5f, circleScale / blackCircle.Size(), 0, 0f);
            Main.spriteBatch.ExitShaderRegion();

            for (int i = 0; i < HandCount; i++)
            {
                float rotation = TwoPi * i / HandCount + Time / 31f + Pi;
                Vector2 ringOffset = (TwoPi * i / HandCount + Time / 31f).ToRotationVector2() * RingRadius;
                Color backglowColor = Color.Violet * Projectile.Opacity * 0.5f;
                for (int j = 0; j < 4; j++)
                {
                    Vector2 offsetDirection = rotation.ToRotationVector2();
                    double spin = Main.GlobalTimeWrappedHourly * TwoPi / 24f + TwoPi * j / 4f;
                    Main.EntitySpriteDraw(tex, drawPosition + ringOffset + offsetDirection.RotatedBy(spin) * 6f, null, backglowColor, rotation, origin, Projectile.scale, 0, 0);
                }
                Main.spriteBatch.Draw(tex, drawPosition + ringOffset, null, Projectile.GetAlpha(Color.White), rotation, origin, Projectile.scale, 0, 0f);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < HandCount; i++)
            {
                Vector2 ringOffset = (TwoPi * i / HandCount + Time / 31f).ToRotationVector2() * RingRadius;
                if (Utils.CenteredRectangle(Projectile.Center + ringOffset, projHitbox.Size()).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }
    }
}
