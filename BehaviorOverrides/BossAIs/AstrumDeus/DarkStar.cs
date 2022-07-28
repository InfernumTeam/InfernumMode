using CalamityMod;
using CalamityMod.Projectiles;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DarkStar : ModProjectile
    {
        public Vector2 AnchorPoint;

        public float InitialOffsetAngle;

        public float FadeToDarkGodColors => Utils.GetLerpValue(90f, 150f, Time, true);

        public ref float ConstellationIndex => ref Projectile.ai[0];

        public ref float ConstellationIndexToAttachTo => ref Projectile.ai[1];

        public ref float ColorSpectrumHue => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public const int PointsInStar = 6;
        public const float RadiusOfConstellation = 575f;

        public const int Lifetime = 960;
        public const int FadeinTime = 18;
        public const int FadeoutTime = 18;

        public static Vector2 CalculateStarPosition(Vector2 origin, float offsetAngle, float spinAngle)
        {
            int n = PointsInStar - 1;
            Vector2 starOffset = new Vector2((float)Math.Sin(offsetAngle) * n - (float)Math.Sin(offsetAngle * n), (float)Math.Cos(offsetAngle) * n + (float)Math.Cos(offsetAngle * n)) * RadiusOfConstellation;
            starOffset /= PointsInStar;
            starOffset.Y *= -1f;
            return origin + starOffset.RotatedBy(spinAngle);
        }
        
        public override void SetStaticDefaults() => DisplayName.SetDefault("Dark Star");

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
            Projectile.hide = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(InitialOffsetAngle);
            writer.Write(Time);
            writer.WriteVector2(AnchorPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            InitialOffsetAngle = reader.ReadSingle();
            Time = reader.ReadSingle();
            AnchorPoint = reader.ReadVector2();
        }

        public override void AI()
        {
            if (Time == 1f)
            {
                Projectile.scale = 1f;
                CalamityGlobalProjectile.ExpandHitboxBy(Projectile, (int)(72 * Projectile.scale));
                ColorSpectrumHue = Main.rand.NextFloat(0f, 0.9999f);
                Projectile.netUpdate = true;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            Time++;

            Projectile.Center = CalculateStarPosition(AnchorPoint, InitialOffsetAngle, Time / 72f);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += (Projectile.identity % 2 == 0).ToDirectionInt() * 0.024f;
            
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeinTime, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - FadeoutTime, Time, true);
            Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(Time / 20f) * 0.02f);
            Projectile.scale = MathHelper.Lerp(0.135f, 0.175f, FadeToDarkGodColors) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sparkleTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/LargeStar").Value;

            // Orange and cyan.
            Color c1 = new(255, 63, 39);
            Color c2 = new(40, 255, 187);

            // Moon lord cyan and violet.
            c1 = Color.Lerp(c1, new(117, 255, 160), FadeToDarkGodColors);
            c2 = Color.Lerp(c2, new(88, 55, 172), FadeToDarkGodColors);

            float hue = (float)Math.Sin(MathHelper.Pi * ColorSpectrumHue + Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f;
            Color sparkleColor = CalamityUtils.MulticolorLerp(hue, c1, c2) * Projectile.Opacity * 0.84f;
            sparkleColor *= MathHelper.Lerp(1f, 1.5f, Utils.GetLerpValue(Lifetime * 0.5f - 15f, Lifetime * 0.5f + 15f, Time, true));
            Vector2 origin = sparkleTexture.Size() / 2f;
            
            Vector2 sparkleScale = Vector2.One * Projectile.Opacity * Projectile.scale;
            Vector2 orthogonalsparkleScale = Vector2.One * Projectile.Opacity * Projectile.scale * 1.4f;

            Projectile projectileToConnectTo = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != Projectile.type || !Main.projectile[i].active || Main.projectile[i].ai[0] != ConstellationIndexToAttachTo)
                {
                    continue;
                }

                projectileToConnectTo = Main.projectile[i];
                break;
            }

            float scaleFactor = Utils.GetLerpValue(0f, 15f, Time, true) + Utils.GetLerpValue(30f, 0f, Projectile.timeLeft, true) * 2f;
            if (projectileToConnectTo != null)
            {
                Texture2D lineTexture = TextureAssets.Extra[47].Value;
                Vector2 start = Projectile.Center;
                Vector2 end = projectileToConnectTo.Center;
                Vector2 scale = new(scaleFactor * 1.5f, (start - end).Length() / lineTexture.Height);
                Vector2 lineOrigin = new(lineTexture.Width * 0.5f, 0f);
                Color drawColor = Color.White * Utils.GetLerpValue(1f, 25f, projectileToConnectTo.timeLeft, true);
                float rotation = (end - start).ToRotation() - MathHelper.PiOver2;
                Main.spriteBatch.Draw(lineTexture, start - Main.screenPosition, null, drawColor, rotation, lineOrigin, scale, SpriteEffects.None, 0f);
            }
            
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             MathHelper.PiOver2 + Projectile.rotation,
                             origin,
                             orthogonalsparkleScale,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             Projectile.rotation,
                             origin,
                             sparkleScale,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             MathHelper.PiOver2 + Projectile.rotation,
                             origin,
                             orthogonalsparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             Projectile.rotation,
                             origin,
                             sparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }
    }
}
