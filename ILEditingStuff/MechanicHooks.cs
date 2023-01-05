using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.UI.DraedonSummoning;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.GlobalInstances.Players;
using InfernumMode.Subworlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class DrawDraedonSelectionUIWithAthena : IHookEdit
    {
        public static ExoMech? PrimaryMechToSummon
        {
            get;
            set;
        } = null;

        public static ExoMech? DestroyerTypeToSummon
        {
            get;
            set;
        } = null;

        internal static void DrawSelectionUI(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.EmitDelegate<Action>(DrawWrapper);
            cursor.Emit(OpCodes.Ret);
        }

        public static void DrawWrapper()
        {
            Vector2 drawAreaVerticalOffset = Vector2.UnitY * 105f;
            Vector2 baseDrawPosition = Main.LocalPlayer.Top + drawAreaVerticalOffset - Main.screenPosition;
            Vector2 destroyerIconDrawOffset = new(-78f, -124f);
            Vector2 primeIconDrawOffset = new(0f, -140f);
            Vector2 twinsIconDrawOffset = new(78f, -124f);

            if (InfernumMode.CanUseCustomAIs)
            {
                bool hoveringOverAnyIcon = false;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 iconDrawOffset = new(MathHelper.Lerp(-92f, 92f, i / 2f), -145f);
                    hoveringOverAnyIcon |= HandleInteractionWithButton(baseDrawPosition + iconDrawOffset, i + 1, PrimaryMechToSummon == null);
                }

                // Reset the selections if the player clicks on something other than the icons.
                if (!hoveringOverAnyIcon && Main.mouseLeft && Main.mouseLeftRelease)
                    PrimaryMechToSummon = DestroyerTypeToSummon = null;
                if (!hoveringOverAnyIcon)
                    ExoMechSelectionUI.HoverSoundMechType = null;

                var font = FontAssets.MouseText.Value;
                string pickTwoText = "Pick two. The first mech will be fought alone. Once sufficiently damaged, the second mech will be summoned and the two will fight together.";
                Vector2 pickToDrawPosition = baseDrawPosition - Vector2.UnitY * 250f;
                foreach (string line in Utils.WordwrapString(pickTwoText, font, 600, 10, out _))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    Vector2 textArea = font.MeasureString(line);
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, line, pickToDrawPosition - Vector2.UnitX * textArea * 0.5f, Draedon.TextColorEdgy, 0f, textArea * new Vector2(0f, 0.5f), Vector2.One);
                    pickToDrawPosition.Y += 50f;
                }
                return;
            }

            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + destroyerIconDrawOffset, ExoMech.Destroyer);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + primeIconDrawOffset, ExoMech.Prime);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + twinsIconDrawOffset, ExoMech.Twins);
        }

        public static bool HandleInteractionWithButton(Vector2 drawPosition, int exoMech, bool selectingPrimaryMech)
        {
            float iconScale;
            string description;
            Texture2D iconMechTexture;
            SoundStyle hoverSound;

            switch (exoMech)
            {
                case 1:
                    iconScale = ExoMechSelectionUI.DestroyerIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_THanos").Value;
                    description = "Thanatos, a serpentine terror with impervious armor and innumerable laser turrets.";
                    hoverSound = ExoMechSelectionUI.ThanatosHoverSound;
                    break;
                case 2:
                    iconScale = ExoMechSelectionUI.PrimeIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_Ares").Value;
                    description = "Ares, a heavyweight, diabolical monstrosity with four Exo superweapons.";
                    hoverSound = ExoMechSelectionUI.AresHoverSound;
                    break;
                case 3:
                default:
                    iconScale = ExoMechSelectionUI.TwinsIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/HeadIcon_ArtemisApollo").Value;
                    description = "Artemis and Apollo, a pair of extremely agile destroyers with unstable energy reserves.";
                    hoverSound = ExoMechSelectionUI.TwinsHoverSound;
                    break;
            }

            // Check for mouse collision/clicks.
            Rectangle clickArea = Utils.CenteredRectangle(drawPosition, iconMechTexture.Size() * iconScale * 0.9f);

            // Check if the mouse is hovering over the contact button area.
            bool alreadySelected = (int)(PrimaryMechToSummon ?? (ExoMech)999) == exoMech || (int)(DestroyerTypeToSummon ?? (ExoMech)999) == exoMech;
            bool hoveringOverIcon = ExoMechSelectionUI.MouseScreenArea.Intersects(clickArea);
            if (hoveringOverIcon)
            {
                // If so, cause the button to inflate a little bit.
                iconScale = MathHelper.Clamp(iconScale + 0.0375f, 1f, 1.35f);

                // Play the hover sound.
                if (ExoMechSelectionUI.HoverSoundMechType != (ExoMech)exoMech)
                {
                    ExoMechSelectionUI.HoverSoundMechType = (ExoMech)exoMech;
                    SoundEngine.PlaySound(hoverSound with { Volume = 1.5f });
                }

                // Make the selection known if a click is done and the icon isn't already in use.
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (selectingPrimaryMech)
                        PrimaryMechToSummon = alreadySelected ? null : (ExoMech)exoMech;
                    else
                        DestroyerTypeToSummon = alreadySelected ? null : (ExoMech)exoMech;

                    int draedon = NPC.FindFirstNPC(ModContent.NPCType<Draedon>());
                    if (draedon != -1 && PrimaryMechToSummon.HasValue && DestroyerTypeToSummon.HasValue)
                    {
                        Main.npc[draedon].ai[0] = Draedon.ExoMechChooseDelay + 8f;
                        Main.npc[draedon].netSpam = 0;
                        Main.npc[draedon].netUpdate = true;

                        DraedonBehaviorOverride.SummonExoMech(Main.player[Main.npc[draedon].target]);
                        PrimaryMechToSummon = DestroyerTypeToSummon = null;
                    }
                }
                Main.blockMouse = Main.LocalPlayer.mouseInterface = true;
            }

            // Otherwise, if not hovering and not selected, cause the button to deflate back to its normal size.
            else if (!alreadySelected)
                iconScale = MathHelper.Clamp(iconScale - 0.05f, 1f, 1.2f);

            // Draw the icon with the new scale.
            Color iconColor = alreadySelected ? Color.Black * 0.8f : Color.White;
            if (alreadySelected)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 drawOffset = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 4f) * iconScale * 2f;
                    Main.spriteBatch.Draw(iconMechTexture, drawPosition + drawOffset, null, Color.Red with { A = 80 }, 0f, iconMechTexture.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(iconMechTexture, drawPosition, null, iconColor, 0f, iconMechTexture.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

            // Draw the descrption if hovering over the icon.
            if (hoveringOverIcon)
            {
                drawPosition.X -= FontAssets.MouseText.Value.MeasureString(description).X * 0.5f;
                drawPosition.Y += 36f;
                Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, description, drawPosition.X, drawPosition.Y, ExoMechSelectionUI.HoverTextColor, Color.Black, Vector2.Zero, 1f);
            }

            // And update to reflect the new scale.
            switch (exoMech)
            {
                case 1:
                    ExoMechSelectionUI.DestroyerIconScale = iconScale;
                    break;
                case 2:
                    ExoMechSelectionUI.PrimeIconScale = iconScale;
                    break;
                case 3:
                    ExoMechSelectionUI.TwinsIconScale = iconScale;
                    break;
            }
            return hoveringOverIcon;
        }

        public void Load() => ExoMechSelectionUIDraw += DrawSelectionUI;

        public void Unload() => ExoMechSelectionUIDraw -= DrawSelectionUI;
    }

    public class NerfAdrenalineHook : IHookEdit
    {
        internal static void NerfAdrenalineRates(ILContext context)
        {
            ILCursor c = new(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<CalamityPlayer>("adrenaline")))
                return;
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchLdloc(out _)))
                return;

            // This mechanic is ridiculous.
            c.EmitDelegate<Func<float>>(() => InfernumMode.CanUseCustomAIs && !Main.LocalPlayer.Calamity().adrenalineModeActive ? BalancingChangesManager.AdrenalineChargeTimeFactor : 1f);
            c.Emit(OpCodes.Div);
        }

        public void Load() => UpdateRippers += NerfAdrenalineRates;
        public void Unload() => UpdateRippers -= NerfAdrenalineRates;
    }

    public class RenameGreatSandSharkHook : IHookEdit
    {
        internal string RenameGSS(On.Terraria.Lang.orig_GetNPCNameValue orig, int netID)
        {
            if (netID == ModContent.NPCType<GreatSandShark>() && InfernumMode.CanUseCustomAIs)
                return GreatSandSharkBehaviorOverride.NewName;

            return orig(netID);
        }

        public void Load() => On.Terraria.Lang.GetNPCNameValue += RenameGSS;

        public void Unload() => On.Terraria.Lang.GetNPCNameValue -= RenameGSS;
    }

    public class MakeHooksInteractWithPlatforms : IHookEdit
    {
        internal static NPC[] GetPlatforms(Projectile projectile)
        {
            return Main.npc.Take(Main.maxNPCs).Where(n => n.active && (n.type == ModContent.NPCType<GolemArenaPlatform>() ||
                n.type == ModContent.NPCType<ProvArenaPlatform>())).
                OrderBy(n => projectile.Distance(n.Center)).ToArray();
        }

        internal static bool PlatformRequirement(Projectile projectile)
        {
            Vector2 adjustedCenter = projectile.Center - new Vector2(5f);
            NPC[] attachedPlatforms = GetPlatforms(projectile).Where(p =>
            {
                Rectangle platformHitbox = p.Hitbox;
                return Utils.CenteredRectangle(adjustedCenter, Vector2.One * 32f).Intersects(platformHitbox);
            }).ToArray();
            return attachedPlatforms.Length >= 1;
        }

        internal static void HandleAttachment(Projectile projectile)
        {
            if (PlatformRequirement(projectile) && projectile.ai[0] != 2f)
                projectile.Center = Vector2.Lerp(projectile.Center, GetPlatforms(projectile)[0].Center, 0.3f);
        }

        internal static void AdjustHitPlatformCoords(Projectile projectile, ref int x, ref int y)
        {
            if (PlatformRequirement(projectile))
            {
                var platform = GetPlatforms(projectile)[0];
                Vector2 hitPoint = new(projectile.Center.X, platform.Center.Y);
                x = (int)(hitPoint.X / 16f);
                y = (int)(hitPoint.Y / 16f);
            }
        }

        public delegate void PlatformCoordsDelegate(Projectile projectile, ref int x, ref int y);

        internal static void AdjustPlatformCollisionChecks(ILContext context)
        {
            ILCursor c = new(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<WorldGen>("GetTileVisualHitbox")))
                return;
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<Projectile>("damage")))
                return;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(HandleAttachment);

            // Go to the last instance of AI_007_GrapplingHooks_CanTileBeLatchedOnTo.
            while (c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Projectile>("AI_007_GrapplingHooks_CanTileBeLatchedOnTo"))) { }

            // Move to the instance of local loading that determines if the hook should return and AND it ensure that the platform requirement is met.
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(out _)))
                return;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Projectile, bool>>(p => !PlatformRequirement(p));
            c.Emit(OpCodes.And);

            // Make the hook and player move with the platform if attached.
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Projectile>>(projectile =>
            {
                if (PlatformRequirement(projectile))
                {
                    Player owner = Main.player[projectile.owner];
                    var platform = GetPlatforms(projectile)[0];
                    Vector2 offset = platform.velocity * 0.5f;

                    if (Collision.CanHit(owner.position, owner.width, owner.height, owner.position + offset * 1.5f, owner.width, owner.height))
                    {
                        owner.position += offset;
                        projectile.position += offset;
                    }
                }
            });

            // Go back and get the label inside the "Has a tile been hit? Collide with it." logic that will be jumped to from outside based on the moving platforms.
            // Also get the local indices for the hit tile's X/Y coordinates. They will be overrided by the result of the platform collision as necessary.
            c.Goto(0);
            for (int i = 0; i < 2; i++)
            {
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld<Player>("grapCount")))
                    return;
            }

            // Get the label to jump to.
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchInitobj<Tile>()))
                return;

            if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdsflda<Main>("tile")))
                return;

            // Delete the stupid fucking tile null check since it fucks up the search process and does literally nothing now.
            int start = c.Index;
            if (!c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Tilemap>("set_Item")))
                return;
            int end = c.Index;
            c.Goto(start);
            c.RemoveRange(end - start);

            int placeToPutPlatformCoordAdjustments = c.Index;

            // Find the local coordinates based on a Main.tile[x, y] check right above.
            int xLocalIndex = 0;
            int yLocalIndex = 0;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out xLocalIndex)))
                return;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out yLocalIndex)))
                return;

            int afterLocalIndicesIndex = c.Index;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Player>("IsBlacklistedForGrappling")))
                return;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld<Main>("player")))
                return;
            var tileHitLogic = c.DefineLabel();
            c.MarkLabel(tileHitLogic);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, xLocalIndex);
            c.Emit(OpCodes.Ldloca, yLocalIndex);
            c.EmitDelegate<PlatformCoordsDelegate>(AdjustHitPlatformCoords);

            // Finally, leave the conditional hell and hook to a default(Vector2) outside of the loop to create the jump/XY change.
            c.Index = afterLocalIndicesIndex;
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchInitobj<Vector2>()))
                return;
            if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdloca(out _)))
                return;

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(PlatformRequirement);
            c.Emit(OpCodes.Brtrue, tileHitLogic);

            c.Index = placeToPutPlatformCoordAdjustments;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, xLocalIndex);
            c.Emit(OpCodes.Ldloca, yLocalIndex);
            c.EmitDelegate<PlatformCoordsDelegate>(AdjustHitPlatformCoords);
        }

        public void Load() => IL.Terraria.Projectile.AI_007_GrapplingHooks += AdjustPlatformCollisionChecks;
        public void Unload() => IL.Terraria.Projectile.AI_007_GrapplingHooks -= AdjustPlatformCollisionChecks;
    }

    public class DrawBlackEffectHook : IHookEdit
    {
        public static List<int> DrawCacheBeforeBlack = new(Main.maxProjectiles);
        public static List<int> DrawCacheProjsOverSignusBlackening = new(Main.maxProjectiles);
        public static List<int> DrawCacheAdditiveLighting = new(Main.maxProjectiles);
        internal static void DrawBlackout(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Main>("DrawBackgroundBlackFill")))
                return;

            cursor.EmitDelegate<Action>(() =>
            {
                for (int i = 0; i < DrawCacheBeforeBlack.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheBeforeBlack[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheBeforeBlack[i]].active = false;
                    }
                }
                DrawCacheBeforeBlack.Clear();
            });

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<MoonlordDeathDrama>("DrawWhite")))
                return;

            cursor.EmitDelegate<Action>(() =>
            {
                float fadeToBlack = 0f;
                if (CalamityGlobalNPC.signus != -1 && Main.npc[CalamityGlobalNPC.signus].active)
                    fadeToBlack = Main.npc[CalamityGlobalNPC.signus].Infernum().ExtraAI[9];
                if (InfernumMode.BlackFade > 0f)
                    fadeToBlack = InfernumMode.BlackFade;

                if (fadeToBlack > 0f)
                {
                    Color color = Color.Black * fadeToBlack;
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < DrawCacheProjsOverSignusBlackening.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheProjsOverSignusBlackening[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheProjsOverSignusBlackening[i]].active = false;
                    }
                }
                DrawCacheProjsOverSignusBlackening.Clear();

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                for (int i = 0; i < DrawCacheAdditiveLighting.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheAdditiveLighting[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheAdditiveLighting[i]].active = false;
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawCacheAdditiveLighting.Clear();

                // Draw the madness effect.
                if (InfernumMode.CanUseCustomAIs && NPC.AnyNPCs(NPCID.Deerclops))
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                    InfernumEffectsRegistry.MadnessScreenShader.GetShader().UseSecondaryColor(Color.DarkViolet with { A = 20 });
                    InfernumEffectsRegistry.MadnessScreenShader.Apply();
                    Main.spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/Misc/noise").Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), Color.White);
                    Main.spriteBatch.ExitShaderRegion();
                }
            });
        }

        public void Load()
        {
            DrawCacheProjsOverSignusBlackening = new List<int>();
            DrawCacheAdditiveLighting = new List<int>();
            IL.Terraria.Main.DoDraw += DrawBlackout;
        }

        public void Unload()
        {
            DrawCacheProjsOverSignusBlackening = DrawCacheAdditiveLighting = null;
            IL.Terraria.Main.DoDraw -= DrawBlackout;
        }
    }

    public class DisableMoonLordBuildingHook : IHookEdit
    {
        internal static void DisableMoonLordBuilding(ILContext instructionContext)
        {
            var c = new ILCursor(instructionContext);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(ItemID.SuperAbsorbantSponge)))
                return;

            c.EmitDelegate(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs)
                    Main.LocalPlayer.noBuilding = true;
            });
        }

        public void Load() => IL.Terraria.Player.ItemCheck += DisableMoonLordBuilding;

        public void Unload() => IL.Terraria.Player.ItemCheck -= DisableMoonLordBuilding;
    }

    public class ChangeHowMinibossesSpawnInDD2EventHook : IHookEdit
    {
        internal static int GiveDD2MinibossesPointPriority(On.Terraria.GameContent.Events.DD2Event.orig_GetMonsterPointsWorth orig, int slainMonsterID)
        {
            if (OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID) && minibossID != NPCID.DD2Betsy && InfernumMode.CanUseCustomAIs)
                return slainMonsterID == minibossID ? 99999 : 0;

            return orig(slainMonsterID);
        }

        public void Load() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth += GiveDD2MinibossesPointPriority;

        public void Unload() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth -= GiveDD2MinibossesPointPriority;
    }

    public class AllowSandstormInColosseumHook : IHookEdit
    {
        internal static bool LetSandParticlesAppear(On.Terraria.GameContent.Events.Sandstorm.orig_ShouldSandstormDustPersist orig)
        {
            return orig() || (SubworldSystem.IsActive<LostColosseum>() && Sandstorm.Happening);
        }

        public void Load() => On.Terraria.GameContent.Events.Sandstorm.ShouldSandstormDustPersist += LetSandParticlesAppear;

        public void Unload() => On.Terraria.GameContent.Events.Sandstorm.ShouldSandstormDustPersist -= LetSandParticlesAppear;
    }

    public class DrawVoidBackgroundDuringMLFightHook : IHookEdit
    {
        public static void PrepareShaderForBG(On.Terraria.Main.orig_DrawSurfaceBG orig, Main self)
        {
            int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
            bool useShader = InfernumMode.CanUseCustomAIs && moonLordIndex >= 0 && moonLordIndex < Main.maxNPCs && !Main.gameMenu;

            FixWeirdDivisionBy0Bug();
            
            try
            {
                orig(self);
            }
            catch (IndexOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            if (useShader)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                
                Vector2 scale = new Vector2(Main.screenWidth, Main.screenHeight) / TextureAssets.MagicPixel.Value.Size() * Main.GameViewMatrix.Zoom * 2f;
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.Black, 0f, Vector2.Zero, scale * 1.5f, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin();
            }
        }

        // I don't know why this is a problem. I'd assume it's some quirk with the Lost Colosseum bg code, but it's hard to know for sure given how
        // utterly hideous that stuff is on vanilla's end.
        public static void FixWeirdDivisionBy0Bug()
        {
            for (int i = 0; i < Main.desertBG.Length; i++)
            {
                if (Main.desertBG[i] <= -1)
                    Main.desertBG[i] = 207;
                if (Main.backgroundWidth[Main.desertBG[i]] <= 0)
                    Main.backgroundWidth[Main.desertBG[i]] = 1024;
            }
        }

        public void Load() => On.Terraria.Main.DrawSurfaceBG += PrepareShaderForBG;

        public void Unload() => On.Terraria.Main.DrawSurfaceBG -= PrepareShaderForBG;
    }

    public class DrawCherishedSealocketHook : IHookEdit
    {
        private void DrawForcefields(On.Terraria.Main.orig_DrawInfernoRings orig, Main self)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active || Main.player[i].outOfRange || Main.player[i].dead)
                    continue;

                SealocketPlayer modPlayer = Main.player[i].GetModPlayer<SealocketPlayer>();
                modPlayer.ForcefieldOpacity = 1f;
                if (modPlayer.ForcefieldOpacity <= 0.01f || modPlayer.ForcefieldDissipationInterpolant >= 0.99f)
                    continue;

                float forcefieldOpacity = (1f - modPlayer.ForcefieldDissipationInterpolant) * modPlayer.ForcefieldOpacity;
                Vector2 forcefieldDrawPosition = Main.player[i].Center + Vector2.UnitY * Main.player[i].gfxOffY - Main.screenPosition;
                BereftVassal.DrawElectricShield(forcefieldOpacity, forcefieldDrawPosition, forcefieldOpacity, modPlayer.ForcefieldDissipationInterpolant * 1.5f + 1.3f);
            }
            orig(self);
        }

        public void Load() => On.Terraria.Main.DrawInfernoRings += DrawForcefields;

        public void Unload() => On.Terraria.Main.DrawInfernoRings -= DrawForcefields;
    }
}