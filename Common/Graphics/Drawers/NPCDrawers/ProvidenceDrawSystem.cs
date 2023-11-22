using CalamityMod.NPCs.Providence;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Cutscenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Providence.ProvidenceBehaviorOverride;

namespace InfernumMode.Common.Graphics.Drawers.NPCDrawers
{
    public class ProvidenceDrawSystem : BaseNPCDrawerSystem
    {
        public override int AssosiatedNPCType => ModContent.NPCType<Providence>();

        public override void DrawToMainTarget(SpriteBatch spriteBatch)
        {
            // Initialize the 3D strip.
            AssosiatedNPC.Infernum().Optional3DStripDrawer ??= new(RuneHeightFunction, c => RuneColorFunction(AssosiatedNPC, c));

            string baseTextureString = "CalamityMod/NPCs/Providence/";
            string baseGlowTextureString = baseTextureString + "Glowmasks/";
            string rockTextureString = "InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/ProvidenceRock";

            string getTextureString = baseTextureString + "Providence";
            string getTextureGlowString;
            string getTextureGlow2String;

            bool useDefenseFrames = AssosiatedNPC.localAI[1] == 1f;
            float lifeRatio = AssosiatedNPC.life / (float)AssosiatedNPC.lifeMax;
            ProvidenceAttackType attackType = (ProvidenceAttackType)(int)AssosiatedNPC.ai[0];

            ref float burnIntensity = ref AssosiatedNPC.localAI[3];

            // Don't draw anything, the cutscene projectile will handle it.
            if (attackType == ProvidenceAttackType.CrystalForm)
                return;

            void drawProvidenceInstance(Vector2 baseDrawPosition, int frameOffset, Color baseDrawColor)
            {
                rockTextureString = "InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/";
                if (AssosiatedNPC.localAI[0] == (int)ProvidenceFrameDrawingType.CocoonState)
                {
                    if (!useDefenseFrames)
                    {
                        rockTextureString += "ProvidenceDefenseRock";
                        getTextureString = baseTextureString + "ProvidenceDefense";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseGlow2";
                    }
                    else
                    {
                        rockTextureString += "ProvidenceDefenseAltRock";
                        getTextureString = baseTextureString + "ProvidenceDefenseAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceDefenseAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceDefenseAltGlow2";
                    }
                }
                else
                {
                    if (AssosiatedNPC.localAI[2] == 0f)
                    {
                        rockTextureString += "ProvidenceRock";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceGlow2";
                    }
                    else if (AssosiatedNPC.localAI[2] == 1f)
                    {
                        getTextureString = baseTextureString + "ProvidenceAlt";
                        rockTextureString += "ProvidenceAltRock";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAltGlow2";
                    }
                    else if (AssosiatedNPC.localAI[2] == 2f)
                    {
                        rockTextureString += "ProvidenceAttackRock";
                        getTextureString = baseTextureString + "ProvidenceAttack";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackGlow2";
                    }
                    else
                    {
                        rockTextureString += "ProvidenceAttackAltRock";
                        getTextureString = baseTextureString + "ProvidenceAttackAlt";
                        getTextureGlowString = baseGlowTextureString + "ProvidenceAttackAltGlow";
                        getTextureGlow2String = baseGlowTextureString + "ProvidenceAttackAltGlow2";
                    }
                }

                float wingVibrance = 1f;
                getTextureGlowString += "Night";

                Texture2D generalTexture = ModContent.Request<Texture2D>(getTextureString).Value;
                Texture2D crystalTexture = ModContent.Request<Texture2D>(getTextureGlow2String).Value;
                Texture2D wingTexture = ModContent.Request<Texture2D>(getTextureGlowString).Value;
                Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;

                SpriteEffects spriteEffects = SpriteEffects.None;
                if (AssosiatedNPC.spriteDirection == 1)
                    spriteEffects = SpriteEffects.FlipHorizontally;

                Vector2 drawOrigin = AssosiatedNPC.frame.Size() * 0.5f;

                // Draw the crystal behind everything. It will appear if providence is herself invisible.
                if (AssosiatedNPC.localAI[3] <= 0f)
                {
                    float backglowTelegraphInterpolant = 0f;
                    if (AssosiatedNPC.ai[0] == (int)ProvidenceAttackType.RockMagicRitual)
                        backglowTelegraphInterpolant = AssosiatedNPC.Infernum().ExtraAI[2];

                    Vector2 crystalOrigin = fatCrystalTexture.Size() * 0.5f;

                    // Draw a backglow if necessary.
                    if (backglowTelegraphInterpolant > 0f)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 drawOffset = (TwoPi * i / 8f).ToRotationVector2() * backglowTelegraphInterpolant * 12f;
                            Color backglowColor = Color.Pink with
                            {
                                A = 0
                            };
                            Main.spriteBatch.Draw(fatCrystalTexture, AssosiatedNPC.Center - Main.screenPosition + drawOffset, null, backglowColor, AssosiatedNPC.rotation, crystalOrigin, AssosiatedNPC.scale, spriteEffects, 0f);
                        }
                    }

                    for (int i = 4; i >= 0; i--)
                    {
                        Color afterimageColor = Color.White * (1f - i / 5f);
                        Vector2 crystalDrawPosition = Vector2.Lerp(AssosiatedNPC.oldPos[i], AssosiatedNPC.position, 0.4f) + AssosiatedNPC.Size * 0.5f - Main.screenPosition;
                        Main.spriteBatch.Draw(fatCrystalTexture, crystalDrawPosition, null, afterimageColor, AssosiatedNPC.rotation, crystalOrigin, AssosiatedNPC.scale, spriteEffects, 0f);
                    }
                }

                int frameHeight = generalTexture.Height / 3;
                if (frameHeight <= 0)
                    frameHeight = 1;

                Rectangle frame = generalTexture.Frame(1, 3, 0, (AssosiatedNPC.frame.Y / frameHeight + frameOffset) % 3);

                // Draw the converging shell if applicable.
                float rockReformOffset = AssosiatedNPC.Infernum().ExtraAI[RockReformOffsetIndex];
                if (rockReformOffset > 0f)
                {
                    Texture2D headRock = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/ProvidenceRock1").Value;
                    Texture2D leftBodyRock = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/ProvidenceRock2").Value;
                    Texture2D rightBodyRock = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/Sheets/ProvidenceRock3").Value;

                    float rockOffsetRotation = TwoPi * rockReformOffset / 2500f;
                    float rockOpacity = Utils.GetLerpValue(700f, 300f, rockReformOffset, true);
                    Vector2 crystalOffsetCorrection = -Vector2.UnitY.RotatedBy(AssosiatedNPC.rotation * AssosiatedNPC.spriteDirection) * AssosiatedNPC.scale * 40f;
                    Vector2 headDrawPosition = baseDrawPosition - new Vector2(0.1f, 1f).RotatedBy(AssosiatedNPC.rotation * AssosiatedNPC.spriteDirection) * (AssosiatedNPC.scale * 60f + rockReformOffset) + crystalOffsetCorrection;
                    Vector2 leftDrawPosition = baseDrawPosition - Vector2.UnitX.RotatedBy(AssosiatedNPC.rotation * AssosiatedNPC.spriteDirection) * (rockReformOffset - 10f) + crystalOffsetCorrection;
                    Vector2 rightDrawPosition = baseDrawPosition + Vector2.UnitX.RotatedBy(AssosiatedNPC.rotation * AssosiatedNPC.spriteDirection) * (rockReformOffset - 12f) + crystalOffsetCorrection;
                    Main.spriteBatch.Draw(headRock, headDrawPosition, null, baseDrawColor * rockOpacity, AssosiatedNPC.rotation + rockOffsetRotation, headRock.Size() * new Vector2(0.5f, 1f), AssosiatedNPC.scale, spriteEffects, 0f);
                    Main.spriteBatch.Draw(leftBodyRock, leftDrawPosition, null, baseDrawColor * rockOpacity, AssosiatedNPC.rotation + rockOffsetRotation, leftBodyRock.Size() * new Vector2(1f, 0.5f), AssosiatedNPC.scale, spriteEffects, 0f);
                    Main.spriteBatch.Draw(rightBodyRock, rightDrawPosition, null, baseDrawColor * rockOpacity, AssosiatedNPC.rotation + rockOffsetRotation, leftBodyRock.Size() * new Vector2(0f, 0.5f), AssosiatedNPC.scale, spriteEffects, 0f);
                }

                // Draw the base texture.
                 Main.spriteBatch.Draw(generalTexture, baseDrawPosition, frame, AssosiatedNPC.GetAlpha(baseDrawColor), AssosiatedNPC.rotation, drawOrigin, AssosiatedNPC.scale, spriteEffects, 0f);

                // Draw the wings.
                DrawProvidenceWings(AssosiatedNPC, wingTexture, wingVibrance, baseDrawPosition, frame, drawOrigin, spriteEffects);

                // Draw the crystals.
                for (int i = 0; i < 9; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 9f).ToRotationVector2() * 2f;
                    Main.spriteBatch.Draw(crystalTexture, baseDrawPosition + drawOffset, frame, Color.White with
                    {
                        A = 0
                    } * AssosiatedNPC.Opacity * Pow(1f - AssosiatedNPC.localAI[3], 3f), AssosiatedNPC.rotation, drawOrigin, AssosiatedNPC.scale, spriteEffects, 0f);
                }
            }

            int totalProvidencesToDraw = (int)Lerp(1f, 30f, burnIntensity);
            for (int i = 0; i < totalProvidencesToDraw; i++)
            {
                float offsetAngle = TwoPi * i * 2f / totalProvidencesToDraw;
                float drawOffsetScalar = Sin(offsetAngle * 6f + Main.GlobalTimeWrappedHourly * Pi);
                drawOffsetScalar *= Pow(burnIntensity, 1.4f) * 36f;
                drawOffsetScalar *= Lerp(1f, 2f, 1f - lifeRatio);

                Vector2 drawOffset = offsetAngle.ToRotationVector2() * drawOffsetScalar;
                if (totalProvidencesToDraw <= 1)
                    drawOffset = Vector2.Zero;

                Vector2 drawPosition = AssosiatedNPC.Center - Main.screenPosition + drawOffset;

                Color baseColor = Color.White * (Lerp(0.4f, 0.8f, burnIntensity) / totalProvidencesToDraw * 7f);
                baseColor.A = 0;
                baseColor = Color.Lerp(Color.White, baseColor, burnIntensity);
                if (IsEnraged)
                {
                    baseColor = Color.Lerp(baseColor, Color.Cyan with
                    {
                        A = 0
                    }, 0.5f);
                }

                drawProvidenceInstance(drawPosition, 0, baseColor);
            }

            // Draw the rock texture above the bloom effects.
            Texture2D rockTexture = ModContent.Request<Texture2D>(rockTextureString).Value;
            float opacity = Utils.GetLerpValue(0.038f, 0.04f, lifeRatio, true) * (1f - AssosiatedNPC.localAI[3]) * 0.6f;
            Main.spriteBatch.Draw(rockTexture, AssosiatedNPC.Center - Main.screenPosition, AssosiatedNPC.frame, AssosiatedNPC.GetAlpha(Color.White) * opacity, AssosiatedNPC.rotation, AssosiatedNPC.frame.Size() * 0.5f, AssosiatedNPC.scale, 0, 0);
        }

        public override void DrawMainTargetContents(SpriteBatch spriteBatch)
        {
            ref float glowIntensity = ref AssosiatedNPC.Infernum().ExtraAI[DeathAnimationGlowIntensityIndex];

            //if ((ProvidenceAttackType)AssosiatedNPC.ai[0] != ProvidenceAttackType.CrystalForm)
            //{
            //    Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;

            //    Main.spriteBatch.Draw(fatCrystalTexture, AssosiatedNPC.Center + Vector2.UnitY * 55f - Main.screenPosition, null, Color.White, 0f, fatCrystalTexture.Size() * 0.5f, AssosiatedNPC.scale, SpriteEffects.None, 0f);
            //}

            bool shouldBurn = glowIntensity > 0f/* && AssosiatedNPC.Infernum().ExtraAI[DeathEffectTimerIndex] > 0f*/;
            if (shouldBurn)
            {
                Effect burn = InfernumEffectsRegistry.SpriteBurnShader.GetShader().Shader;
                burn.Parameters["noiseZoom"]?.SetValue(20f);
                burn.Parameters["noiseSpeed"]?.SetValue(2f);
                burn.Parameters["noiseFactor"]?.SetValue(1.8f);
                burn.Parameters["brightnessFactor"]?.SetValue(1.9f);
                burn.Parameters["thickness"]?.SetValue(0.006f);
                burn.Parameters["time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                float burnRatio = Lerp(0.3f, 0.075f, Pow(AssosiatedNPC.Infernum().ExtraAI[DeathEffectTimerIndex] / 400f, 3f));
                burn.Parameters["burnRatio"]?.SetValue(burnRatio);
                burn.Parameters["innerNoiseFactor"]?.SetValue(Lerp(0.2f, 1f, Pow(AssosiatedNPC.Infernum().ExtraAI[DeathEffectTimerIndex] / 400f, 3f)) * glowIntensity);
                burn.Parameters["distanceMultiplier"]?.SetValue(1f);
                burn.Parameters["resolution"]?.SetValue(Utilities.CreatePixelationResolution(MainTarget.Target.Size()));
                burn.Parameters["focalPointUV"]?.SetValue((AssosiatedNPC.Center + Vector2.UnitY * 55f - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight));
                burn.Parameters["burnColor"]?.SetValue(DoGPostProviCutscene.TimeColor.ToVector3());

                InfernumTextureRegistry.HarshNoise.Value.SetTexture1();
                InfernumTextureRegistry.HarshNoise.Value.SetTexture2();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, burn, Main.GameViewMatrix.TransformationMatrix);
            }

            spriteBatch.Draw(MainTarget.Target, Vector2.Zero, Color.White);

            if (shouldBurn)
                Main.spriteBatch.ExitShaderRegion();

            // Draw the rune strip on top of everything else during the ritual attack.
            if (AssosiatedNPC.ai[0] == (int)ProvidenceAttackType.RockMagicRitual)
            {
                Main.spriteBatch.SetBlendState(BlendState.NonPremultiplied);
                AssosiatedNPC.Infernum().Optional3DStripDrawer.UseBandTexture(ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/TerminusSymbols"));
                AssosiatedNPC.Infernum().Optional3DStripDrawer.Draw(AssosiatedNPC.Center - Vector2.UnitX * 80f - Main.screenPosition, AssosiatedNPC.Center + Vector2.UnitX * 80f - Main.screenPosition, 0.4f, 0f, 0f);
                Main.spriteBatch.ResetBlendState();
            }
        }
    }
}
