using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.DesertScourge;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.ILEditingStuff.HookManager;
using Terraria.Audio;
using Terraria.DataStructures;
using CalamityMod.UI;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using CalamityMod.World;
using Terraria.GameContent;

namespace InfernumMode.ILEditingStuff
{
    public class ReplaceGoresHook : IHookEdit
    {
        internal static Gore AlterGores(On.Terraria.Gore.orig_NewGorePerfect_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return new();

            if (InfernumMode.CanUseCustomAIs && Type == 573)
                Type = Utilities.GetGoreID("DukeFishronGore1");
            if (InfernumMode.CanUseCustomAIs && Type == 574)
                Type = Utilities.GetGoreID("DukeFishronGore3");
            if (InfernumMode.CanUseCustomAIs && Type == 575)
                Type = Utilities.GetGoreID("DukeFishronGore2");
            if (InfernumMode.CanUseCustomAIs && Type == 576)
                Type = Utilities.GetGoreID("DukeFishronGore4");

            return orig(source, Position, Velocity, Type, Scale);
        }

        public void Load() => On.Terraria.Gore.NewGorePerfect_IEntitySource_Vector2_Vector2_int_float += AlterGores;

        public void Unload() => On.Terraria.Gore.NewGorePerfect_IEntitySource_Vector2_Vector2_int_float -= AlterGores;
    }

    public class AureusPlatformWalkingHook : IHookEdit
    {
        internal static bool LetAureusWalkOnPlatforms(On.Terraria.NPC.orig_Collision_DecideFallThroughPlatforms orig, NPC npc)
        {
            if (npc.type == ModContent.NPCType<AstrumAureus>())
            {
                if (Main.player[npc.target].position.Y > npc.Bottom.Y)
                    return true;
                return false;
            }
            return orig(npc);
        }

        public void Load() => On.Terraria.NPC.Collision_DecideFallThroughPlatforms += LetAureusWalkOnPlatforms;

        public void Unload() => On.Terraria.NPC.Collision_DecideFallThroughPlatforms -= LetAureusWalkOnPlatforms;
    }

    public class DrawDraedonSelectionUIWithAthena : IHookEdit
    {
        public static float AthenaIconScale
        {
            get;
            set;
        } = 1f;

        internal static void DrawSelectionUI(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.EmitDelegate(DrawWrapper);
            cursor.Emit(OpCodes.Ret);
        }

        public static void DrawWrapper()
        {
            Vector2 drawAreaVerticalOffset = Vector2.UnitY * 105f;
            Vector2 baseDrawPosition = Main.LocalPlayer.Top + drawAreaVerticalOffset - Main.screenPosition;
            Vector2 destroyerIconDrawOffset = new(-78f, -124f);
            Vector2 primeIconDrawOffset = new(0f, -140f);
            Vector2 twinsIconDrawOffset = new(78f, -124f);
            Vector2 athenaIconDrawOffset = new(78f, -130f);

            if (InfernumMode.CanUseCustomAIs)
            {
                destroyerIconDrawOffset = new(-78f, -130f);
                primeIconDrawOffset = new(-26f, -130f);
                twinsIconDrawOffset = new(26f, -130f);

                HandleInteractionWithButton(baseDrawPosition + destroyerIconDrawOffset, (int)ExoMech.Destroyer);
                HandleInteractionWithButton(baseDrawPosition + primeIconDrawOffset, (int)ExoMech.Prime);
                HandleInteractionWithButton(baseDrawPosition + twinsIconDrawOffset, (int)ExoMech.Twins);
                HandleInteractionWithButton(baseDrawPosition + athenaIconDrawOffset, 4);
                return;
            }

            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + destroyerIconDrawOffset, ExoMech.Destroyer);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + primeIconDrawOffset, ExoMech.Prime);
            ExoMechSelectionUI.HandleInteractionWithButton(baseDrawPosition + twinsIconDrawOffset, ExoMech.Twins);
        }

        public static void HandleInteractionWithButton(Vector2 drawPosition, int exoMech)
        {
            float iconScale;
            string description;
            Texture2D iconMechTexture;

            switch (exoMech)
            {
                case 1:
                    iconScale = ExoMechSelectionUI.DestroyerIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/UI/HeadIcon_THanos").Value;
                    description = "Thanatos, a serpentine terror with impervious armor and innumerable laser turrets.";
                    break;
                case 2:
                    iconScale = ExoMechSelectionUI.PrimeIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/UI/HeadIcon_Ares").Value;
                    description = "Ares, a heavyweight, diabolical monstrosity with four Exo superweapons.";
                    break;
                case 3:
                    iconScale = ExoMechSelectionUI.TwinsIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/UI/HeadIcon_ArtemisApollo").Value;
                    description = "Artemis and Apollo, a pair of extremely agile destroyers with pulse cannons.";
                    break;
                case 4:
                default:
                    iconScale = AthenaIconScale;
                    iconMechTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/HeadIcon_Athena").Value;
                    description = "Athena, a giant supercomputer with multiple mounted pulse turrets.";
                    drawPosition.Y += 2f;
                    break;
            }

            // Check for mouse collision/clicks.
            Rectangle clickArea = Utils.CenteredRectangle(drawPosition, iconMechTexture.Size() * iconScale * 0.9f);

            // Check if the mouse is hovering over the contact button area.
            bool hoveringOverIcon = ExoMechSelectionUI.MouseScreenArea.Intersects(clickArea);
            if (hoveringOverIcon)
            {
                // If so, cause the button to inflate a little bit.
                iconScale = MathHelper.Clamp(iconScale + 0.0375f, 1f, 1.35f);

                // Make the selection known if a click is done.
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    CalamityWorld.DraedonMechToSummon = (ExoMech)exoMech;

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        var netMessage = InfernumMode.CalamityMod.GetPacket();
                        netMessage.Write((byte)CalamityModMessageType.ExoMechSelection);
                        netMessage.Write((int)CalamityWorld.DraedonMechToSummon);
                        netMessage.Send();
                    }
                }
                Main.blockMouse = Main.LocalPlayer.mouseInterface = true;
            }

            // Otherwise, if not hovering, cause the button to deflate back to its normal size.
            else
                iconScale = MathHelper.Clamp(iconScale - 0.05f, 1f, 1.2f);

            // Draw the icon with the new scale.
            Main.spriteBatch.Draw(iconMechTexture, drawPosition, null, Color.White, 0f, iconMechTexture.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

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
                case 4:
                    AthenaIconScale = iconScale;
                    break;
            }
        }

        public void Load() => ExoMechSelectionUIDraw += DrawSelectionUI;

        public void Unload() => ExoMechSelectionUIDraw -= DrawSelectionUI;
    }

    public class FishronSkyDistanceLeniancyHook : IHookEdit
    {
        internal static void AdjustFishronScreenDistanceRequirement(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchLdcR4(3000f));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_R4, 6000f);
        }

        public void Load() => IL.Terraria.GameContent.Events.ScreenDarkness.Update += AdjustFishronScreenDistanceRequirement;

        public void Unload() => IL.Terraria.GameContent.Events.ScreenDarkness.Update -= AdjustFishronScreenDistanceRequirement;
    }

    public class LessenDesertTileRequirementsHook : IHookEdit
    {
        internal static void MakeDesertRequirementsMoreLenient(On.Terraria.Player.orig_UpdateBiomes orig, Player self)
        {
            orig(self);
            self.ZoneDesert = Main.SceneMetrics.SandTileCount > 300;
        }

        public void Load() => On.Terraria.Player.UpdateBiomes += MakeDesertRequirementsMoreLenient;

        public void Unload() => On.Terraria.Player.UpdateBiomes -= MakeDesertRequirementsMoreLenient;
    }

    public class SepulcherOnHitProjectileEffectRemovalHook : IHookEdit
    {
        internal static void EarlyReturn(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load()
        {
            SepulcherHeadModifyProjectile += EarlyReturn;
            SepulcherBodyModifyProjectile += EarlyReturn;
            SepulcherTailModifyProjectile += EarlyReturn;
        }

        public void Unload()
        {
            SepulcherHeadModifyProjectile -= EarlyReturn;
            SepulcherBodyModifyProjectile -= EarlyReturn;
            SepulcherTailModifyProjectile -= EarlyReturn;
        }
    }

    public class GetRidOfDesertNuisancesHook : IHookEdit
    {
        internal static void GetRidOfDesertNuisances(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Player>>(player =>
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position, 0);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertScourgeHead>());
                else
                    NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertScourgeHead>());
            });
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => DesertScourgeItemUseItem += GetRidOfDesertNuisances;

        public void Unload() => DesertScourgeItemUseItem -= GetRidOfDesertNuisances;
    }

    public class LetAresHitPlayersHook : IHookEdit
    {
        internal static void LetAresHitPlayer(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => AresBodyCanHitPlayer += LetAresHitPlayer;

        public void Unload() => AresBodyCanHitPlayer -= LetAresHitPlayer;
    }
}