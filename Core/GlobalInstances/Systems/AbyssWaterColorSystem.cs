using InfernumMode.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class AbyssWaterColorSystem : ModSystem
    {
        public static int AbyssWaterID
        {
            get;
            internal set;
        }

        public static float WaterBlacknessInterpolant
        {
            get;
            set;
        } = 1f;

        public static float OrangeAbyssWaterInterpolant
        {
            get;
            set;
        }

        internal static VertexColors ChangeAbyssColors(VertexColors initialColor, int liquidType, Point p)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld && !InfernumConfig.Instance.ReducedGraphicsConfig && liquidType == AbyssWaterID)
            {
                Color acidWaterColor = new(62, 217, 145);
                Color abyssWaterColor = new(29, 15, 56);
                Color orangeWaterColor = new(204, 58, 9);

                float topLeftBrightness = (initialColor.TopLeftColor.R + initialColor.TopLeftColor.G + initialColor.TopLeftColor.B) / 765f;
                float topRightBrightness = (initialColor.TopRightColor.R + initialColor.TopRightColor.G + initialColor.TopRightColor.B) / 765f;
                float bottomLeftBrightness = (initialColor.BottomLeftColor.R + initialColor.BottomLeftColor.G + initialColor.BottomLeftColor.B) / 765f;
                float bottomRightBrightness = (initialColor.BottomRightColor.R + initialColor.BottomRightColor.G + initialColor.BottomRightColor.B) / 765f;

                // Conditional exists for optimization purposes.
                if (p.Y >= CustomAbyss.Layer2Top - 24f)
                    initialColor = new(abyssWaterColor);
                else
                {
                    float sulphuricWaterInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top - 24f, CustomAbyss.AbyssTop - 4f, p.Y, true);
                    initialColor = new(Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant));
                }

                if (p.Y >= CustomAbyss.Layer2Top && WaterBlacknessInterpolant > 0f)
                {
                    float blacknessInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top, CustomAbyss.Layer4Top, p.Y, true) * WaterBlacknessInterpolant * 0.44f;
                    initialColor.TopLeftColor *= blacknessInterpolant;
                    initialColor.TopLeftColor.A = 255;

                    initialColor.TopRightColor *= blacknessInterpolant;
                    initialColor.TopRightColor.A = 255;

                    initialColor.BottomLeftColor *= blacknessInterpolant;
                    initialColor.BottomLeftColor.A = 255;

                    initialColor.BottomRightColor *= blacknessInterpolant;
                    initialColor.BottomRightColor.A = 255;
                }
                
                float orangeWaterInterpolant = OrangeAbyssWaterInterpolant;
                if (orangeWaterInterpolant > 0f)
                    initialColor = new(Color.Lerp(abyssWaterColor, orangeWaterColor, orangeWaterInterpolant));

                initialColor.TopLeftColor *= topLeftBrightness;
                initialColor.TopRightColor *= topRightBrightness;
                initialColor.BottomLeftColor *= bottomLeftBrightness;
                initialColor.BottomRightColor *= bottomRightBrightness;
            }

            return initialColor;
        }

        private static void ChangeWaterQuadColors(ILContext il)
        {
            ILCursor cursor = new(il);
            MethodInfo textureGetValueMethod = typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < 2; i++)
            {
                if (!cursor.TryGotoNext(MoveType.After, c => c.MatchCallOrCallvirt("MonoMod.Cil.RuntimeILReferenceBag/FastDelegateInvokers", "Invoke")))
                    return;
            }

            // Pass the texture in so that the method can ensure it is not messing around with non-lava textures.
            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.Emit(OpCodes.Ldloc, 3);
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.EmitDelegate<Func<VertexColors, int, int, int, VertexColors>>((initialColor, liquidType, x, y) =>
            {
                return ChangeAbyssColors(initialColor, liquidType, new(x, y));
            });
        }

        public override void OnModLoad()
        {
            if (Main.netMode != NetmodeID.Server)
                AbyssWaterID = ModContent.Find<ModWaterStyle>("InfernumMode/AbyssWater").Slot;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += ChangeWaterQuadColors;
        }

        public override void Unload()
        {
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= ChangeWaterQuadColors;
        }

        public override void PreUpdateEntities()
        {
            WaterBlacknessInterpolant = MathHelper.Clamp(WaterBlacknessInterpolant + 0.02f, 0f, 1f);
        }
    }
}