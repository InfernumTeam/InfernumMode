using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.DataStructures;
using CalamityMod.Events;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.Schematics;
using CalamityMod.World;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.DataStructures;
using InfernumMode.Common.UtilityMethods;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Signus;
using InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.ILEditingStuff
{
    internal sealed class NerfAdrenalineHook : ModSystem
    {
        #region Hooks
        public static MethodInfo? CalApplyRippersToDamageMethod = typeof(CalamityUtils).GetMethod("ApplyRippersToDamage", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalApplyRippersToDamageMethod(CalamityPlayer mp, bool trueMelee, ref float damageMult);
        public Hook? CalApplyRippersToDamage_Detour_Hook;

        public static MethodInfo? CalGetAdrenalineDamageMethod = typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags);
        public delegate float Orig_CalGetAdrenalineDamageMethod(CalamityPlayer mp);
        public Hook? CalGetAdrenalineDamage_Detour_Hook;

        public static MethodInfo? CalModifyHitNPCWithItemMethod = typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithItem", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalModifyHitNPCWithItemMethod(CalamityPlayer self, Item item, NPC target, ref NPC.HitModifiers modifiers);
        public Hook? CalModifyHitNPCWithItem_Detour_Hook;

        public static MethodInfo? CalModifyHitNPCWithProjMethod = typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithProj", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalModifyHitNPCWithProjMethod(CalamityPlayer self, Projectile proj, NPC target, ref NPC.HitModifiers modifiers);
        public Hook? CalModifyHitNPCWithProj_Detour_Hook;

        public static MethodInfo? UpdateRippers = typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags);
        public static ILHook? NerfAdrenaline_IL_Hook;
        #endregion

        internal static bool ShouldGetRipperDamageModifiers
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            if (CalApplyRippersToDamageMethod != null)
            {
                CalApplyRippersToDamage_Detour_Hook = new(CalApplyRippersToDamageMethod, NerfAdrenalineHook.ApplyRippersToDamageDetour);
                CalApplyRippersToDamage_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalGetAdrenalineDamageMethod != null)
            {
                CalGetAdrenalineDamage_Detour_Hook = new(CalGetAdrenalineDamageMethod, NerfAdrenalineHook.NerfAdrenDamageMethod);
                CalGetAdrenalineDamage_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalModifyHitNPCWithItemMethod != null)
            {
                CalModifyHitNPCWithItem_Detour_Hook = new(CalModifyHitNPCWithItemMethod, NerfAdrenalineHook.ModifyHitNPCWithItemDetour);
                CalModifyHitNPCWithItem_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalModifyHitNPCWithProjMethod != null)
            {
                CalModifyHitNPCWithProj_Detour_Hook = new(CalModifyHitNPCWithProjMethod, NerfAdrenalineHook.ModifyHitNPCWithProjDetour);
                CalModifyHitNPCWithProj_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (UpdateRippers != null)
            {
                NerfAdrenaline_IL_Hook = new(UpdateRippers, NerfAdrenaline_IL);
                NerfAdrenaline_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }
        #region Methods
        public static void ApplyRippersToDamageDetour(Orig_CalApplyRippersToDamageMethod orig, CalamityPlayer mp, bool trueMelee, ref float damageMult)
        {
            if (!InfernumMode.CanUseCustomAIs || ShouldGetRipperDamageModifiers)
            {
                orig(mp, trueMelee, ref damageMult);
                return;
            }
        }

        public static void ModifyHitNPCWithItemDetour(Orig_CalModifyHitNPCWithItemMethod orig, CalamityPlayer self, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!InfernumMode.CanUseCustomAIs)
            {
                orig(self, item, target, ref modifiers);
                return;
            }

            ShouldGetRipperDamageModifiers = false;
            orig(self, item, target, ref modifiers);
            ShouldGetRipperDamageModifiers = true;
            float damageMult = 0f;
            CalamityUtils.ApplyRippersToDamage(self, item.IsTrueMelee(), ref damageMult);
            modifiers.SourceDamage += damageMult;
        }

        public static void ModifyHitNPCWithProjDetour(Orig_CalModifyHitNPCWithProjMethod orig, CalamityPlayer self, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            try
            {
                if (!InfernumMode.CanUseCustomAIs)
                {
                    orig(self, proj, target, ref modifiers);
                    return;
                }

                ShouldGetRipperDamageModifiers = false;
                orig(self, proj, target, ref modifiers);
            }
            catch (Exception)
            {

            }
            ShouldGetRipperDamageModifiers = true;
            float damageMult = 0f;
            CalamityUtils.ApplyRippersToDamage(self, proj.IsTrueMelee(), ref damageMult);
            modifiers.SourceDamage += damageMult;
        }

        public static float NerfAdrenDamageMethod(Orig_CalGetAdrenalineDamageMethod orig, CalamityPlayer mp)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return orig(mp);

            float adrenalineBoost = BalancingChangesManager.AdrenalineDamageBoost;

            if (mp.adrenalineBoostOne)
                adrenalineBoost += BalancingChangesManager.AdrenalineDamagePerBooster;
            if (mp.adrenalineBoostTwo)
                adrenalineBoost += BalancingChangesManager.AdrenalineDamagePerBooster;
            if (mp.adrenalineBoostThree)
                adrenalineBoost += BalancingChangesManager.AdrenalineDamagePerBooster;

            return adrenalineBoost;
        }

        public static void NerfAdrenaline_IL(ILContext context)
        {
            ILCursor c = new(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<CalamityPlayer>("adrenaline")))
                return;
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchLdloc(out _)))
                return;

            // This mechanic is ridiculous.
            c.EmitDelegate(() =>
            {
                return InfernumMode.CanUseCustomAIs && !Main.LocalPlayer.Calamity().adrenalineModeActive ? BalancingChangesManager.AdrenalineChargeTimeFactor : 1f;
            });
            c.Emit(OpCodes.Div);
        }
        #endregion
    }

    internal sealed class RenameGreatSandSharkHook : ModSystem
    {
        public override void Load() => On_Lang.GetNPCNameValue += RenameGSS;
        public static string RenameGSS(On_Lang.orig_GetNPCNameValue orig, int netID)
        {
            if (netID == ModContent.NPCType<GreatSandShark>() && InfernumMode.CanUseCustomAIs)
                return GreatSandSharkBehaviorOverride.NewName.Value;

            return orig(netID);
        }
    }

    internal sealed class MakeHooksInteractWithPlatforms : ModSystem
    {
        #region Utils
        public static NPC[] GetPlatforms(Projectile projectile)
        {
            return [.. Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.type == ModContent.NPCType<GolemArenaPlatform>()).OrderBy(n => projectile.Distance(n.Center))];
        }

        public static bool PlatformRequirement(Projectile projectile)
        {
            // I don't know what the problem is. I'm sorry.
            if (Main.netMode != NetmodeID.SinglePlayer)
                return false;

            Vector2 adjustedCenter = projectile.Center - new Vector2(5f);
            NPC[] attachedPlatforms = GetPlatforms(projectile).Where(p =>
            {
                Rectangle platformHitbox = p.Hitbox;
                return Utils.CenteredRectangle(adjustedCenter, Vector2.One * 32f).Intersects(platformHitbox);
            }).ToArray();
            return attachedPlatforms.Length >= 1;
        }

        public static void HandleAttachment(Projectile projectile)
        {
            if (PlatformRequirement(projectile) && projectile.ai[0] != 2f)
                projectile.Center = Vector2.Lerp(projectile.Center, GetPlatforms(projectile)[0].Center, 0.3f);
        }

        public static void AdjustHitPlatformCoords(Projectile projectile, ref int x, ref int y)
        {
            // I don't know what the problem is. I'm sorry.
            if (Main.netMode != NetmodeID.SinglePlayer)
                return;

            if (PlatformRequirement(projectile))
            {
                var platform = GetPlatforms(projectile)[0];
                Vector2 hitPoint = new(projectile.Center.X, platform.Center.Y);
                x = (int)(hitPoint.X / 16f);
                y = (int)(hitPoint.Y / 16f);
            }
        }

        public delegate void PlatformCoordsDelegate(Projectile projectile, ref int x, ref int y);
        #endregion

        public override void OnModLoad() => IL_Projectile.AI_007_GrapplingHooks += MakeHooksInteractWithPlatforms_IL;

        public static void MakeHooksInteractWithPlatforms_IL(ILContext context)
        {
            ILCursor c = new(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<WorldGen>("GetTileVisualHitbox")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchCallOrCallvirt<WorldGen>(\"GetTileVisualHitbox\")");
                return;
            }
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<Projectile>("damage")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchStfld<Projectile>(\"damage\")");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(HandleAttachment);

            // Go to the last instance of AI_007_GrapplingHooks_CanTileBeLatchedOnTo.
            while (c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Projectile>("AI_007_GrapplingHooks_CanTileBeLatchedOnTo"))) { }

            // Move to the instance of local loading that determines if the hook should return and AND it ensure that the platform requirement is met.
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(out _)))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdloc(out _)");
                return;
            }

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
                {
                    InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdfld<Player>(\"grapCount\")");
                    return;
                }
            }

            // Get the label to jump to.
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchInitobj<Tile>()))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchInitobj<Tile>()");
                return;
            }

            if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdsflda<Main>("tile")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdsflda<Main>(\"tile\")");
                return;
            }

            // Delete the stupid fucking tile null check since it fucks up the search process and does literally nothing now.
            int start = c.Index;
            if (!c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Tilemap>("set_Item")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchCallOrCallvirt<Tilemap>(\"set_Item\")");
                return;
            }
            int end = c.Index;
            c.Goto(start);
            c.RemoveRange(end - start);

            int placeToPutPlatformCoordAdjustments = c.Index;

            // Find the local coordinates based on a Main.tile[x, y] check right above.
            int xLocalIndex = 0;
            int yLocalIndex = 0;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out xLocalIndex)))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdloc(out xLocalIndex)");
                return;
            }
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out yLocalIndex)))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdloc(out yLocalIndex)");
                return;
            }

            int afterLocalIndicesIndex = c.Index;
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Player>("IsBlacklistedForGrappling")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchCallOrCallvirt<Player>(\"IsBlacklistedForGrappling\")");
                return;
            }
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld<Main>("player")))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdsfld<Main>(\"player\")");
                return;
            }
            var tileHitLogic = c.DefineLabel();
            c.MarkLabel(tileHitLogic);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, xLocalIndex);
            c.Emit(OpCodes.Ldloca, yLocalIndex);
            c.EmitDelegate<PlatformCoordsDelegate>(AdjustHitPlatformCoords);

            // Finally, leave the conditional hell and hook to a default(Vector2) outside of the loop to create the jump/XY change.
            c.Index = afterLocalIndicesIndex;
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchInitobj<Vector2>()))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchInitobj<Vector2>()");
                return;
            }
            if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdloca(out _)))
            {
                InfernumMode.Instance.Logger.Error("MakeHooksInteractWithPlatforms ILEdit failed MatchLdloca(out _)");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(PlatformRequirement);
            c.Emit(OpCodes.Brtrue, tileHitLogic);

            c.Index = placeToPutPlatformCoordAdjustments;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, xLocalIndex);
            c.Emit(OpCodes.Ldloca, yLocalIndex);
            c.EmitDelegate<PlatformCoordsDelegate>(AdjustHitPlatformCoords);
        }
    }

    internal sealed class DisableMoonLordBuildingHook : ModSystem
    {
        public override void OnModLoad() => IL_Player.ItemCheck += DisableMoonLordBuilding_IL;

        public static void DisableMoonLordBuilding_IL(ILContext context)
        {
            var c = new ILCursor(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(ItemID.SuperAbsorbantSponge)))
                return;

            c.EmitDelegate(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs)
                    Main.LocalPlayer.noBuilding = true;
            });
        }
    }

    internal sealed class ChangeHowMinibossesSpawnInDD2EventHook : ModSystem
    {
        public override void Load() => On_DD2Event.GetMonsterPointsWorth += GiveDD2MinibossesPointPriority;

        private static int GiveDD2MinibossesPointPriority(On_DD2Event.orig_GetMonsterPointsWorth orig, int slainMonsterID)
        {
            if (OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID) && minibossID != NPCID.DD2Betsy && InfernumMode.CanUseCustomAIs)
                return slainMonsterID == minibossID ? 99999 : 0;

            return orig(slainMonsterID);
        }
    }

    internal sealed class AllowSandstormInColosseumHook : ModSystem
    {
        public override void Load() => On_Sandstorm.ShouldSandstormDustPersist += LetSandParticlesAppear;

        private static bool LetSandParticlesAppear(On_Sandstorm.orig_ShouldSandstormDustPersist orig)
        {
            return orig() || (SubworldSystem.IsActive<LostColosseum>() && Sandstorm.Happening);
        }

    }

    internal sealed class DrawVoidBackgroundDuringMLFightHook : ModSystem
    {
        public override void Load() => On_Main.DrawSurfaceBG += PrepareShaderForBG;

        public static void PrepareShaderForBG(On_Main.orig_DrawSurfaceBG orig, Main self)
        {
            int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
            bool useMoonLordShader = InfernumMode.CanUseCustomAIs && moonLordIndex >= 0 && moonLordIndex < Main.maxNPCs && !Main.gameMenu;

            FixWeirdDivisionBy0Bug();

            try
            {
                orig(self);
            }
            catch (IndexOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            if (useMoonLordShader)
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
    }

    internal sealed class DrawCherishedSealocketHook : ModSystem
    {
        public static ManagedRenderTarget PlayerForcefieldTarget
        {
            get;
            set;
        }

        public static ArmorShaderData ForcefieldShader
        {
            get;
            set;
        }

        public override void Load()
        {
            DyeFindingSystem.FindDyeEvent += FindSealocketItemDyeShader;

            On_Main.CheckMonoliths += PrepareSealocketTarget;
            On_Main.DrawInfernoRings += DrawForcefields;

            if (Main.netMode != NetmodeID.Server)
                Main.QueueMainThreadAction(() => PlayerForcefieldTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget));
        }

        #region Methods
        private void DrawForcefields(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            Main.LocalPlayer.Infernum_CalShadowHex().DrawAllHexes();

            if (PlayerForcefieldTarget is null)
            {
                // Ensure orig is called regardless.
                orig(self);
                return;
            }

            Referenced<float> sealocketForcefieldOpacity = Main.LocalPlayer.Infernum().GetRefValue<float>("SealocketForcefieldOpacity");
            Referenced<float> forcefieldDissipationInterpolant = Main.LocalPlayer.Infernum().GetRefValue<float>("SealocketForcefieldDissipationInterpolant");

            // Draw the render target, optionally with a dye shader.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer);

            float shieldScale = sealocketForcefieldOpacity.Value * 0.3f;
            Vector2 shieldSize = Vector2.One * shieldScale * 512f;
            Rectangle shaderArea = Utils.CenteredRectangle(PlayerForcefieldTarget.Target.Size(), shieldSize);

            if (sealocketForcefieldOpacity.Value >= 0.01f && forcefieldDissipationInterpolant.Value < 0.99f)
                ForcefieldShader?.Apply(null, new(PlayerForcefieldTarget.Target, Vector2.Zero, shaderArea, Color.White));
            Main.spriteBatch.Draw(PlayerForcefieldTarget.Target, Main.LocalPlayer.Center - Main.screenPosition, null, Color.White, 0f, PlayerForcefieldTarget.Target.Size() * 0.5f, 1f, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();

            orig(self);
        }

        private static void PrepareSealocketTarget(On_Main.orig_CheckMonoliths orig)
        {
            orig();

            if (Main.gameMenu)
                return;

            var device = Main.instance.GraphicsDevice;
            RenderTargetBinding[] bindings = device.GetRenderTargets();
            PlayerForcefieldTarget.SwapToRenderTarget();

            // Draw forcefields to the render target.
            Main.spriteBatch.Begin();
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.outOfRange || player.dead || player.ghost)
                    continue;

                DrawForcefield(player);
            }
            Main.spriteBatch.End();
            device.SetRenderTargets(bindings);
        }

        public static void DrawForcefield(Player player)
        {
            Referenced<float> sealocketForcefieldOpacity = player.Infernum().GetRefValue<float>("SealocketForcefieldOpacity");
            Referenced<float> forcefieldDissipationInterpolant = player.Infernum().GetRefValue<float>("SealocketForcefieldDissipationInterpolant");
            sealocketForcefieldOpacity.Value = 1f;

            // Draw the sealocket forcefield.
            Vector2 forcefieldDrawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + Vector2.UnitY * player.gfxOffY;
            if (sealocketForcefieldOpacity.Value >= 0.01f && forcefieldDissipationInterpolant.Value < 0.99f)
            {
                float forcefieldOpacity = (1f - forcefieldDissipationInterpolant.Value) * sealocketForcefieldOpacity.Value;
                BereftVassal.DrawElectricShield(forcefieldOpacity, forcefieldDrawPosition, forcefieldOpacity, forcefieldDissipationInterpolant.Value * 1.5f + 1.3f);
            }

            // Draw the Brimstone Crescent forcefield.
            if (player.Infernum().GetValue<float>("BrimstoneCrescentForcefieldStrengthInterpolant") > 0f)
            {
                float scale = Lerp(0.6f, 1.5f, 1f - player.Infernum().GetValue<float>("BrimstoneCrescentForcefieldStrengthInterpolant"));
                Color forcefieldColor = CalamityUtils.ColorSwap(Color.Lerp(Color.Red, Color.Yellow, 0.06f), Color.OrangeRed, 5f) * player.Infernum().GetValue<float>("BrimstoneCrescentForcefieldStrengthInterpolant");
                CultistBehaviorOverride.DrawForcefield(forcefieldDrawPosition, 1.35f, forcefieldColor, InfernumTextureRegistry.HarshNoise.Value, true, scale, fresnelScaleFactor: 0.68f, noiseScaleFactor: 0.5f);
            }
        }

        private void FindSealocketItemDyeShader(Item armorItem, Item dyeItem)
        {
            if (armorItem.type == ModContent.ItemType<CherishedSealocket>())
                ForcefieldShader = GameShaders.Armor.GetShaderFromItemId(dyeItem.type);
        }
        #endregion
    }

    internal sealed class LetAresHitPlayersHook : ModSystem
    {
        public static MethodInfo? AresBodyCanHitPlayer = typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AresBodyCanHitPlayer(AresBody self, Player target, ref int cooldownSlot);
        public static Hook? LetAresHitPlayers_Detour_Hook;
        public override void OnModLoad()
        {
            if (AresBodyCanHitPlayer != null)
            {
                LetAresHitPlayers_Detour_Hook = new(AresBodyCanHitPlayer, AresBodyCanHitPlayer_Detour);
                LetAresHitPlayers_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }
        public bool AresBodyCanHitPlayer_Detour(Orig_AresBodyCanHitPlayer orig, AresBody self, Player target, ref int cooldownSlot)
        {
            bool value = orig(self, target, ref cooldownSlot);
            if (InfernumMode.CanUseCustomAIs)
                return true;
            return value;
        }
    }

    internal sealed class DisableWaterEffectsInFightsHook : ModSystem
    {
        public override void Load() => IL_Player.Update += DisableWaterEffectsInFights;

        public static void DisableWaterEffectsInFights(ILContext context)
        {
            ILCursor c = new(context);

            c.GotoNext(MoveType.After, i => i.MatchCall<Collision>("WetCollision"));
            c.EmitDelegate(() =>
            {
                if (!InfernumMode.CanUseCustomAIs)
                    return true;

                bool specialNPC = NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>());
                if (!specialNPC)
                    return true;

                return false;
            });
            c.Emit(OpCodes.And);
        }
    }

    /*public sealed class MakeSulphSeaWaterEasierToSeeInHook : ModSystem
    {
        public static ILHook? MakeSulphSeaWaterEasierToSeeIn_IL_Hook;
        internal static int SulphurWaterIndex
        {
            get;
            set;
        }

        // WHY IS THIS SO LAGGY WHAT THE ACTUAL FUCK???
        public static bool CanUseHighQualityWater => false;

        public override void Load() => On_TileLightScanner.GetTileLight += MakeSulphSeaWaterBrighter;

        public override void OnModLoad()
        {
            if (Main.netMode != NetmodeID.Server)
                SulphurWaterIndex = ModContent.Find<ModWaterStyle>("CalamityMod/SulphuricWater").Slot;

            MakeSulphSeaWaterEasierToSeeIn_IL_Hook = new(ModifySulphuricWaterColor, MakeSulphSeaWaterEasierToSeeIn_IL);

        }

        public static void MakeSulphSeaWaterEasierToSeeIn_IL(ILContext context)
        {
            ILCursor c = new(context);
            c.EmitDelegate(() =>
            {
                if (!CanUseHighQualityWater)
                    SulphuricWaterSafeZoneSystem.NearbySafeTiles.Clear();
            });

            for (int i = 0; i < 4; i++)
            {
                c.GotoNext(MoveType.After, i => i.MatchLdcR4(0.4f));
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_R4, 0.15f);
            }
        }

        public static readonly Vector3 cleanColor = new(10, 109, 193);
        private void MakeSulphSeaWaterBrighter(Terraria.Graphics.Light.On_TileLightScanner.orig_GetTileLight orig, Terraria.Graphics.Light.TileLightScanner self, int x, int y, out Vector3 outputColor)
        {
            orig(self, x, y, out outputColor);


            Tile tile = Framing.GetTileSafely(x, y);
            if (tile.LiquidAmount <= 0 || tile.HasTile || Main.waterStyle != SulphurWaterIndex)
                return;

            if (tile.TileType != (ushort)ModContent.TileType<RustyChestTile>())
            {
                Vector3 idealColor = Color.LightSeaGreen.ToVector3();

                if (SulphuricWaterSafeZoneSystem.NearbySafeTiles.Count >= 1)
                {
                    Vector2 pos = new(x, y);
                    Point closestSafeZone = SulphuricWaterSafeZoneSystem.NearbySafeTiles.Keys.OrderBy(t => t.ToVector2().DistanceSQ(pos)).First();
                    float distanceToClosest = pos.Distance(closestSafeZone.ToVector2());
                    float acidicWaterInterpolant = Utils.GetLerpValue(12f, 20.5f, distanceToClosest + (1f - SulphuricWaterSafeZoneSystem.NearbySafeTiles[closestSafeZone]) * 21f, true);
                    idealColor = Vector3.Lerp(idealColor, cleanColor, 1f - acidicWaterInterpolant);
                }

                outputColor = Vector3.Lerp(outputColor, idealColor, 0.8f);
            }
        }
    }*/

    internal sealed class ChangeRuneOfKosCanUseHook : ModSystem
    {
        public static MethodInfo? RuneOfKosCanUseItem = typeof(MarkofProvidence).GetMethod("CanUseItem", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeRuneOfKosCanUse_IL_Hook;
        public override void OnModLoad()
        {
            if (RuneOfKosCanUseItem != null)
            {
                ChangeRuneOfKosCanUse_IL_Hook = new(RuneOfKosCanUseItem, ChangeRuneOfKosCanUse_IL);
                ChangeRuneOfKosCanUse_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeRuneOfKosCanUse_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((Player player) =>
            {
                bool correctBiome = player.ZoneSkyHeight || player.ZoneUnderworldHeight || player.ZoneDungeon;
                bool bossIsNotPresent = !NPC.AnyNPCs(ModContent.NPCType<StormWeaverHead>()) && !NPC.AnyNPCs(ModContent.NPCType<CeaselessVoid>()) && !NPC.AnyNPCs(ModContent.NPCType<Signus>());
                return correctBiome && (bossIsNotPresent || InfernumMode.CanUseCustomAIs) && !BossRushEvent.BossRushActive;
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ChangeRuneOfKosUseItemHook : ModSystem
    {
        public static MethodInfo? RuneOfKosUseItem = typeof(MarkofProvidence).GetMethod("UseItem", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeRuneOfKosUseItem_IL_Hook;
        public override void OnModLoad()
        {
            if (RuneOfKosUseItem != null)
            {
                ChangeRuneOfKosUseItem_IL_Hook = new(RuneOfKosUseItem, ChangeRuneOfKosUse_IL);
                ChangeRuneOfKosUseItem_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeRuneOfKosUse_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((Player player) =>
            {
                if (player.ZoneDungeon)
                {
                    // Anger the Ceaseless Void if it's around but not attacking.
                    if (CalamityGlobalNPC.voidBoss != -1)
                    {
                        NPC ceaselessVoid = Main.npc[CalamityGlobalNPC.voidBoss];
                        if (ceaselessVoid.ai[0] == (int)CeaselessVoidBehaviorOverride.CeaselessVoidAttackType.ChainedUp)
                        {
                            SoundEngine.PlaySound(MarkofProvidence.CVSound, player.Center);
                            CeaselessVoidBehaviorOverride.SelectNewAttack(ceaselessVoid);
                            ceaselessVoid.ai[0] = (int)CeaselessVoidBehaviorOverride.CeaselessVoidAttackType.DarkEnergySwirl;

                            PacketManager.SendPacket<SyncNPCAIClientside>(CalamityGlobalNPC.voidBoss);
                        }
                    }
                    else if (!InfernumMode.CanUseCustomAIs || WorldSaveSystem.ForbiddenArchiveCenter.X == 0)
                    {
                        SoundEngine.PlaySound(MarkofProvidence.CVSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            CalamityUtils.SpawnBossBetter(player.Center, ModContent.NPCType<CeaselessVoid>(), new ExactPositionBossSpawnContext(), (int)CeaselessVoidBehaviorOverride.CeaselessVoidAttackType.DarkEnergySwirl, 0f, 0f, 1f);
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<CeaselessVoid>());
                        return false;
                    }
                }
                else if (player.ZoneUnderworldHeight)
                {
                    // Anger Signus if he's around but not attacking.
                    if (CalamityGlobalNPC.signus != -1)
                    {
                        NPC signus = Main.npc[CalamityGlobalNPC.signus];
                        if (signus.ai[1] == (int)SignusBehaviorOverride.SignusAttackType.Patrol)
                        {
                            SoundEngine.PlaySound(MarkofProvidence.SignutSound, player.Center);
                            SignusBehaviorOverride.SelectNextAttack(signus);
                            signus.ai[1] = (int)SignusBehaviorOverride.SignusAttackType.KunaiDashes;
                            signus.Infernum().ExtraAI[9] = 0f;

                            PacketManager.SendPacket<SyncNPCAIClientside>(CalamityGlobalNPC.signus);
                        }
                    }
                    else
                    {
                        SoundEngine.PlaySound(MarkofProvidence.SignutSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Signus>());
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<Signus>());
                        return false;
                    }
                }
                else if (player.ZoneSkyHeight)
                {
                    int weaverIndex = NPC.FindFirstNPC(ModContent.NPCType<StormWeaverHead>());
                    if (weaverIndex != -1)
                    {
                        NPC weaver = Main.npc[weaverIndex];
                        if (weaver.ai[1] == (int)StormWeaverHeadBehaviorOverride.StormWeaverAttackType.HuntSkyCreatures)
                        {
                            SoundEngine.PlaySound(MarkofProvidence.StormSound, player.Center);
                            StormWeaverHeadBehaviorOverride.SelectNewAttack(weaver);
                            weaver.ai[1] = (int)StormWeaverHeadBehaviorOverride.StormWeaverAttackType.IceStorm;

                            PacketManager.SendPacket<SyncNPCAIClientside>(weaverIndex);
                        }
                    }
                    else
                    {
                        SoundEngine.PlaySound(MarkofProvidence.StormSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<StormWeaverHead>());
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<StormWeaverHead>());
                        return false;
                    }
                }

                return true;
            });
            cursor.Emit(OpCodes.Newobj, typeof(bool?).GetConstructor([typeof(bool)])!);
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class StoreForbiddenArchivePositionHook : ModSystem
    {
        public static MethodInfo? PlaceForbiddenArchive = typeof(DungeonArchive).GetMethod("PlaceArchive", Utilities.UniversalBindingFlags);
        public static ILHook? StoreForbiddenArchivePosition_IL_Hook;
        public override void OnModLoad()
        {
            if (PlaceForbiddenArchive != null)
            {
                StoreForbiddenArchivePosition_IL_Hook = new(PlaceForbiddenArchive, StoreForbiddenArchivePosition_IL);
                StoreForbiddenArchivePosition_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void StoreForbiddenArchivePosition_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            int xLocalIndex = 0;
            int yLocalIndex = 0;
            ConstructorInfo pointConstructor = typeof(Point).GetConstructor([typeof(int), typeof(int)])!;
            MethodInfo placementMethod = typeof(SchematicManager).GetMethods().First(m => m.Name == "PlaceSchematic");

            // Find the first instance of the schematic placement call. There are three, but they all take the same information so it doesn't matter which one is used as a reference.
            cursor.GotoNext(i => i.MatchLdstr(SchematicManager.BlueArchiveKey));

            // Find the part of the method call where the placement Point type is made, and read off the IL indices for the X and Y coordinates with intent to store them elsewhere.
            cursor.GotoNext(i => i.MatchNewobj(pointConstructor));
            cursor.GotoPrev(i => i.MatchLdloc(out yLocalIndex));
            cursor.GotoPrev(i => i.MatchLdloc(out xLocalIndex));

            // Go back to the beginning of the method and store the placement position so that it isn't immediately discarded after world generation- Ceaseless Void's natural spawning needs it.
            // This needs to be done at each of hte three schematic placement variants since sometimes post-compilation optimizations can scatter about return instructions.
            cursor.Index = 0;
            for (int i = 0; i < 3; i++)
            {
                cursor.GotoNext(i => i.MatchLdftn(out _));
                cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(out _));
                cursor.Emit(OpCodes.Ldloc, xLocalIndex);
                cursor.Emit(OpCodes.Ldloc, yLocalIndex);
                cursor.EmitDelegate((int x, int y) =>
                {
                    WorldSaveSystem.ForbiddenArchiveCenter = new(x, y);
                });
            }
        }
    }

    internal sealed class ChangeProfanedShardCanUseHook : ModSystem
    {
        public static MethodInfo? ProfanedShardCanUseItem = typeof(ProfanedShard).GetMethod("CanUseItem", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeProfanedShardCanUse_IL_Hook;
        public override void OnModLoad()
        {
            if (ProfanedShardCanUseItem != null)
            {
                ChangeProfanedShardCanUse_IL_Hook = new(ProfanedShardCanUseItem, ChangeProfanedShardCanUse_IL);
                ChangeProfanedShardCanUse_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeProfanedShardCanUse_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((Player player) =>
            {
                bool correctBiome = player.Hitbox.Intersects(GuardianComboAttackManager.ShardUseisAllowedArea) && !WeakReferenceSupport.InAnySubworld();
                bool bossIsNotPresent = !NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>());

                if (InfernumMode.CanUseCustomAIs)
                    return correctBiome && bossIsNotPresent && !BossRushEvent.BossRushActive;
                else
                {
                    // Base cals checks, so they still function if you have the mod on but not the mode.
                    if (!NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()) && (Main.dayTime || Main.remixWorld) && (player.ZoneHallow || player.ZoneUnderworldHeight))
                        return !BossRushEvent.BossRushActive;
                    return false;
                }
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ChangeProfanedShardUseHook : ModSystem
    {
        public static MethodInfo? ProfanedShardUseItem = typeof(ProfanedShard).GetMethod("UseItem", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeProfanedShardUse_IL_Hook;
        public override void OnModLoad()
        {
            if (ProfanedShardUseItem != null)
            {
                ChangeProfanedShardUse_IL_Hook = new(ProfanedShardUseItem, ChangeProfanedShardUse_IL);
                ChangeProfanedShardUse_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeProfanedShardUse_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((Player player) =>
            {
                // Normal spawning stuff
                if (!WorldSaveSystem.InfernumModeEnabled)
                {
                    // This runs like 6 times without this check for some fucking reason.
                    if (!NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()))
                    {
                        SoundEngine.PlaySound(in SoundID.Roar, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<ProfanedGuardianCommander>());
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<ProfanedGuardianCommander>());
                    }
                }
                else if (Main.myPlayer == player.whoAmI && !Main.projectile.Any(p => p.active && p.type == ModContent.ProjectileType<GuardiansSummonerProjectile>()))
                    Utilities.NewProjectileBetter(player.Center, Vector2.Zero, ModContent.ProjectileType<GuardiansSummonerProjectile>(), 0, 0f);
            });
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Newobj, typeof(bool?).GetConstructor([typeof(bool)])!);
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class StopCultistShieldDrawingHook : ModSystem
    {
        public static MethodInfo? CalGlobalNPCPostDraw = typeof(CalamityGlobalNPC).GetMethod("PostDraw", Utilities.UniversalBindingFlags);
        public static ILHook? StopCultistShieldDrawing_IL_Hook;

        public override void OnModLoad()
        {
            if (CalGlobalNPCPostDraw != null)
            {
                StopCultistShieldDrawing_IL_Hook = new(CalGlobalNPCPostDraw, CalGlobalNPCPostDraw_IL);
                StopCultistShieldDrawing_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void CalGlobalNPCPostDraw_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            // Make the type checks check a negative number, which they will never match.
            if (cursor.TryGotoNext(MoveType.After, c => c.MatchLdcI4(439)))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4, -1);
            }

            if (cursor.TryGotoNext(MoveType.After, c => c.MatchLdcI4(440)))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4, -1);
            }
        }
    }

    internal sealed class MakeAquaticScourgeSpitOutDropsHook : ModSystem
    {
        public override void Load() => On_CommonCode.ModifyItemDropFromNPC += ThrowItemsOut;

        public static void ThrowItemsOut(On_CommonCode.orig_ModifyItemDropFromNPC orig, NPC npc, int itemIndex)
        {
            orig(npc, itemIndex);
            if (npc.type == ModContent.NPCType<AquaticScourgeHead>() && InfernumMode.CanUseCustomAIs)
            {
                Item item = Main.item[itemIndex];
                item.velocity = npc.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.74f) * Main.rand.NextFloat(9f, 25f);

                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex, 1f, 0f, 0f, 0, 0, 0);
            }
        }
    }
}
