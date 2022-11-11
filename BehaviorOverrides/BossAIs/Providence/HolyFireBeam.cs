using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class HolyFireBeam : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 360;

        public const float LaserLength = 4800f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fire Beam");

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.holyBoss == -1 ||
                !Main.npc[CalamityGlobalNPC.holyBoss].active ||
                Main.npc[CalamityGlobalNPC.holyBoss].ai[0] != (int)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalBladesWithLaser)
            {
                Projectile.Kill();
                return;
            }

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);
            Projectile.scale = (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.velocity = (MathHelper.TwoPi * Projectile.ai[1] + Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[0]).ToRotationVector2();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.75f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.01f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Orange, Color.DarkRed, (float)Math.Pow(completionRatio, 2D));
            if (ProvidenceBehaviorOverride.IsEnraged)
                color = Color.Lerp(Color.Cyan, Color.Lime, (float)Math.Pow(completionRatio, 2D) * 0.5f);

            color *= Projectile.Opacity;
            return color;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            #region Old Drawing
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, specialShader: GameShaders.Misc["Infernum:ProviLaserShader"]);
            Color color = ProvidenceBehaviorOverride.IsEnraged ? Color.Lerp(Color.CadetBlue, Color.Cyan, Time) : Color.Lerp(Color.Gold, Color.Goldenrod, Time);
            GameShaders.Misc["Infernum:ProviLaserShader"].UseColor(color);
            GameShaders.Misc["Infernum:ProviLaserShader"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Streak1", (AssetRequestMode)1));

            float oldGlobalTime = Main.GlobalTimeWrappedHourly;
            Main.GlobalTimeWrappedHourly %= 1f;

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;

            for (int i = 0; i < 2; i++)
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 32);
            Main.instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.GlobalTimeWrappedHourly = oldGlobalTime;
            return false;
            #endregion
            Texture2D sphericalGlow = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/LaserCircle", (AssetRequestMode)1).Value;
            Texture2D mainLaserTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Streak1", (AssetRequestMode)1).Value;
            float rotation = MathHelper.TwoPi * Projectile.ai[1] + Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[0];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin((SpriteSortMode)1, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color2 = Color.Gold;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["Infernum:ProviLaserShader"].UseColor(color);
            GameShaders.Misc["Infernum:ProviLaserShader"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Streak1", (AssetRequestMode)1));
            GameShaders.Misc["Infernum:ProviLaserShader"].Apply();
            Main.spriteBatch.Draw(mainLaserTexture, Projectile.Center - Main.screenPosition, new Rectangle(Projectile.timeLeft * 8, 0, mainLaserTexture.Width * 5, mainLaserTexture.Height), color, rotation, new Vector2(0f, (float)(mainLaserTexture.Width / 2)), new Vector2(LaserLength / 256f / 5f, Projectile.scale), 0, 0f);
            Main.spriteBatch.ExitShaderRegion();
            //Main.spriteBatch.Draw(sphericalGlow, Projectile.Center - Main.screenPosition, null, color, 0f, sphericalGlow.Size() / 2f, Projectile.scale * 2f, 0, 0f);
            //Main.spriteBatch.Draw(sphericalGlow, Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * LaserLength - Main.screenPosition, null, color, 0f, sphericalGlow.Size() / 2f, Projectile.scale * 2f, 0, 0f);
            //Texture2D backglowTexture = TextureAssets.Extra[89].Value;
            //Main.spriteBatch.Draw(backglowTexture, Projectile.Center - Main.screenPosition, null, color, (float)Projectile.timeLeft * 0.08f, backglowTexture.Size() / 2f, new Vector2(0.5f, 3f) * Projectile.scale * 3f, 0, 0f);
            //Main.spriteBatch.Draw(backglowTexture, Projectile.Center - Main.screenPosition, null, color, (float)(-Projectile.timeLeft) * 0.1f, backglowTexture.Size() / 2f, new Vector2(0.5f, 2f) * Projectile.scale * 3f, 0, 0f);
            //Main.spriteBatch.Draw(backglowTexture, Projectile.Center - Main.screenPosition, null, color, 0f, backglowTexture.Size() / 2f, new Vector2(0.6f, 4f) * Projectile.scale * 2f, 0, 0f);
            //Main.spriteBatch.Draw(backglowTexture, Projectile.Center - Main.screenPosition, null, color, 1.57f, backglowTexture.Size() / 2f, new Vector2(0.6f, 6f) * Projectile.scale * 2f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
