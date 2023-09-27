using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.DataStructures;
using CalamityMod.Events;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.Schematics;
using CalamityMod.Systems;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
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
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Core.ILEditingStuff.HookManager;

namespace InfernumMode.Core.ILEditingStuff
{
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
            c.EmitDelegate(() => InfernumMode.CanUseCustomAIs && !Main.LocalPlayer.Calamity().adrenalineModeActive ? BalancingChangesManager.AdrenalineChargeTimeFactor : 1f);
            c.Emit(OpCodes.Div);
        }

        public void Load() => UpdateRippers += NerfAdrenalineRates;
        public void Unload() => UpdateRippers -= NerfAdrenalineRates;
    }

    public class RenameGreatSandSharkHook : IHookEdit
    {
        internal string RenameGSS(On_Lang.orig_GetNPCNameValue orig, int netID)
        {
            if (netID == ModContent.NPCType<GreatSandShark>() && InfernumMode.CanUseCustomAIs)
                return GreatSandSharkBehaviorOverride.NewName;

            return orig(netID);
        }

        public void Load() => On_Lang.GetNPCNameValue += RenameGSS;

        public void Unload() => On_Lang.GetNPCNameValue -= RenameGSS;
    }

    public class MakeHooksInteractWithPlatforms : IHookEdit
    {
        internal static NPC[] GetPlatforms(Projectile projectile)
        {
            return Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.type == ModContent.NPCType<GolemArenaPlatform>()).OrderBy(n => projectile.Distance(n.Center)).ToArray();
        }

        internal static bool PlatformRequirement(Projectile projectile)
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

        internal static void HandleAttachment(Projectile projectile)
        {
            if (PlatformRequirement(projectile) && projectile.ai[0] != 2f)
                projectile.Center = Vector2.Lerp(projectile.Center, GetPlatforms(projectile)[0].Center, 0.3f);
        }

        internal static void AdjustHitPlatformCoords(Projectile projectile, ref int x, ref int y)
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

        public void Load() => IL_Projectile.AI_007_GrapplingHooks += AdjustPlatformCollisionChecks;
        public void Unload() => IL_Projectile.AI_007_GrapplingHooks -= AdjustPlatformCollisionChecks;
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

        public void Load() => IL_Player.ItemCheck += DisableMoonLordBuilding;

        public void Unload() => IL_Player.ItemCheck -= DisableMoonLordBuilding;
    }

    public class ChangeHowMinibossesSpawnInDD2EventHook : IHookEdit
    {
        internal static int GiveDD2MinibossesPointPriority(On_DD2Event.orig_GetMonsterPointsWorth orig, int slainMonsterID)
        {
            if (OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID) && minibossID != NPCID.DD2Betsy && InfernumMode.CanUseCustomAIs)
                return slainMonsterID == minibossID ? 99999 : 0;

            return orig(slainMonsterID);
        }

        public void Load() => On_DD2Event.GetMonsterPointsWorth += GiveDD2MinibossesPointPriority;

        public void Unload() => On_DD2Event.GetMonsterPointsWorth -= GiveDD2MinibossesPointPriority;
    }

    public class AllowSandstormInColosseumHook : IHookEdit
    {
        internal static bool LetSandParticlesAppear(On_Sandstorm.orig_ShouldSandstormDustPersist orig)
        {
            return orig() || SubworldSystem.IsActive<LostColosseum>() && Sandstorm.Happening;
        }

        public void Load() => On_Sandstorm.ShouldSandstormDustPersist += LetSandParticlesAppear;

        public void Unload() => On_Sandstorm.ShouldSandstormDustPersist -= LetSandParticlesAppear;
    }

    public class DrawVoidBackgroundDuringMLFightHook : IHookEdit
    {
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

        public void Load() => On_Main.DrawSurfaceBG += PrepareShaderForBG;

        public void Unload() => On_Main.DrawSurfaceBG -= PrepareShaderForBG;
    }

    public class DrawCherishedSealocketHook : IHookEdit
    {
        public static ManagedRenderTarget PlayerForcefieldTarget
        {
            get;
            internal set;
        }

        public static ArmorShaderData ForcefieldShader
        {
            get;
            internal set;
        }

        private void DrawForcefields(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            Main.LocalPlayer.Infernum_CalShadowHex().DrawAllHexes();

            if (PlayerForcefieldTarget is null)
            {
                // Ensure orig is called regardless.
                orig(self);
                return;
            }

            // Draw the render target, optionally with a dye shader.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer);

            float shieldScale = Main.LocalPlayer.GetModPlayer<SealocketPlayer>().ForcefieldOpacity * 0.3f;
            Vector2 shieldSize = Vector2.One * shieldScale * 512f;
            Rectangle shaderArea = Utils.CenteredRectangle(PlayerForcefieldTarget.Target.Size(), shieldSize);
            SealocketPlayer sealocketPlayer = Main.LocalPlayer.GetModPlayer<SealocketPlayer>();

            if (sealocketPlayer.ForcefieldOpacity >= 0.01f && sealocketPlayer.ForcefieldDissipationInterpolant < 0.99f)
                ForcefieldShader?.Apply(null, new(PlayerForcefieldTarget.Target, Vector2.Zero, shaderArea, Color.White));
            Main.spriteBatch.Draw(PlayerForcefieldTarget.Target, Main.LocalPlayer.Center - Main.screenPosition, null, Color.White, 0f, PlayerForcefieldTarget.Target.Size() * 0.5f, 1f, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();

            orig(self);
        }

        private void PrepareSealocketTarget(On_Main.orig_CheckMonoliths orig)
        {
            orig();

            var device = Main.instance.GraphicsDevice;
            RenderTargetBinding[] bindings = device.GetRenderTargets();
            PlayerForcefieldTarget.SwapToRenderTarget();

            // Draw forcefields to the render target.
            Main.spriteBatch.Begin();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active || Main.player[i].outOfRange || Main.player[i].dead)
                    continue;

                DrawForcefield(Main.player[i]);
            }
            Main.spriteBatch.End();
            device.SetRenderTargets(bindings);
        }

        public static void DrawForcefield(Player player)
        {
            SealocketPlayer sealocketPlayer = player.GetModPlayer<SealocketPlayer>();
            BrimstoneCrescentForcefieldPlayer crescentPlayer = player.GetModPlayer<BrimstoneCrescentForcefieldPlayer>();

            sealocketPlayer.ForcefieldOpacity = 1f;

            // Draw the sealocket forcefield.
            Vector2 forcefieldDrawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + Vector2.UnitY * player.gfxOffY;
            if (sealocketPlayer.ForcefieldOpacity >= 0.01f && sealocketPlayer.ForcefieldDissipationInterpolant < 0.99f)
            {
                float forcefieldOpacity = (1f - sealocketPlayer.ForcefieldDissipationInterpolant) * sealocketPlayer.ForcefieldOpacity;
                BereftVassal.DrawElectricShield(forcefieldOpacity, forcefieldDrawPosition, forcefieldOpacity, sealocketPlayer.ForcefieldDissipationInterpolant * 1.5f + 1.3f);
            }

            // Draw the Brimstone Crescent forcefield.
            if (crescentPlayer.ForcefieldStrengthInterpolant > 0f)
            {
                float scale = Lerp(0.55f, 1.5f, 1f - crescentPlayer.ForcefieldStrengthInterpolant);
                Color forcefieldColor = CalamityUtils.ColorSwap(Color.Lerp(Color.Red, Color.Yellow, 0.06f), Color.OrangeRed, 5f) * crescentPlayer.ForcefieldStrengthInterpolant;
                CultistBehaviorOverride.DrawForcefield(forcefieldDrawPosition, 1.35f, forcefieldColor, InfernumTextureRegistry.FireNoise.Value, true, scale);
            }
        }

        private void FindSealocketItemDyeShader(Item armorItem, Item dyeItem)
        {
            if (armorItem.type == ModContent.ItemType<CherishedSealocket>())
                ForcefieldShader = GameShaders.Armor.GetShaderFromItemId(dyeItem.type);
        }

        public void Load()
        {
            On_Main.CheckMonoliths += PrepareSealocketTarget;
            On_Main.DrawInfernoRings += DrawForcefields;
            DyeFindingSystem.FindDyeEvent += FindSealocketItemDyeShader;
            PlayerForcefieldTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
        }

        public void Unload()
        {
            On_Main.CheckMonoliths -= PrepareSealocketTarget;
            On_Main.DrawInfernoRings -= DrawForcefields;
        }
    }

    public class DisableWaterEffectsInFightsHook : IHookEdit
    {
        private void DisableWaterEffects(ILContext il)
        {
            ILCursor c = new(il);

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

        public void Load() => IL_Player.Update += DisableWaterEffects;

        public void Unload() => IL_Player.Update -= DisableWaterEffects;
    }

    public class MakeSulphSeaWaterEasierToSeeInHook : IHookEdit
    {
        internal static int SulphurWaterIndex
        {
            get;
            set;
        }

        // WHY IS THIS SO LAGGY WHAT THE ACTUAL FUCK???
        public static bool CanUseHighQualityWater => false;

        private void MakeWaterEasierToSeeIn(ILContext il)
        {
            ILCursor c = new(il);
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

        private void MakeSulphSeaWaterBrighter(Terraria.Graphics.Light.On_TileLightScanner.orig_GetTileLight orig, Terraria.Graphics.Light.TileLightScanner self, int x, int y, out Vector3 outputColor)
        {
            orig(self, x, y, out outputColor);

            Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
            if (tile.LiquidAmount <= 0 || tile.HasTile || Main.waterStyle != SulphurWaterIndex)
                return;

            if (tile.TileType != (ushort)ModContent.TileType<RustyChestTile>())
            {
                Vector3 idealColor = Color.LightSeaGreen.ToVector3();

                if (SulphuricWaterSafeZoneSystem.NearbySafeTiles.Count >= 1)
                {
                    Color cleanWaterColor = new(10, 109, 193);
                    Point closestSafeZone = SulphuricWaterSafeZoneSystem.NearbySafeTiles.Keys.OrderBy(t => t.ToVector2().DistanceSQ(new(x, y))).First();
                    float distanceToClosest = new Vector2(x, y).Distance(closestSafeZone.ToVector2());
                    float acidicWaterInterpolant = Utils.GetLerpValue(12f, 20.5f, distanceToClosest + (1f - SulphuricWaterSafeZoneSystem.NearbySafeTiles[closestSafeZone]) * 21f, true);
                    idealColor = Vector3.Lerp(idealColor, cleanWaterColor.ToVector3(), 1f - acidicWaterInterpolant);
                }

                outputColor = Vector3.Lerp(outputColor, idealColor, 0.8f);
            }
        }

        public void Load()
        {
            if (Main.netMode != NetmodeID.Server)
                SulphurWaterIndex = ModContent.Find<ModWaterStyle>("CalamityMod/SulphuricWater").Slot;

            SelectSulphuricWaterColor += MakeWaterEasierToSeeIn;
            Terraria.Graphics.Light.On_TileLightScanner.GetTileLight += MakeSulphSeaWaterBrighter;
        }

        public void Unload()
        {
            SelectSulphuricWaterColor -= MakeWaterEasierToSeeIn;
            Terraria.Graphics.Light.On_TileLightScanner.GetTileLight -= MakeSulphSeaWaterBrighter;
        }
    }

    public class ChangeRuneOfKosUsageHook : IHookEdit
    {
        private void ChangeUsageCondition(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((Player player) =>
            {
                bool correctBiome = player.ZoneSkyHeight || player.ZoneUnderworldHeight || player.ZoneDungeon;
                bool bossIsNotPresent = !NPC.AnyNPCs(ModContent.NPCType<StormWeaverHead>()) && !NPC.AnyNPCs(ModContent.NPCType<CeaselessVoid>()) && !NPC.AnyNPCs(ModContent.NPCType<Signus>());
                return correctBiome && (bossIsNotPresent || InfernumMode.CanUseCustomAIs) && !BossRushEvent.BossRushActive;
            });
            cursor.Emit(OpCodes.Ret);
        }

        private void ChangeItemUsage(ILContext il)
        {
            ILCursor cursor = new(il);
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
                            SoundEngine.PlaySound(RuneofKos.CVSound, player.Center);
                            CeaselessVoidBehaviorOverride.SelectNewAttack(ceaselessVoid);
                            ceaselessVoid.ai[0] = (int)CeaselessVoidBehaviorOverride.CeaselessVoidAttackType.DarkEnergySwirl;

                            PacketManager.SendPacket<SyncNPCAIClientside>(CalamityGlobalNPC.voidBoss);
                        }
                    }
                    else if (!InfernumMode.CanUseCustomAIs || WorldSaveSystem.ForbiddenArchiveCenter.X == 0)
                    {
                        SoundEngine.PlaySound(RuneofKos.CVSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            CalamityUtils.SpawnBossBetter(player.Center, ModContent.NPCType<CeaselessVoid>(), new ExactPositionBossSpawnContext(), (int)CeaselessVoidBehaviorOverride.CeaselessVoidAttackType.DarkEnergySwirl, 0f, 0f, 1f);
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<CeaselessVoid>());
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
                            SoundEngine.PlaySound(RuneofKos.SignutSound, player.Center);
                            SignusBehaviorOverride.SelectNextAttack(signus);
                            signus.ai[1] = (int)SignusBehaviorOverride.SignusAttackType.KunaiDashes;
                            signus.Infernum().ExtraAI[9] = 0f;

                            PacketManager.SendPacket<SyncNPCAIClientside>(CalamityGlobalNPC.signus);
                        }
                    }
                    else
                    {
                        SoundEngine.PlaySound(RuneofKos.SignutSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Signus>());
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<Signus>());
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
                            SoundEngine.PlaySound(RuneofKos.StormSound, player.Center);
                            StormWeaverHeadBehaviorOverride.SelectNewAttack(weaver);
                            weaver.ai[1] = (int)StormWeaverHeadBehaviorOverride.StormWeaverAttackType.IceStorm;

                            PacketManager.SendPacket<SyncNPCAIClientside>(weaverIndex);
                        }
                    }
                    else
                    {
                        SoundEngine.PlaySound(RuneofKos.StormSound, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<StormWeaverHead>());
                        else
                            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<StormWeaverHead>());
                    }
                }
            });
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load()
        {
            RuneOfKosCanUseItem += ChangeUsageCondition;
            RuneOfKosUseItem += ChangeItemUsage;
        }

        public void Unload()
        {
            RuneOfKosCanUseItem -= ChangeUsageCondition;
            RuneOfKosUseItem -= ChangeItemUsage;
        }
    }

    public class StoreForbiddenArchivePositionHook : IHookEdit
    {
        private void StorePosition(ILContext il)
        {
            ILCursor cursor = new(il);

            int xLocalIndex = 0;
            int yLocalIndex = 0;
            ConstructorInfo pointConstructor = typeof(Point).GetConstructor(new Type[] { typeof(int), typeof(int) });
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

        public void Load() => PlaceForbiddenArchive += StorePosition;

        public void Unload() => PlaceForbiddenArchive -= StorePosition;
    }

    public class ChangeProfanedShardUsageHook : IHookEdit
    {
        public void Load()
        {
            ProfanedShardUseItem += SummonGuardianSpawnerManager;
            ProfanedShardCanUseItem += CanUseItemEdit;
        }

        public void Unload()
        {
            ProfanedShardUseItem -= SummonGuardianSpawnerManager;
            ProfanedShardCanUseItem -= CanUseItemEdit;
        }

        private void SummonGuardianSpawnerManager(ILContext il)
        {
            ILCursor cursor = new(il);
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
            cursor.Emit(OpCodes.Ret);
        }

        private void CanUseItemEdit(ILContext il)
        {
            ILCursor cursor = new(il);
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

    public class StopCultistShieldDrawingHook : IHookEdit
    {
        public void Load() => CalGlobalNPCPostDraw += StopShieldDrawing;
        public void Unload() => CalGlobalNPCPostDraw -= StopShieldDrawing;

        private void StopShieldDrawing(ILContext il)
        {
            ILCursor cursor = new(il);

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

    public class MakeAquaticScourgeSpitOutDropsHook : IHookEdit
    {
        private void ThrowItemsOut(Terraria.GameContent.ItemDropRules.On_CommonCode.orig_ModifyItemDropFromNPC orig, NPC npc, int itemIndex)
        {
            orig(npc, itemIndex);
            if (npc.type == ModContent.NPCType<AquaticScourgeHead>() && InfernumMode.CanUseCustomAIs)
            {
                Item item = Main.item[itemIndex];
                item.velocity = npc.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.74f) * Main.rand.NextFloat(9f, 25f);

                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex, 1f, 0f, 0f, 0, 0, 0);
            }
        }

        public void Load()
        {
            Terraria.GameContent.ItemDropRules.On_CommonCode.ModifyItemDropFromNPC += ThrowItemsOut;
        }

        public void Unload()
        {
            Terraria.GameContent.ItemDropRules.On_CommonCode.ModifyItemDropFromNPC -= ThrowItemsOut;
        }
    }
}
