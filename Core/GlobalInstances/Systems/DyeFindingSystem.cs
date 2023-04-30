using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class DyeFindingSystem : ModSystem
    {
        public delegate void FindDyeDelegate(Item armorItem, Item dyeItem);

        public static event FindDyeDelegate FindDyeEvent;

        public override void OnModLoad()
        {
            On.Terraria.Player.UpdateItemDye += FindDyes;
        }

        private void FindDyes(On.Terraria.Player.orig_UpdateItemDye orig, Player self, bool isNotInVanitySlot, bool isSetToHidden, Item armorItem, Item dyeItem)
        {
            orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);
            FindDyeEvent?.Invoke(armorItem, dyeItem);
        }
    }
}