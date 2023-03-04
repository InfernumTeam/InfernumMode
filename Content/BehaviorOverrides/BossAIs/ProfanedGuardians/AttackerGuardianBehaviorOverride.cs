using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Projectiles.Wayfinder;
using CalamityMod.Buffs.StatDebuffs;
using InfernumMode.Common.Graphics;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Buffs;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        internal PrimitiveTrailCopy DashTelegraphDrawer;

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianDefender>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianHealer>()).ToInt();

        public const float ImmortalUntilPhase2LifeRatio = 0.75f;

        public const float Phase2LifeRatio = 0.6f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianCommander>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ImmortalUntilPhase2LifeRatio,
            Phase4LifeRatio
        };

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianDefender>());
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianHealer>());
                npc.localAI[1] = 1f;
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Despawn if no valid target exists.
            npc.timeLeft = 3600;
            Player target = Main.player[npc.target];
            if ((!target.active || target.dead) || target.Center.X < WorldSaveSystem.ProvidenceArena.X * 16)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
                if (npc.timeLeft < 180)
                    npc.timeLeft = 180;
                if (!npc.WithinRange(target.Center, 2000f) || target.dead)
                    npc.active = false;

                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyFireWall>());
                return false;
            }

            float lifeRatio = (float)npc.life / npc.lifeMax;

            // Don't take damage if other guardians are around.
            npc.dontTakeDamage = false;
            if (TotalRemaininGuardians == 3f || (TotalRemaininGuardians == 2f && lifeRatio < 0.75f))
                npc.dontTakeDamage = true;

            else if (TotalRemaininGuardians == 2f)
            {
                npc.Calamity().DR = 0.9999f;
                npc.chaseable = false;
            }
            else
                npc.Calamity().DR = 0.4f;

            // Reset fields.
            npc.Infernum().ExtraAI[DefenderShouldGlowIndex] = 0;

            // Give the player infinite flight time.
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(npc.Center) <= 10000f)
                {
                    player.wingTime = player.wingTimeMax;
                    player.AddBuff(ModContent.BuffType<ElysianGrace>(), 2, true);
                }
            }

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Reset opacities depending on whether they are being drawn or not.
            ref float smearOpacity = ref npc.Infernum().ExtraAI[CommanderSpearSmearOpacityIndex];
            if (npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex] == 1)
                smearOpacity = MathHelper.Clamp(smearOpacity + 0.1f, 0f, 1f);
            else
                smearOpacity = MathHelper.Clamp(smearOpacity - 0.1f, 0f, 1f);

            ref float blackBarOpacity = ref npc.Infernum().ExtraAI[CommanderBlackBarsOpacityIndex];
            if (npc.Infernum().ExtraAI[CommanderDrawBlackBarsIndex] == 1)
                blackBarOpacity = MathHelper.Clamp(blackBarOpacity + 0.1f, 0f, 1f);
            else
                blackBarOpacity = MathHelper.Clamp(blackBarOpacity - 0.1f, 0f, 1f);

            npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex] = 0;

            //if (attackState >= (float)GuardiansAttackType.DefenderDeathAnimation)
            //    npc.Infernum().ShouldUseSaturationBlur = true;

            // Do attacks.
            switch ((GuardiansAttackType)attackState)
            {
                case GuardiansAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;

                case GuardiansAttackType.FlappyBird:
                    DoBehavior_FlappyBird(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.SoloHealer:
                    DoBehavior_SoloHealer(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.SoloDefender:
                    DoBehavior_SoloDefender(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.HealerAndDefender:
                    DoBehavior_HealerAndDefender(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.HealerDeathAnimation:
                    DoBehavior_HealerDeathAnimation(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.SpearDashAndGroundSlam:
                    DoBehavior_SpearDashAndGroundSlam(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.CrashRam:
                    DoBehavior_CrashRam(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.DefenderDeathAnimation:
                    DoBehavior_DefenderDeathAnimation(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.LargeGeyserAndFireCharge:
                    DoBehavior_LargeGeyserAndFireCharge(npc, target, ref attackTimer);
                    break;

                case GuardiansAttackType.DogmaLaserBall:
                    DoBehavior_DogmaLaserBall(npc, target, ref attackTimer);
                    break;

                case GuardiansAttackType.BerdlySpears:
                    DoBehavior_BerdlySpears(npc, target, ref attackTimer);
                    break;

                case GuardiansAttackType.SpearSpinThrow:
                    DoBehavior_SpearSpinThrow(npc, target, ref attackTimer);
                    break;

                case GuardiansAttackType.CommanderDeathAnimation:
                    DoBehavior_CommanderDeathAnimation(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_CommanderDeathAnimation(NPC npc, Player target, ref float attackTimer)
        {
            int widthExpandDelay = 90;
            int firstExpansionTime = 20;
            int secondExpansionDelay = 1;
            int secondExpansionTime = 132;
            ref float fadeOutFactor = ref npc.Infernum().ExtraAI[0];
            ref float brightnessWidthFactor = ref npc.Infernum().ExtraAI[CommanderBrightnessWidthFactorIndex];

            // Slow to a screeching halt.
            npc.velocity *= 0.9f;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Close the boss bar.
            npc.Calamity().ShouldCloseHPBar = true;

            if (attackTimer == widthExpandDelay + firstExpansionTime - 10f)
                SoundEngine.PlaySound(ProvidenceBoss.HolyRaySound with { Volume = 3f, Pitch = 0.4f });

            // Determine the brightness width factor.
            float expansion1 = Utils.GetLerpValue(widthExpandDelay, widthExpandDelay + firstExpansionTime, attackTimer, true) * 0.9f;
            float expansion2 = Utils.GetLerpValue(0f, secondExpansionTime, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay, true) * 3.2f;
            brightnessWidthFactor = expansion1 + expansion2;
            fadeOutFactor = Utils.GetLerpValue(0f, -25f, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay - secondExpansionTime, true);

            // Fade out over time.
            npc.Opacity = Utils.GetLerpValue(3f, 1.9f, brightnessWidthFactor, true);

            // Disappear and drop loot.
            if (attackTimer >= widthExpandDelay + firstExpansionTime + secondExpansionDelay + secondExpansionTime)
            {
                npc.life = 0;
                npc.Center = target.Center;
                npc.checkDead();
                npc.active = false;
            }
        }     
        #endregion AI and Behaviors

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int afterimageCount = 7;
            float brightnessWidthFactor = npc.Infernum().ExtraAI[CommanderBrightnessWidthFactorIndex];
            float fadeToBlack = Utils.GetLerpValue(1.84f, 2.66f, brightnessWidthFactor, true);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianCommanderGlow").Value;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = npc.frame.Size() * 0.5f;

            bool shouldDrawShield = (GuardiansAttackType)npc.ai[0] is GuardiansAttackType.SoloHealer or GuardiansAttackType.SoloDefender or GuardiansAttackType.HealerAndDefender;
            float shieldOpacity = (GuardiansAttackType)npc.ai[0] is GuardiansAttackType.SoloHealer ? 1f : 0.5f;

            // Draw the pillar of light behind the guardian when ready.
            if (brightnessWidthFactor > 0f)
            {
                if (!Main.dedServ)
                {
                    if (!Filters.Scene["CrystalDestructionColor"].IsActive())
                        Filters.Scene.Activate("CrystalDestructionColor");

                    Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(Color.Orange.ToVector3());
                    Filters.Scene["CrystalDestructionColor"].GetShader().UseIntensity(Utils.GetLerpValue(0.96f, 1.92f, brightnessWidthFactor, true) * 0.9f);
                }

                Vector2 lightPillarPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 3000f;
                for (int i = 0; i < 16; i++)
                {
                    float intensity = MathHelper.Clamp(brightnessWidthFactor * 1.1f - i / 15f, 0f, 1f);
                    Vector2 lightPillarOrigin = new(TextureAssets.MagicPixel.Value.Width / 2f, TextureAssets.MagicPixel.Value.Height);
                    Vector2 lightPillarScale = new((float)Math.Sqrt(intensity + i) * brightnessWidthFactor * 200f, 6f);
                    Color lightPillarColor = new Color(0.7f, 0.55f, 0.38f, 0f) * intensity * npc.Infernum().ExtraAI[0] * 0.4f;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, lightPillarPosition, null, lightPillarColor, 0f, lightPillarOrigin, lightPillarScale, 0, 0f);
                }
            }

            // Draw afterimages of the commander.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageDrawColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageDrawColor * (1f - fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            // Draw back afterimages, indicating that the guardian is fading away into ashes.
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            float radius = Utils.Remap(npc.Opacity, 1f, 0f, 0f, 55f);
            if (radius > 0.5f && npc.ai[0] == (int)GuardiansAttackType.CommanderDeathAnimation)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * radius;
                    Color backimageColor = Color.Black;
                    backimageColor.A = (byte)MathHelper.Lerp(164f, 0f, npc.Opacity);
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backimageColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            if (TotalRemaininGuardians == 1 && npc.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex] == 1f)
                DrawDashTelegraph(npc);

            if (shouldDrawShield)
                DrawBackglowEffects(npc, spriteBatch, texture);

            if (npc.Infernum().ExtraAI[CommanderFireAfterimagesIndex] == 1)
                PrepareFireAfterimages(npc, spriteBatch, direction);

            if ((GuardiansAttackType)npc.ai[0] > GuardiansAttackType.HealerDeathAnimation)
                DefenderGuardianBehaviorOverride.DrawBackglow(npc, spriteBatch, texture);

            spriteBatch.Draw(texture, drawPosition, npc.frame, Color.Lerp(npc.GetAlpha(lightColor), Color.Black * npc.Opacity, fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Black, fadeToBlack) * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);

            // Draw an overlay.
            ref float glowAmount = ref npc.Infernum().ExtraAI[CommanderAngerGlowAmountIndex];
            if (glowAmount > 0f && (GuardiansAttackType)npc.ai[0] is GuardiansAttackType.HealerDeathAnimation)
                DrawAngerOverlay(npc, spriteBatch, texture, glowmask, lightColor, direction, glowAmount);

            if (shouldDrawShield)
                DrawHealerShield(npc, spriteBatch, 3.5f, shieldOpacity);
            return false;
        }

        public void DrawDashTelegraph(NPC npc)
        {
            DashTelegraphDrawer ??= new PrimitiveTrailCopy(c => 65f,
                c => DefenderGuardianBehaviorOverride.DashTelegraphColor(),
                null, true, InfernumEffectsRegistry.SideStreakVertexShader);

            InfernumEffectsRegistry.SideStreakVertexShader.SetShaderTexture(InfernumTextureRegistry.CultistRayMap);
            InfernumEffectsRegistry.SideStreakVertexShader.UseOpacity(0.3f);

            Vector2 startPos = npc.Center;
            float distance = npc.Distance(Main.player[npc.target].Center);
            Vector2 direction = npc.DirectionTo(Main.player[npc.target].Center);
            Vector2 endPos = npc.Center + direction * distance;
            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(startPos, endPos, (float)i / drawPositions.Length);

            DashTelegraphDrawer.Draw(drawPositions, -Main.screenPosition, 30);

            // Draw arrows.
            Texture2D arrowTexture = InfernumTextureRegistry.Arrow.Value;

            Color drawColor = Color.Orange;
            drawColor.A = 0;
            Vector2 drawPosition = (startPos + direction * 120f) - Main.screenPosition;
            for (int i = 1; i < distance / 125f; i++)
            {
                Vector2 arrowOrigin = arrowTexture.Size() * 0.5f;
                float arrowRotation = direction.ToRotation() + MathHelper.PiOver2;
                float sineValue = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 10.5f - i)) / 2f;
                float finalOpacity = CalamityUtils.SineInOutEasing(sineValue, 1);
                Main.spriteBatch.Draw(arrowTexture, drawPosition, null, drawColor * finalOpacity, arrowRotation, arrowOrigin, 0.75f, SpriteEffects.None, 0f);
                drawPosition += direction * 75f;
            }
        }

        public static void DrawBackglowEffects(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            // Glow effect.
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color drawColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.5f);
            drawColor.A = 0;
            Vector2 origin = glow.Size() * 0.5f;
            spriteBatch.Draw(glow, drawPosition, null, drawColor, 0f, origin, 3.5f, SpriteEffects.None, 0f);

            // Draw a glow effect at the end of the laser.
            Texture2D glowBloom = ModContent.Request<Texture2D>("CalamityMod/UI/ModeIndicator/BloomFlare").Value;
            Texture2D glowCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 glowPosition = npc.Center - Main.screenPosition;
            Color glowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.3f);
            glowColor.A = 0;
            float glowRotation = Main.GlobalTimeWrappedHourly * 3;
            float scaleInterpolant = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f)) / 2f;
            float scale = MathHelper.Lerp(3.6f, 4.1f, scaleInterpolant);
            Main.spriteBatch.Draw(glowBloom, glowPosition, null, glowColor, glowRotation, glowBloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowBloom, glowPosition, null, glowColor, glowRotation * -1, glowBloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None, 0f);
            // Backglow
            int backglowAmount = 12;
            float sine = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
            float backglowDistance = MathHelper.Lerp(4.5f, 6.5f, sine);
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * backglowDistance;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }

        public static void DrawAngerOverlay(NPC npc, SpriteBatch spriteBatch, Texture2D texture, Texture2D glowmask, Color lightColor, SpriteEffects direction, float glowAmount)
        {
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.OrangeRed) with { A = 0 } * glowAmount * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, npc.Center - Main.screenPosition, npc.frame, WayfinderSymbol.Colors[0] with { A = 0 } * glowAmount * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
        }

        public static void PrepareFireAfterimages(NPC npc, SpriteBatch spriteBatch, SpriteEffects direction)
        {
            Texture2D afterTexture = InfernumTextureRegistry.GuardianCommanderGlow.Value;
            float length = npc.Infernum().ExtraAI[CommanderFireAfterimagesLengthIndex];
            float timer = (GuardiansAttackType)npc.ai[0] is GuardiansAttackType.SpearDashAndGroundSlam ? npc.Infernum().ExtraAI[1] : npc.ai[1];
            float fadeOutLength = 6f;
            int maxAfterimages = 5;

            DrawFireAfterimages(npc, spriteBatch, afterTexture, direction, length, timer, fadeOutLength, maxAfterimages);
        }

        public static void DrawFireAfterimages(NPC npc, SpriteBatch spriteBatch, Texture2D afterTexture, SpriteEffects direction, float length, float timer, float fadeOutLength, int maxAfterimages)
        {
            if (timer < maxAfterimages)
                maxAfterimages = (int)timer;
            else if (timer >= length - fadeOutLength)
                maxAfterimages = (int)MathHelper.Lerp(6f, 0f, (timer - (length - fadeOutLength)) / fadeOutLength);

            // Failsafe
            if (maxAfterimages > npc.oldPos.Length)
                maxAfterimages = npc.oldPos.Length;

            for (int i = 0; i < maxAfterimages; i++)
            {
                Vector2 basePosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Vector2 positionOffset = (npc.velocity * MathHelper.Lerp(0f, 8f, (float)i / npc.oldPos.Length));
                spriteBatch.Draw(afterTexture, basePosition + positionOffset, null, WayfinderSymbol.Colors[1] with { A = 0 } * 0.8f * npc.Opacity, npc.rotation, afterTexture.Size() * 0.5f, npc.scale * 0.8f, direction, 0f);
            }
        }

        public static void DrawHealerShield(NPC npc, SpriteBatch spriteBatch, float scaleFactor, float opacity)
        {
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            float scale = MathHelper.Lerp(0.15f, 0.155f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f) * 0.5f + 0.5f) * scaleFactor;
            float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // Prepare the forcefield opacity.
            float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity * (opacity * 0.9f + 0.1f));
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            Color edgeColor = Color.DeepPink;
            Color shieldColor = Color.LightPink;

            // Prepare the forcefield colors.
            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            // Draw the forcefield. This doesn't happen if the lighting behind is too low, to ensure that it doesn't draw if underground or in a darkly lit area.
            Texture2D noise = InfernumTextureRegistry.CultistRayMap.Value;
            if (shieldColor.ToVector4().Length() > 0.02f)
                spriteBatch.Draw(noise, drawPosition, null, Color.White * opacity, 0, noise.Size() / 2f, scale * 2f, 0, 0);

            spriteBatch.ExitShaderRegion();
        }
        #endregion Draw Effects

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Reset the crystal shader. This is necessary since the vanilla values are only stored once.
            if (Main.netMode != NetmodeID.Server)
                Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(1f, 0f, 0.75f);

            // Just die as usual if the Profaned Guardian is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.ai[0] == (int)GuardiansAttackType.CommanderDeathAnimation)
                return true;

            npc.ai[0] = (int)GuardiansAttackType.CommanderDeathAnimation;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Get rid of the silly hands.
            int handID = ModContent.NPCType<EtherealHand>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == handID && Main.npc[i].active)
                {
                    Main.npc[i].active = false;
                    Main.npc[i].netUpdate = true;
                }
            }

            DespawnTransitionProjectiles();

            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Stay away from those energy fields! Being too close to them will hurt you!";
            yield return n => "Going in a tight circular pattern helps with the attacker guardian's spears!";
        }
        #endregion Tips
    }
}
