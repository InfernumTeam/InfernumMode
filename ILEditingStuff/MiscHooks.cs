using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class DrawInfernumModeIndicatorHook : IHookEdit
    {
        public static float ModeUIFrameTimer = 0f;
        internal static void DrawInfernumModeUI()
        {
            // The mode indicator should only be displayed when the inventory is open, to prevent obstruction.
            if (!Main.playerInventory)
                return;

            bool renderingText = false;
            Rectangle mouseRectangle = Utils.CenteredRectangle(Main.MouseScreen, Vector2.One * 2f);

            // 3 times the number of total frames, so that each frame gets displayed 3 times.
            if (ModeUIFrameTimer >= 186f)
                ModeUIFrameTimer = 0f;

            float row = (float)Math.Floor(ModeUIFrameTimer / 3);
            if (row >= 27)
                row -= (((float)Math.Floor(row / 27)) * 27);

            Texture2D outerAreaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumBG");
            Texture2D armaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumArma");
            Texture2D maliceTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumMalice");

            Rectangle areaFrame = outerAreaTexture.Frame(3, 27, (int)Math.Floor(ModeUIFrameTimer / 81), (int)row);
            Vector2 drawCenter = new Vector2(Main.screenWidth - 400f, 72f) + areaFrame.Size() * 0.5f;

            if (CalamityPlayer.areThereAnyDamnBosses)
            {
                Color drawColor = Color.Red * 0.4f;
                drawColor.A = 0;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTime * 4f).ToRotationVector2() * 5f;
                    Main.spriteBatch.Draw(outerAreaTexture, drawCenter + drawOffset, areaFrame, drawColor, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(outerAreaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (CalamityWorld.armageddon)
                Main.spriteBatch.Draw(armaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (CalamityWorld.malice)
                Main.spriteBatch.Draw(maliceTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Infernum active text.
            if (mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter - Vector2.UnitY * 14f, new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage("Infernum Mode is active.", Color.Red));
                renderingText = true;
            }

            // Armageddon active text.
            if (!renderingText && mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter + new Vector2(12f, 7f), new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage($"Armageddon is {(CalamityWorld.armageddon ? "active" : "not active")}.", Color.DarkViolet));
                renderingText = true;
            }

            // Armageddon active text.
            if (!renderingText && mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter + new Vector2(-12f, 7f), new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage($"Malice is {(CalamityWorld.malice ? "active" : "not active")}.", Color.LightCoral));
                renderingText = true;
            }

            ModeUIFrameTimer++;
            // Displays each frame for 2 ticks instead of 3 if there are any bosses by skipping the 3rd frame.
            if (CalamityPlayer.areThereAnyDamnBosses && (ModeUIFrameTimer + 4) % 3 == 0)
                ModeUIFrameTimer++;

            // Flush text data to the screen.
            if (renderingText)
                Main.instance.MouseTextHackZoom(string.Empty);
        }

        internal static void DrawInfernumIcon(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Go to the last Ret and leave a marker to return to so that manual
            // drawing can be done.
            while (cursor.TryGotoNext(i => i.MatchRet())) { }

            ILLabel endOfMethod = cursor.DefineLabel();
            cursor.MarkLabel(endOfMethod);

            cursor.Index = 0;
            cursor.EmitDelegate<Action>(() =>
            {
                if (PoDWorld.InfernumMode)
                    DrawInfernumModeUI();
            });

            cursor.Emit(OpCodes.Ldsfld, typeof(PoDWorld).GetField("InfernumMode"));
            cursor.Emit(OpCodes.Brtrue, endOfMethod);
        }

        public void Load() => ModeIndicatorUIDraw += DrawInfernumIcon;

        public void Unload() => ModeIndicatorUIDraw -= DrawInfernumIcon;
    }

    public class PermitOldDukeRainHook : IHookEdit
    {
        internal static void PermitODRain(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStsfld<Main>("raining")))
                return;

            int start = cursor.Index - 1;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<CalamityNetcode>("SyncWorld")))
                return;

            int end = cursor.Index;
            cursor.Goto(start);
            cursor.RemoveRange(end - start);
            cursor.Emit(OpCodes.Nop);
        }

        public void Load() => CalamityWorldPostUpdate += PermitODRain;

        public void Unload() => CalamityWorldPostUpdate -= PermitODRain;
    }

    public class NerfShellfishStaffDebuffHook : IHookEdit
    {
        internal static void NerfShellfishStaff(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            for (int j = 0; j < 2; j++)
            {
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(250)))
                    return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 150);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(50)))
                return;

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 30);
        }

        public void Load() => CalamityNPCLifeRegen += NerfShellfishStaff;

        public void Unload() => CalamityNPCLifeRegen -= NerfShellfishStaff;
    }

    public class RemoveProjectileOnHitLagHook : IHookEdit
    {
        internal static void RemoveProjectileOnHitLag(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, c => c.MatchLdcI4(267));
            cursor.GotoPrev(MoveType.After, c => c.MatchStloc(5));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Stloc, 4);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Stloc, 5);
        }

        public void Load() => CalamityPlayerModifyHitByProjectile += RemoveProjectileOnHitLag;

        public void Unload() => CalamityPlayerModifyHitByProjectile -= RemoveProjectileOnHitLag;
    }

    /*
    public class UseDeathContactDamageHook : IHookEdit
    {
        internal static FieldInfo EnemyStatsField = typeof(NPCStats).GetNestedType("EnemyStats", Utilities.UniversalBindingFlags).GetField("ContactDamageValues", Utilities.UniversalBindingFlags);
        internal static void UseDeathContactDamageInInfernum(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.EmitDelegate<Action<NPC>>(CalculateContactDamage);
            cursor.Emit(OpCodes.Ret);
        }

        internal static void CalculateContactDamage(NPC npc)
        {
            double damageAdjustment = NPCStats.GetExpertDamageMultiplier(npc) * 2D;

            // Safety check: If for some reason the contact damage array is not initialized yet, set the NPC's damage to 1.
            SortedDictionary<int, int[]> enemyStats = (SortedDictionary<int, int[]>)EnemyStatsField.GetValue(null);
            bool exists = enemyStats.TryGetValue(npc.type, out int[] contactDamage);
            if (!exists)
                npc.damage = 1;

            int normalDamage = contactDamage[0];
            int expertDamage = contactDamage[1] == -1 ? -1 : (int)Math.Round(contactDamage[1] / damageAdjustment);
            int revengeanceDamage = contactDamage[2] == -1 ? -1 : (int)Math.Round(contactDamage[2] / damageAdjustment);
            int deathDamage = contactDamage[3] == -1 ? -1 : (int)Math.Round(contactDamage[3] / damageAdjustment);

            // If the assigned value would be -1, don't actually assign it. This allows for conditionally disabling the system.
            int damageToUse = (CalamityWorld.death || PoDWorld.InfernumMode) ? deathDamage : CalamityWorld.revenge ? revengeanceDamage : Main.expertMode ? expertDamage : normalDamage;
            if (CalamityWorld.malice && damageToUse != -1)
                damageToUse = (int)Math.Round(damageToUse * CalamityGlobalNPC.MaliceModeDamageMultiplier);
            if (damageToUse != -1)
                npc.damage = damageToUse;
        }

        public void Load() => NPCStatsDefineContactDamage += UseDeathContactDamageInInfernum;

        public void Unload() => NPCStatsDefineContactDamage -= UseDeathContactDamageInInfernum;
    }
    */
}