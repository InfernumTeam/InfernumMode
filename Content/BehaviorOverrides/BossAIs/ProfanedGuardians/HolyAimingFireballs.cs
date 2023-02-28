using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyAimingFireballs : ModProjectile, IPixelPrimitiveDrawer
    {
        public enum StateType
        {
            Initial,
            Slowing,
            SettingAim,
            FiringBeams
        }

        internal PrimitiveTrailCopy BeamDrawer;

        public ref float Timer => ref Projectile.ai[0];

        public StateType CurrentState
        {
            get
            {
                return (StateType)Projectile.ai[1];
            }
            set
            {
                Projectile.ai[1] = (float)value;
            }
        }

        public static NPC Commander
        {
            get
            {
                if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
                {
                    if (Main.npc[CalamityGlobalNPC.doughnutBoss].type == GuardianComboAttackManager.CommanderType)
                        return Main.npc[CalamityGlobalNPC.doughnutBoss];
                }
                return null;
            }
        }

        public float WidthScalar
        { 
            get
            {
                float interpolant = (Timer - BeamTelegraphTime) / BeamShootTime;
                float smoothedInterpolant = CalamityUtils.PolyInOutEasing(interpolant, 1);
                return MathF.Sin(MathF.PI * (smoothedInterpolant));
            }
        }

        public float SlowdownWaitTime = 30f;

        public float BeamTelegraphTime = 30f;

        public float BeamShootTime = 30f;

        public float BeamLength = 3000f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fireballs");
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.Opacity = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = (int)(SlowdownWaitTime + BeamTelegraphTime + BeamShootTime + 300f);
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            switch (CurrentState)
            {
                case StateType.Initial:
                    if (Timer >= SlowdownWaitTime)
                    {
                        CurrentState++;
                        Timer = 0;
                        return;
                    }

                    Projectile.position += Projectile.velocity;
                    break;

                case StateType.Slowing:
                    Projectile.velocity *= 0.9f;
                    if (Projectile.velocity.Length() <= 2)
                    {
                        Projectile.velocity = Vector2.Zero;
                        CurrentState++;
                        Timer = 0;
                        return;
                    }
                    Projectile.position += Projectile.velocity;
                    break;

                case StateType.SettingAim:
                    if (Commander is null)
                    {
                        Projectile.Kill();
                        return;
                    }
                    Projectile.velocity = Projectile.SafeDirectionTo(Main.player[Commander.target].Center);
                    CurrentState++;
                    Timer = 0;                   
                    return;

                case StateType.FiringBeams:
                    if (Timer == BeamTelegraphTime && !ScreenEffectSystem.AnyBlurOrFlashActive())
                        ScreenEffectSystem.SetFlashEffect(Main.LocalPlayer.Center, 1f, 15);

                    if (Timer >= BeamTelegraphTime + BeamShootTime)
                        Projectile.Kill();
                    break;
            }

            Timer++;
        }

        // This is manually updated when needed.
        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            bool collidingWithBall = projHitbox.Intersects(targetHitbox);
            bool collidingWithBeam = false;

            if (CurrentState is StateType.FiringBeams && Timer > BeamTelegraphTime)
            {
                float _ = 0;
                float width = 30f * WidthScalar;

                collidingWithBall = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength,
                    width, ref _);
            }

            return collidingWithBall || collidingWithBeam;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (CurrentState is StateType.FiringBeams && Timer <= BeamTelegraphTime)
            {
                Texture2D texture = InfernumTextureRegistry.Line.Value;
                Vector2 position = Projectile.Center - Main.screenPosition;
                Color colorInner = Color.Gold * 0.75f;
                colorInner.A = 0;
                Color colorOuter = Color.Lerp(colorInner, Color.White, 0.5f) * 0.75f;
                colorOuter.A = 0;
                float rotation = Projectile.velocity.ToRotation();// - MathHelper.PiOver2;

                float scaleInterpolant = MathHelper.Clamp(MathF.Sin(Timer / BeamTelegraphTime * MathHelper.Pi) * 3f, 0f, 1f);
                Vector2 scaleInner = new(BeamLength / texture.Height,scaleInterpolant);
                Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.5f);
                Vector2 origin = texture.Size() * new Vector2(0f, 0.5f);
                Main.EntitySpriteDraw(texture, position, null, colorOuter, rotation, origin, scaleOuter, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, position, null, colorInner, rotation, origin, scaleInner, SpriteEffects.None, 0);
            }

            Texture2D invis = InfernumTextureRegistry.Invisible.Value;
            Texture2D noise = InfernumTextureRegistry.HarshNoise.Value;
            Effect fireball = InfernumEffectsRegistry.FireballShader.GetShader().Shader;

            fireball.Parameters["sampleTexture2"].SetValue(noise);
            fireball.Parameters["mainColor"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.3f).ToVector3());
            fireball.Parameters["resolution"].SetValue(new Vector2(250f, 250f));
            fireball.Parameters["speed"].SetValue(0.76f);
            fireball.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            fireball.Parameters["zoom"].SetValue(0.0004f);
            fireball.Parameters["dist"].SetValue(60f);
            fireball.Parameters["opacity"].SetValue(Projectile.Opacity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, fireball, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(invis, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, invis.Size() * 0.5f, 70f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public float BeamWidthFunction(float completionRatio) => WidthScalar * 40f;

        public Color BeamColorFunction(float completionRatio) => Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], completionRatio);

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (CurrentState != StateType.FiringBeams || Timer <= BeamTelegraphTime)
                return;

            BeamDrawer ??= new(BeamWidthFunction, BeamColorFunction, null, true, InfernumEffectsRegistry.FireBeamVertexShader);

            InfernumEffectsRegistry.FireBeamVertexShader.UseOpacity(1f);
            InfernumEffectsRegistry.FireBeamVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThinGlow);
            InfernumEffectsRegistry.FireBeamVertexShader.UseColor(Color.LightYellow);

            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * BeamLength;

            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(start, end, (float)i / drawPositions.Length);

            BeamDrawer.DrawPixelated(drawPositions, -Main.screenPosition, 30);
        }
    }
}
