using System.Reflection;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WindGridSystem : ModSystem
    {
        public static WindGrid Windgrid
        {
            get;
            internal set;
        }

        public override void OnModLoad()
        {
            On_TileDrawing.Update += StoreWindGrid;
        }

        internal static void StoreWindGrid(On_TileDrawing.orig_Update orig, TileDrawing self)
        {
            orig(self);

            // FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK YOU FUCK
            Windgrid ??= typeof(TileDrawing).GetField("_windGrid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as WindGrid;
        }
    }
}