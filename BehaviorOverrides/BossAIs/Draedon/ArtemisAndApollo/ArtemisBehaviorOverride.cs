using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ApolloBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ArtemisBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Artemis>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        #region AI

        // Most attacks are present in Apollo's AI, since Apollo is supposed to be a "manager" for the twins, handling
        // things like NPC summoning and such, while Artemis primarily inherits attributes from Apollo.
        public override bool PreAI(NPC npc)
        {
            // Despawn if Apollo is not present.
            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                return false;
            }

            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechTwinRed = npc.whoAmI;

            // Define the Apollo NPC instance.
            NPC apollo = Main.npc[npc.realLife];

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(apollo);
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hoverSide = ref npc.ai[2];
            ref float phaseTransitionAnimationTime = ref npc.ai[3];
            ref float frame = ref npc.localAI[0];
            ref float hasDoneInitializations = ref npc.localAI[1];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float sideSwitchAttackDelay = ref npc.Infernum().ExtraAI[ExoMechManagement.Twins_SideSwitchDelayIndex];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && hasDoneInitializations == 0f)
            {
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                hasDoneInitializations = 1f;
                npc.netUpdate = true;
            }

            // Reset things and use variables from Apollo.
            npc.damage = 0;
            npc.defDamage = 520;
            npc.dontTakeDamage = apollo.dontTakeDamage;
            npc.target = apollo.target;
            npc.life = apollo.life;
            npc.lifeMax = apollo.lifeMax;

            // Inherit a bunch of things from Apollo, the "manager" of the twins' AI.
            TwinsAttackType apolloAttackType = (TwinsAttackType)(int)apollo.ai[0];
            if (apolloAttackType != TwinsAttackType.LaserRayScarletBursts && apolloAttackType != TwinsAttackType.PlasmaCharges)
            {
                attackState = (int)apollo.ai[0];
                attackTimer = apollo.ai[1];
            }
            hoverSide = -apollo.ai[2];
            phaseTransitionAnimationTime = apollo.ai[3];
            hasSummonedComplementMech = apollo.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            complementMechIndex = apollo.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            wasNotInitialSummon = apollo.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            finalMechIndex = apollo.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            finalPhaseAnimationTime = apollo.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            sideSwitchAttackDelay = apollo.Infernum().ExtraAI[18];
            deathAnimationTimer = apollo.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];
            npc.Calamity().newAI[0] = (int)Artemis.Phase.Charge;

            // Get a target.
            Player target = Main.player[npc.target];

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center - Vector2.UnitY * 2700f;

                attackTimer = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                npc.Calamity().newAI[1] = (int)Artemis.SecondaryPhase.PassiveAndImmune;
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Handle the second phase transition.
            if (phaseTransitionAnimationTime < Phase2TransitionTime && lifeRatio < ExoMechManagement.Phase3LifeRatio)
            {
                npc.dontTakeDamage = true;
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                DoBehavior_DoPhaseTransition(npc, target, ref frame, hoverSide, phaseTransitionAnimationTime);
                return false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime < ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentTwinsPhase >= 6 && !performingDeathAnimation)
            {
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                finalPhaseAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, ref frame, hoverSide, finalPhaseAnimationTime);
                return false;
            }

            // Perform specific attack behaviors.
            if (!performingDeathAnimation)
            {
                switch ((TwinsAttackType)(int)attackState)
                {
                    case TwinsAttackType.BasicShots:
                        DoBehavior_BasicShots(npc, target, sideSwitchAttackDelay > 0f, false, hoverSide, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.SingleLaserBlasts:
                        DoBehavior_SingleLaserBlasts(npc, target, hoverSide, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.FireCharge:
                        DoBehavior_FireCharge(npc, target, hoverSide, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.PlasmaCharges:
                        DoBehavior_PlasmaCharges(npc, target, hoverSide, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.LaserRayScarletBursts:
                        DoBehavior_LaserRayScarletBursts(npc, target, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.GatlingLaserAndPlasmaFlames:
                        DoBehavior_GatlingLaserAndPlasmaFlames(npc, target, hoverSide, ref frame, ref attackTimer);
                        break;
                }
            }
            else
                DoBehavior_DeathAnimation(npc, target, ref frame, ref npc.ModNPC<Artemis>().ChargeFlash, ref deathAnimationTimer);

            // Perform specific combo attack behaviors.
            ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, hoverSide, ref attackTimer, ref frame);
            ExoMechComboAttackContent.UseTwinsAthenaComboAttack(npc, hoverSide, ref attackTimer, ref frame);
            return false;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameX = (int)npc.localAI[0] / 9;
            int frameY = (int)npc.localAI[0] % 9;
            npc.frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
        }


        public static float FlameTrailWidthFunction(NPC npc, float completionRatio) => MathHelper.SmoothStep(21f, 8f, completionRatio) * npc.ModNPC<Artemis>().ChargeFlash;

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio) => MathHelper.SmoothStep(34f, 12f, completionRatio) * npc.ModNPC<Artemis>().ChargeFlash;

        public static float RibbonTrailWidthFunction(float completionRatio)
        {
            float baseWidth = Utils.GetLerpValue(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CalamityUtils.Convert01To010(Utils.GetLerpValue(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return baseWidth + endTipWidth;
        }

        public static Color FlameTrailColorFunction(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Yellow, 0.31f);
            Color endColor = Color.OrangeRed;
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Artemis>().ChargeFlash * trailOpacity;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.56f;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
            Color middleColor = Color.Lerp(Color.Blue, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.DarkBlue, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Artemis>().ChargeFlash * trailOpacity;
            color.A = 0;
            return color;
        }

        public static Color RibbonTrailColorFunction(NPC npc, float completionRatio)
        {
            Color startingColor = new(34, 40, 48);
            Color endColor = new(219, 82, 28);
            return Color.Lerp(startingColor, endColor, (float)Math.Pow(completionRatio, 1.5D)) * npc.Opacity;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (npc.ModNPC<Artemis>().ChargeFlameTrail is null)
                npc.ModNPC<Artemis>().ChargeFlameTrail = new PrimitiveTrail(c => FlameTrailWidthFunction(npc, c), c => FlameTrailColorFunction(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Artemis>().ChargeFlameTrailBig is null)
                npc.ModNPC<Artemis>().ChargeFlameTrailBig = new PrimitiveTrail(c => FlameTrailWidthFunctionBig(npc, c), c => FlameTrailColorFunctionBig(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Artemis>().RibbonTrail is null)
                npc.ModNPC<Artemis>().RibbonTrail = new PrimitiveTrail(RibbonTrailWidthFunction, c => RibbonTrailColorFunction(npc, c));

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

            int numAfterimages = npc.ModNPC<Artemis>().ChargeFlash > 0f ? 0 : 5;
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = npc.Size * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = ExoMechComboAttackContent.EnrageTimer > 0f ? Color.Red : Color.White;

            // Draws a single instance of a regular, non-glowmask based Artemis.
            // This is created to allow easy duplication of them when drawing the charge.
            void drawInstance(Vector2 drawOffset, Color baseColor)
            {
                if (CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < numAfterimages; i += 2)
                    {
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(baseColor, afterimageBaseColor, 0.75f)) * ((numAfterimages - i) / 15f);
                        afterimageColor.A /= 8;

                        Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
                        Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
                    }
                }

                Main.spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(baseColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            // Draw ribbons near the main thruster
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 ribbonOffset = -Vector2.UnitY.RotatedBy(npc.rotation) * 14f;
                ribbonOffset += Vector2.UnitX.RotatedBy(npc.rotation) * direction * 26f;

                float currentSegmentRotation = npc.rotation;
                List<Vector2> ribbonDrawPositions = new();
                for (int i = 0; i < 12; i++)
                {
                    float ribbonCompletionRatio = i / 12f;
                    float wrappedAngularOffset = MathHelper.WrapAngle(npc.oldRot[i + 1] - currentSegmentRotation) * 0.3f;
                    float segmentRotationOffset = MathHelper.Clamp(wrappedAngularOffset, -0.12f, 0.12f);

                    // Add a sinusoidal offset that goes based on time and completion ratio to create a waving-flag-like effect.
                    // This is dampened for the first few points to prevent weird offsets. It is also dampened by high velocity.
                    float sinusoidalRotationOffset = (float)Math.Sin(ribbonCompletionRatio * 2.22f + Main.GlobalTimeWrappedHourly * 3.4f) * 1.36f;
                    float sinusoidalRotationOffsetFactor = Utils.GetLerpValue(0f, 0.37f, ribbonCompletionRatio, true) * direction * 24f;
                    sinusoidalRotationOffsetFactor *= Utils.GetLerpValue(24f, 16f, npc.velocity.Length(), true);

                    Vector2 sinusoidalOffset = Vector2.UnitY.RotatedBy(npc.rotation + sinusoidalRotationOffset) * sinusoidalRotationOffsetFactor;
                    Vector2 ribbonSegmentOffset = Vector2.UnitY.RotatedBy(currentSegmentRotation) * ribbonCompletionRatio * 540f + sinusoidalOffset;
                    ribbonDrawPositions.Add(npc.Center + ribbonSegmentOffset + ribbonOffset);

                    currentSegmentRotation += segmentRotationOffset;
                }
                npc.ModNPC<Artemis>().RibbonTrail.Draw(ribbonDrawPositions, -Main.screenPosition, 66);
            }

            int instanceCount = (int)MathHelper.Lerp(1f, 15f, npc.ModNPC<Artemis>().ChargeFlash);
            Color baseInstanceColor = Color.Lerp(lightColor, Color.White, npc.ModNPC<Artemis>().ChargeFlash);
            baseInstanceColor.A = (byte)(int)(255f - npc.ModNPC<Artemis>().ChargeFlash * 255f);

            Main.spriteBatch.EnterShaderRegion();

            ExoMechAIUtilities.DrawFinalPhaseGlow(spriteBatch, npc, texture, center, frame, origin);
            drawInstance(Vector2.Zero, baseInstanceColor);

            if (instanceCount > 1)
            {
                baseInstanceColor *= 0.04f;
                float backAfterimageOffset = MathHelper.SmoothStep(0f, 2f, npc.ModNPC<Artemis>().ChargeFlash);
                for (int i = 0; i < instanceCount; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / instanceCount + Main.GlobalTimeWrappedHourly * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    drawInstance(drawOffset, baseInstanceColor);
                }
            }

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Artemis/ArtemisGlow");
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();

            // Draw a telegraph line as necessary.
            if (npc.ai[0] == (int)TwinsAttackType.SingleLaserBlasts)
            {
                float telegraphInterpolant = npc.Infernum().ExtraAI[3];
                Vector2 aimDirection = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                Vector2 telegraphStart = npc.Center + aimDirection * (ExoMechManagement.ExoTwinsAreInSecondPhase ? 112f : 78f);
                Vector2 telegraphEnd = telegraphStart + aimDirection * 4000f;

                if (telegraphInterpolant > 0f)
                    Main.spriteBatch.DrawLineBetter(telegraphStart, telegraphEnd, Color.Orange * telegraphInterpolant, telegraphInterpolant * 5f);
            }

            // Draw a flame trail on the thrusters if needed. This happens during charges.
            if (npc.ModNPC<Artemis>().ChargeFlash > 0f)
            {
                for (int direction = -1; direction <= 1; direction++)
                {
                    Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? 18f : 60f).RotatedBy(npc.rotation);
                    baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

                    float backFlameLength = direction == 0f ? 700f : 190f;
                    Vector2 drawStart = npc.Center + baseDrawOffset;
                    Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * npc.ModNPC<Artemis>().ChargeFlash * backFlameLength;
                    Vector2[] drawPositions = new Vector2[]
                    {
                        drawStart,
                        drawEnd
                    };

                    if (direction == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 8f;
                            npc.ModNPC<Artemis>().ChargeFlameTrailBig.Draw(drawPositions, drawOffset - Main.screenPosition, 70);
                        }
                    }
                    else
                        npc.ModNPC<Artemis>().ChargeFlameTrail.Draw(drawPositions, -Main.screenPosition, 70);
                }
            }

            return false;
        }
        #endregion Frames and Drawcode
    }
}
