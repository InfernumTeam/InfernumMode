using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.UI.DraedonSummoning;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.UtilityMethods;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.ILEditingStuff.HookManager;

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

        public void Load() => IL.Terraria.Projectile.AI_007_GrapplingHooks += AdjustPlatformCollisionChecks;
        public void Unload() => IL.Terraria.Projectile.AI_007_GrapplingHooks -= AdjustPlatformCollisionChecks;
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
            return orig() || SubworldSystem.IsActive<LostColosseum>() && Sandstorm.Happening;
        }

        public void Load() => On.Terraria.GameContent.Events.Sandstorm.ShouldSandstormDustPersist += LetSandParticlesAppear;

        public void Unload() => On.Terraria.GameContent.Events.Sandstorm.ShouldSandstormDustPersist -= LetSandParticlesAppear;
    }

    public class DrawVoidBackgroundDuringMLFightHook : IHookEdit
    {
        public static void PrepareShaderForBG(On.Terraria.Main.orig_DrawSurfaceBG orig, Main self)
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