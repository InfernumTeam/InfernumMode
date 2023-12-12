using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    internal class HolySpinningFireBeam : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TelegraphDrawer;

        internal PrimitiveTrailCopy BeamDrawer;

        public bool DrawBeforeNPCs => true;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 360;

        public const float MaxLaserLength = 8300f;

        public float CurrentLaserLength;

        public static int TelegraphTime => 45;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Holy Fire Beam");
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
            Projectile.scale = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                Projectile.Kill();
                return;
            }
            NPC owner = Main.npc[CalamityGlobalNPC.doughnutBoss];

            if (owner.type != ModContent.NPCType<ProfanedGuardianCommander>() || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.width = 60;

            // Do not naturally die.
            Projectile.timeLeft = Lifetime;

            if (Time <= TelegraphTime)
            {
                Projectile.Opacity = Sin(Time / TelegraphTime * Pi) * 2f;
                Projectile.velocity = (TwoPi * Projectile.ai[1] + PiOver2 + Projectile.rotation).ToRotationVector2();
                Projectile.Center = owner.Center + Projectile.velocity;
                Time++;
                return;
            }
            bool fadeOut = owner.Infernum().ExtraAI[CommanderBlenderShouldFadeOutIndex] == 1 && (GuardiansAttackType)owner.ai[0] is GuardiansAttackType.HealerDeathAnimation;
            // Fade in.
            if (!fadeOut)
            {
                Projectile.Opacity = Clamp(Projectile.Opacity + 0.025f, 0f, 1f);
                Projectile.scale = Clamp(Projectile.scale + 0.025f, 0f, 1f);
            }
            else
            {
                Projectile.Opacity = Clamp(Projectile.Opacity - 0.025f, 0f, 1f);
                Projectile.scale = Clamp(Projectile.scale - 0.025f, 0f, 1f);
                if (Projectile.Opacity == 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // Rotate.
            Projectile.rotation += Lerp(0f, 0.014f, Projectile.Opacity);
            Projectile.velocity = (TwoPi * Projectile.ai[1] + PiOver2 + Projectile.rotation).ToRotationVector2();
            Projectile.Center = owner.Center + Projectile.velocity;

            // Sort out length.
            DetermineLaserLength(owner);

            if (Main.myPlayer == Projectile.owner && !InfernumConfig.Instance.ReducedGraphicsConfig)
                CreateTileHitEffects();
            Time++;
        }

        public void DetermineLaserLength(NPC owner)
        {
            Player target = Main.player[owner.target];
            float width = Projectile.width * Projectile.scale * 1.75f;

            // If something is inbetween the npc and the target, go through blocks.
            if (!Collision.CanHitLine(owner.Center, (int)width, (int)1, target.position, target.Hitbox.Width, target.Hitbox.Height))
            {
                CurrentLaserLength = MaxLaserLength;
                return;
            }
            // Else, end at the first tile collision.
            float[] samples = new float[20];
            Collision.LaserScan(owner.Center, Projectile.velocity, width, MaxLaserLength, samples);
            CurrentLaserLength = samples.Average();
        }

        public void CreateTileHitEffects()
        {
            Vector2 endOfLaser = Projectile.Center + Projectile.velocity * (CurrentLaserLength);
            //ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(endOfLaser + Main.rand.NextVector2Circular(15f, 15f), Vector2.Zero, new(95f));
            
            for (int i = 0; i < 10; i++)
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(endOfLaser + Main.rand.NextVector2Circular(35f, 35f), Main.rand.NextVector2Unit() * 2f, new(25f), 0.94f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * Projectile.scale * 1.75f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * CurrentLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.85f;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time > TelegraphTime)
            {
                // Draw a glow effect at the end of the laser.
                Texture2D glowBloom = ModContent.Request<Texture2D>("CalamityMod/UI/ModeIndicator/BloomFlare").Value;
                Texture2D glowCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Vector2 glowPosition = Projectile.Center + Projectile.velocity * CurrentLaserLength;
                Color glowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.3f);
                glowColor.A = 0;
                float glowRotation = Main.GlobalTimeWrappedHourly * 4;
                float scaleInterpolant = (1f + Sin(Main.GlobalTimeWrappedHourly * 5f)) / 2f;
                float scale = Lerp(1.8f, 2.2f, scaleInterpolant);
                Main.spriteBatch.Draw(glowBloom, glowPosition - Main.screenPosition, null, glowColor, glowRotation, glowBloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowCircle, glowPosition - Main.screenPosition, null, glowColor, glowRotation, glowCircle.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public float WidthFunction(float completionRatio) => Projectile.width * Projectile.scale * 2f;

        public Color ColorFunction(float completionRatio)
        {
            float interpolant = (1f + Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float colorInterpolant = Lerp(0.3f, 0.5f, interpolant);
            return Color.Lerp(Color.OrangeRed, Color.Gold, colorInterpolant) * Projectile.Opacity;
        }

        public float TelegraphWidthFunction(float completionRatio) => Projectile.width * 1.5f;

        public Color TelegraphColorFunction(float completionRatio)
        {
            Color orange = Color.Lerp(Color.OrangeRed, WayfinderSymbol.Colors[2], 0.5f);
            return Color.Lerp(orange, WayfinderSymbol.Colors[0], completionRatio) * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Time <= TelegraphTime)
            {
                TelegraphDrawer ??= new PrimitiveTrailCopy(TelegraphWidthFunction, TelegraphColorFunction, null, true, InfernumEffectsRegistry.SideStreakVertexShader);

                bool flipY = Projectile.ai[1] == 0.5f;

                InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
                InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);
                InfernumEffectsRegistry.SideStreakVertexShader.Shader.Parameters["flipY"].SetValue(flipY);

                Vector2 telegraphStartPos = Projectile.Center - Projectile.velocity * 2f;
                Vector2 telegraphEndPos = Projectile.Center + Projectile.velocity * 1750f;

                Vector2[] telegraphDrawPoints = new Vector2[8];
                for (int i = 0; i < telegraphDrawPoints.Length; i++)
                    telegraphDrawPoints[i] = Vector2.Lerp(telegraphStartPos, telegraphEndPos, (float)i / telegraphDrawPoints.Length);

                TelegraphDrawer.DrawPixelated(telegraphDrawPoints, -Main.screenPosition, 40);

                Texture2D warningSymbol = InfernumTextureRegistry.VolcanoWarning.Value;
                Vector2 drawPosition = (Projectile.Center + Projectile.velocity * 280f) - Main.screenPosition;
                Color drawColor = Color.Orange * Projectile.Opacity;
                drawColor.A = 0;
                float rotation = flipY ? Pi : 0f;
                Vector2 origin = warningSymbol.Size() * 0.5f;

                spriteBatch.Draw(warningSymbol, drawPosition, null, drawColor, rotation, origin, 0.8f, SpriteEffects.None, 0f);
                return;
            }

            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GuardiansLaserVertexShader);

            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.CrustyNoise);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.SetShaderTexture2(InfernumTextureRegistry.CultistRayMap);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.UseColor(new Color(255, 221, 135));
            InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["flipY"].SetValue(false);
            float lengthScalar = CurrentLaserLength / MaxLaserLength;
            InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["stretchAmount"].SetValue(4f * lengthScalar);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["pillarVarient"].SetValue(false);
            InfernumEffectsRegistry.GuardiansLaserVertexShader.Shader.Parameters["scrollSpeed"].SetValue(1.8f);

            Vector2 startPos = Projectile.Center - Projectile.velocity * 2f;
            Vector2 endPos = Projectile.Center + Projectile.velocity * CurrentLaserLength * (0.2f * (1f - (lengthScalar * 0.8f)) + 1f);

            Vector2[] drawPoints = new Vector2[54];
            for (int i = 0; i < drawPoints.Length; i++)
                drawPoints[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPoints.Length);

            BeamDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 30);
        }
    }
}
