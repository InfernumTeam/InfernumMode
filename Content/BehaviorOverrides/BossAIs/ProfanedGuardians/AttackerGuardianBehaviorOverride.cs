using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Common.Graphics.Primitives.PrimitiveTrailCopy;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        internal PrimitiveTrailCopy DashTelegraphDrawer;

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianDefender>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianHealer>()).ToInt();

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianCommander>();

        #region AI and Behaviors
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 228;
            npc.height = 186;
            npc.scale = 1f;
            npc.defense = 40;
            npc.DR_NERD(0.3f);
        }

        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (npc.localAI[1] == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)DefenderStartingHoverPosition.X + 20, (int)DefenderStartingHoverPosition.Y + 90, ModContent.NPCType<ProfanedGuardianDefender>());
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)HealerStartingHoverPosition.X + 20, (int)HealerStartingHoverPosition.Y + 90, ModContent.NPCType<ProfanedGuardianHealer>());
                }
                DoPhaseTransitionEffects(npc, 1);
                npc.localAI[1] = 1f;
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Despawn if no valid target exists.
            npc.timeLeft = 3600;
            Player target = Main.player[npc.target];
            if (((!target.active || target.dead) || target.Center.X < WorldSaveSystem.ProvidenceArena.X * 16))
            {
                npc.velocity.Y = Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
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
            {
                npc.Calamity().DR = 0.4f;
                npc.chaseable = true;
            }

            // Deal damage.
            npc.damage = npc.defDamage;

            // Reset fields.
            npc.Infernum().ExtraAI[DefenderShouldGlowIndex] = 0;

            bool giveInfFlight = true;
            // Wait before giving this at the start of the fight, to allow the camera time to pan back first.
            if ((GuardiansAttackType)npc.ai[0] == GuardiansAttackType.SpawnEffects || ((GuardiansAttackType)npc.ai[0] == GuardiansAttackType.FlappyBird && npc.ai[1] < 105f))
                giveInfFlight = false;

            // Give the player infinite flight time, and keep them in the bounds.
            if (giveInfFlight)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead)
                    {
                        if (player.WithinRange(npc.Center, 10000f))
                            player.DoInfiniteFlightCheck(Color.Orange);

                        if (player.WithinRange(new Vector2(WorldSaveSystem.ProvidenceDoorXPosition, (WorldSaveSystem.ProvidenceArena.Y + WorldSaveSystem.ProvidenceArena.Height * 0.5f) * 16f), 10000f))
                        {
                            if (player.Center.X > WorldSaveSystem.ProvidenceDoorXPosition)
                                player.Center = new(WorldSaveSystem.ProvidenceDoorXPosition, player.Center.Y);
                        }
                    }
                }
            }

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Reset opacities depending on whether they are being drawn or not.
            ref float smearOpacity = ref npc.Infernum().ExtraAI[CommanderSpearSmearOpacityIndex];
            if (npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex] == 1f)
                smearOpacity = Clamp(smearOpacity + 0.1f, 0f, 1f);
            else
                smearOpacity = Clamp(smearOpacity - 0.1f, 0f, 1f);

            npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex] = 0f;

            ref float fireBorderOpacity = ref npc.Infernum().ExtraAI[FireBorderInterpolantIndex];
            if (npc.Infernum().ExtraAI[FireBorderShouldDrawIndex] == 1f)
                fireBorderOpacity = Clamp(fireBorderOpacity + 0.1f, 0f, 1f);
            else
                fireBorderOpacity = Clamp(fireBorderOpacity - 0.1f, 0f, 1f);

            // Decrease the extra sky opacity;
            ref float skyOpacity = ref npc.Infernum().ExtraAI[GuardianSkyExtraIntensityIndex];
            skyOpacity = Clamp(skyOpacity - 0.01f, 0f, 1f);

            // Force the player into the area if the opacity is drawn.
            if (fireBorderOpacity > 0f && target.Center.Distance(npc.Center) > 1250f)
                target.Center = Vector2.Lerp(target.Center, npc.Center + npc.Center.DirectionTo(target.Center) * 1250f, 0.2f);

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

                case GuardiansAttackType.FireballBulletHell:
                    DoBehavior_FireballBulletHell(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.DefenderDeathAnimation:
                    DoBehavior_DefenderDeathAnimation(npc, target, ref attackTimer, npc);
                    break;

                case GuardiansAttackType.LargeGeyserAndCharge:
                    DoBehavior_LargeGeyserAndCharge(npc, target, ref attackTimer);
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

                case GuardiansAttackType.RiftFireCharges:
                    DoBehavior_RiftFireCharges(npc, target, ref attackTimer);
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
            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];

            // Slow to a screeching halt.
            npc.velocity *= 0.9f;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Despawn the spear if it is active.
            if ((DefenderShieldStatus)spearStatus != DefenderShieldStatus.Inactive || Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;

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
                    float intensity = Clamp(brightnessWidthFactor * 1.1f - i / 15f, 0f, 1f);
                    Vector2 lightPillarOrigin = new(TextureAssets.MagicPixel.Value.Width / 2f, TextureAssets.MagicPixel.Value.Height);
                    Vector2 lightPillarScale = new(Sqrt(intensity + i) * brightnessWidthFactor * 200f, 6f);
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
                    Vector2 drawOffset = (TwoPi * i / 24f).ToRotationVector2() * radius;
                    Color backimageColor = Color.Black;
                    backimageColor.A = (byte)Lerp(164f, 0f, npc.Opacity);
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backimageColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            if (TotalRemaininGuardians == 1 && npc.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex] == 1f)
                DrawDashTelegraph(npc);

            if (shouldDrawShield)
                DrawBackglowEffects(npc, spriteBatch, texture);

            //if (npc.Infernum().ExtraAI[CommanderFireAfterimagesIndex] == 1)
            //    PrepareFireAfterimages(npc, spriteBatch, direction);

            if ((GuardiansAttackType)npc.ai[0] > GuardiansAttackType.HealerDeathAnimation)
                DefenderGuardianBehaviorOverride.DrawBackglow(npc, spriteBatch, texture);

            spriteBatch.Draw(texture, drawPosition, npc.frame, Color.Lerp(npc.GetAlpha(lightColor), Color.Black * npc.Opacity, fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Black, fadeToBlack) * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);

            // Draw a defensive overlay over the commander if the healer is dead, to help identify him from the defender.
            if (TotalRemaininGuardians == 2 && (GuardiansAttackType)npc.ai[0] != GuardiansAttackType.DefenderDeathAnimation)
                DefenderGuardianBehaviorOverride.DrawDefenseOverlay(npc, spriteBatch, texture);

            // Draw an overlay.
            ref float glowAmount = ref npc.Infernum().ExtraAI[CommanderAngerGlowAmountIndex];
            if (glowAmount > 0f && (GuardiansAttackType)npc.ai[0] is GuardiansAttackType.HealerDeathAnimation)
                DrawAngerOverlay(npc, spriteBatch, texture, glowmask, direction, glowAmount);

            if (shouldDrawShield)
                DrawHealerShield(npc, spriteBatch, 3.5f, shieldOpacity);

            if (npc.Infernum().ExtraAI[FireBorderInterpolantIndex] > 0f)
                DrawFireBorder(npc, spriteBatch);

            if (WorldSaveSystem.HasProvidenceDoorShattered)
                DrawTempleBorder(npc, spriteBatch);
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
                float arrowRotation = direction.ToRotation() + PiOver2;
                float sineValue = (1f + Sin(Main.GlobalTimeWrappedHourly * 10.5f - i)) / 2f;
                float finalOpacity = CalamityUtils.SineInOutEasing(sineValue, 1);
                Main.spriteBatch.Draw(arrowTexture, drawPosition, null, drawColor * finalOpacity, arrowRotation, arrowOrigin, 0.75f, SpriteEffects.None, 0f);
                drawPosition += direction * 75f;
            }
        }

        public static void DrawBackglowEffects(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture)
        {
            ref float commanderHasAlreadyDoneBoom = ref npc.Infernum().ExtraAI[CommanderHasSpawnedBlenderAlreadyIndex];
            ref float opacity = ref npc.Infernum().ExtraAI[CommanderBlenderBackglowOpacityIndex];
            ref float spawnedLasers = ref npc.Infernum().ExtraAI[1];
            bool initialWaitIsOver = commanderHasAlreadyDoneBoom == 0 && spawnedLasers == 1;

            if (commanderHasAlreadyDoneBoom == 1 || initialWaitIsOver)
            {
                opacity = Clamp(opacity + 0.05f, 0f, 1f);
                // Glow effect.
                Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Vector2 drawPosition = npc.Center - Main.screenPosition;
                Color drawColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.5f);
                drawColor.A = 0;
                Vector2 origin = glow.Size() * 0.5f;
                spriteBatch.Draw(glow, drawPosition, null, drawColor, 0f, origin, 3.5f, SpriteEffects.None, 0f);

                // Draw a glow effect.
                Texture2D glowBloom = ModContent.Request<Texture2D>("CalamityMod/UI/ModeIndicator/BloomFlare").Value;
                Vector2 glowPosition = npc.Center - Main.screenPosition;
                Color glowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.3f);
                glowColor.A = 0;
                float glowRotation = Main.GlobalTimeWrappedHourly * 3;
                float scaleInterpolant = (1f + Sin(Main.GlobalTimeWrappedHourly * 5f)) / 2f;
                float scale = Lerp(3.6f, 4.1f, scaleInterpolant);
                Main.spriteBatch.Draw(glowBloom, glowPosition, null, glowColor * opacity, glowRotation, glowBloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowBloom, glowPosition, null, glowColor * opacity, glowRotation * -1, glowBloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None, 0f);

                // Backglow
                int backglowAmount = 12;
                float sine = (1f + Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
                float backglowDistance = Lerp(4.5f, 6.5f, sine);
                for (int i = 0; i < backglowAmount; i++)
                {
                    Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * backglowDistance;
                    Color backglowColor = WayfinderSymbol.Colors[1];
                    backglowColor.A = 0;
                    SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
                }
            }
        }

        public static void DrawAngerOverlay(NPC npc, SpriteBatch spriteBatch, Texture2D texture, Texture2D glowmask, SpriteEffects direction, float glowAmount)
        {
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.OrangeRed) with { A = 0 } * glowAmount * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, npc.Center - Main.screenPosition, npc.frame, WayfinderSymbol.Colors[0] with { A = 0 } * glowAmount * npc.Opacity, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
        }

        public static void DrawHealerShield(NPC npc, SpriteBatch spriteBatch, float scaleFactor, float opacity)
        {
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            float scale = Lerp(0.15f, 0.155f, Sin(Main.GlobalTimeWrappedHourly * 0.5f) * 0.5f + 0.5f) * scaleFactor;
            float noiseScale = Lerp(0.4f, 0.8f, Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // Prepare the forcefield opacity.
            float baseShieldOpacity = 0.9f + 0.1f * Sin(Main.GlobalTimeWrappedHourly * 2f);
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

        public static void DrawFireBorder(NPC npc, SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            // Get variables.
            List<VertexPosition2DColor> vertices = new();
            float totalPoints = 200;
            float width = 300f;
            float radius = 1300f;
            Color color = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.75f);
            float distanceFromCenter = radius - Main.player[npc.target].Center.Distance(npc.Center);
            float alpha = Clamp(1f - distanceFromCenter / (radius * 1.5f), 0f, 1f);
            color *= alpha * npc.Infernum().ExtraAI[FireBorderInterpolantIndex];

            for (int i = 0; i <= totalPoints; i++)
            {
                float interpolant = i / totalPoints;
                Vector2 position = npc.Center - Main.screenPosition + (i * TwoPi / totalPoints).ToRotationVector2() * radius;
                Vector2 position2 = npc.Center - Main.screenPosition + (i * TwoPi / totalPoints).ToRotationVector2() * (radius + width);

                Vector2 textureCoords = new(interpolant, 0f);
                Vector2 textureCoords2 = new(interpolant, 1f);

                vertices.Add(new VertexPosition2DColor(position, color, textureCoords));
                vertices.Add(new VertexPosition2DColor(position2, color, textureCoords2));
            }

            CalamityUtils.CalculatePerspectiveMatricies(out var view, out var projection);
            InfernumEffectsRegistry.AreaBorderVertexShader.UseOpacity(alpha * npc.Infernum().ExtraAI[FireBorderInterpolantIndex]);
            InfernumEffectsRegistry.AreaBorderVertexShader.UseColor(WayfinderSymbol.Colors[2]);
            InfernumEffectsRegistry.AreaBorderVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["noiseSpeed"].SetValue(new Vector2(0.1f, 0.1f));
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["timeFactor"].SetValue(2f);
            InfernumEffectsRegistry.AreaBorderVertexShader.Apply();

            Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/SolidEdgeGradient", AssetRequestMode.ImmediateLoad).Value;
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
            spriteBatch.ExitShaderRegion();
        }

        public static void DrawTempleBorder(NPC npc, SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            // Get variables.
            List<VertexPosition2DColor> vertices = new();
            float totalPoints = 50;
            float width = 300f;
            Color color = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.75f);
            Vector2 startPos = new(WorldSaveSystem.ProvidenceDoorXPosition, CenterOfGarden.Y - 1200f);
            Vector2 endPos = startPos + new Vector2(0f, 2670f);
            float fadeDistance = 500f;
            float distanceFromBorder = new Vector2(Main.player[npc.target].Center.X, startPos.Y).Distance(startPos);
            float alpha = Clamp(1f - distanceFromBorder / (fadeDistance * 1.5f), 0f, 1f);
            color *= alpha;

            for (int i = 0; i < totalPoints; i++)
            {
                float interpolant = i / totalPoints;
                Vector2 position = Vector2.Lerp(startPos, endPos, interpolant) - Main.screenPosition;
                Vector2 position2 = (Vector2.Lerp(startPos, endPos, interpolant) - Main.screenPosition);
                Vector2 textureCoords = new(interpolant, 0f);
                Vector2 textureCoords2 = new(interpolant, 1f);
                vertices.Add(new VertexPosition2DColor(position, color, textureCoords));
                vertices.Add(new VertexPosition2DColor(position2 + new Vector2(width, 0f), color, textureCoords2));
            }

            CalamityUtils.CalculatePerspectiveMatricies(out var view, out var projection);
            InfernumEffectsRegistry.AreaBorderVertexShader.UseOpacity(alpha);
            InfernumEffectsRegistry.AreaBorderVertexShader.UseColor(WayfinderSymbol.Colors[2]);
            InfernumEffectsRegistry.AreaBorderVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["noiseSpeed"].SetValue(new Vector2(0.1f, 0.1f));
            InfernumEffectsRegistry.AreaBorderVertexShader.Shader.Parameters["timeFactor"].SetValue(2f);
            InfernumEffectsRegistry.AreaBorderVertexShader.Apply();

            Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/SolidEdgeGradient", AssetRequestMode.ImmediateLoad).Value;
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
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
    }
}
