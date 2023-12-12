using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyFireRift : ModProjectile
    {
        #region Properties
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public bool SpearRift => Projectile.ai[0] == 1;

        public bool MarkedAsDead => Projectile.ai[1] == 1;

        public Vector2 RiftSize
        {
            get;
            set;
        } = new(50f, 50f);

        public float BallSize
        {
            get;
            set;
        } = 55f;
        #endregion

        #region Overrides
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Fire Rift");

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = CommanderSpearThrown.TelegraphTime;
            Projectile.Opacity = 0;
            Projectile.scale = 0;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Die if the commander is non-existant.
            if (HolySineSpear.Commander is null)
            {
                Projectile.Kill();
                return;
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.45f);

            if (!SpearRift)
            {
                // Do not die naturally, the commander will manually kill these.
                Projectile.timeLeft = 240;
                if (!MarkedAsDead)
                {
                    Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
                    Projectile.scale = Clamp(Projectile.scale + 0.1f, 0f, 1f);
                }
                else
                {
                    Projectile.Opacity = Clamp(Projectile.Opacity - 0.05f, 0f, 1f);
                    Projectile.scale = Clamp(Projectile.scale - 0.1f, 0f, 1f);

                    if (Projectile.scale == 0f || Projectile.Opacity == 0f)
                        Projectile.Kill();
                }
            }

            // Spawn a bunch of metaballs.
            if (SpearRift)
            {
                for (int i = 0; i < 3; i++)
                    ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(Projectile.Center +
                        Main.rand.NextVector2Circular(RiftSize.X, RiftSize.Y), Vector2.Zero, new(Main.rand.NextFloat(BallSize * 0.75f, BallSize)), 0.92f);
            }
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public void DrawPortal(SpriteBatch spriteBatch)
        {
            Texture2D fireNoise = InfernumTextureRegistry.WavyNoise.Value;
            Texture2D miscNoise = InfernumTextureRegistry.FireNoise.Value;

            Effect portal = InfernumEffectsRegistry.ProfanedPortalShader.Shader;
            portal.Parameters["sampleTexture"].SetValue(fireNoise);
            portal.Parameters["sampleTexture2"].SetValue(miscNoise);
            portal.Parameters["mainColor"].SetValue(WayfinderSymbol.Colors[1].ToVector3());
            portal.Parameters["secondaryColor"].SetValue(WayfinderSymbol.Colors[2].ToVector3());
            portal.Parameters["resolution"].SetValue(new Vector2(120f));
            portal.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            portal.Parameters["opacity"].SetValue(Projectile.Opacity);
            portal.Parameters["innerGlowAmount"].SetValue(0.8f);
            portal.Parameters["innerGlowDistance"].SetValue(0.15f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, portal, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(fireNoise, Projectile.Center - Main.screenPosition, null, Color.White, 0f, fireNoise.Size() * 0.5f, 2f * Projectile.scale, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!SpearRift)
            {
                DrawPortal(Main.spriteBatch);
                return false;
            }
            float scaleInterpolant = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) * (1f + 0.1f *
                Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * (Pi * 2f) * 3f)) * 0.225f;

            Texture2D texture = InfernumTextureRegistry.Gleam.Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = WayfinderSymbol.Colors[1];
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= scaleInterpolant;
            colorB *= scaleInterpolant;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new Vector2(0.5f, 2f) * Projectile.scale * scaleInterpolant;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float upRight = Projectile.rotation + PiOver4;
            float up = Projectile.rotation + PiOver2;
            float upLeft = Projectile.rotation + 3f * PiOver4;
            float left = Projectile.rotation + Pi;
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upLeft, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upRight, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upLeft, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upRight, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, up, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, left, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, up, origin, scale * 0.36f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, left, origin, scale * 0.36f, spriteEffects, 0);

            return false;
        }
        #endregion
    }
}
